using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCategoriesViewModel : ViewModelBase
{
    private readonly IGoldCategoryService _categoryService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private System.Timers.Timer? _debounceTimer;
    private List<GoldCategory> _allCategories = [];
    private int? _editingId;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private GoldCategory? _selectedCategory;

    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private string _dialogError = string.Empty;

    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private GoldCategory? _categoryToDelete;

    public ObservableCollection<GoldCategory> Categories { get; } = [];

    public GoldCategoriesViewModel(
        IGoldCategoryService categoryService,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _categoryService = categoryService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "تصنيفات الذهب";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.Categories);
        await LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            _allCategories = (await _categoryService.GetAllAsync(activeOnly: false)).ToList();
            ApplyPaging();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل التصنيفات:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyPaging()
    {
        IEnumerable<GoldCategory> query = _allCategories;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            query = query.Where(t => t.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var list = query.ToList();

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            list = ColumnFilterEngine.Apply(list, ColumnFilters).ToList();

        MasterDataColumnFilterHelper.ApplyClientPagination(
            list, Categories, CurrentPage, PageSize,
            out var total, out var pages, out var text);
        TotalCount = total;
        TotalPages = pages;
        PaginationText = text;
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        ApplyPaging();
    }

    partial void OnSearchTextChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(400);
        _debounceTimer.Elapsed += (_, _) =>
        {
            _debounceTimer?.Stop();
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                CurrentPage = 1;
                ApplyPaging();
            });
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    [RelayCommand]
    private void FirstPage() { CurrentPage = 1; ApplyPaging(); }

    [RelayCommand]
    private void PreviousPage() { if (CurrentPage > 1) { CurrentPage--; ApplyPaging(); } }

    [RelayCommand]
    private void NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; ApplyPaging(); } }

    [RelayCommand]
    private void LastPage() { CurrentPage = TotalPages; ApplyPaging(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        await LoadCategoriesAsync();
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        if (!CanAdd) return;
        _editingId = null;
        IsEditMode = false;
        DialogTitle = "إضافة تصنيف";
        EditName = string.Empty;
        EditIsActive = true;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(GoldCategory? item)
    {
        if (item is null || !CanEdit) return;
        _editingId = item.Id;
        IsEditMode = true;
        DialogTitle = "تعديل التصنيف";
        EditName = item.Name;
        EditIsActive = item.IsActive;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task SaveCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم التصنيف مطلوب";
            return;
        }

        try
        {
            if (IsEditMode && _editingId.HasValue)
            {
                var existing = _allCategories.FirstOrDefault(t => t.Id == _editingId.Value);
                if (existing is null)
                {
                    DialogError = "التصنيف غير موجود";
                    return;
                }

                existing.Name = EditName.Trim();
                existing.IsActive = EditIsActive;
                existing.UpdatedBy = _currentUserService.Username;
                await _categoryService.UpdateAsync(existing);
            }
            else
            {
                await _categoryService.CreateAsync(new GoldCategory
                {
                    Name = EditName.Trim(),
                    IsActive = EditIsActive,
                    CreatedBy = _currentUserService.Username
                });
            }

            IsDialogOpen = false;
            BeautifulMessageDialog.ShowSuccess("تم حفظ التصنيف");
            await LoadCategoriesAsync();
        }
        catch (Exception ex)
        {
            DialogError = ex.Message;
        }
    }

    [RelayCommand]
    private void ConfirmDelete(GoldCategory? item)
    {
        if (item is null || !CanDelete) return;
        CategoryToDelete = item;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        CategoryToDelete = null;
    }

    [RelayCommand]
    private async Task ExecuteDeleteAsync()
    {
        if (CategoryToDelete is null) return;
        try
        {
            await _categoryService.DeleteAsync(CategoryToDelete.Id, _currentUserService.Username);
            IsDeleteDialogOpen = false;
            CategoryToDelete = null;
            BeautifulMessageDialog.ShowSuccess("تم حذف التصنيف");
            await LoadCategoriesAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            if (_allCategories.Count == 0)
                await LoadCategoriesAsync();

            var exportData = _allCategories.Select(t => new
            {
                الاسم = t.Name,
                نشط = t.IsActive ? "نعم" : "لا"
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"تصنيفات_الذهب_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "تصنيفات الذهب");
                BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء التصدير: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintTable()
    {
        try
        {
            if (_allCategories.Count == 0)
                await LoadCategoriesAsync();

            var columns = new[] { "الاسم", "نشط" };
            IList<object[]> rows = _allCategories.Select(t => new object[]
            {
                t.Name,
                t.IsActive ? "نعم" : "لا"
            }).ToList();
            _exportService.PrintTable("تصنيفات الذهب", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}
