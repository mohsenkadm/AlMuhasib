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

public partial class GoldExpenseTypesViewModel : ViewModelBase
{
    private readonly IGoldExpenseService _expenseService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private System.Timers.Timer? _debounceTimer;
    private List<GoldExpenseType> _allTypes = [];
    private int? _editingId;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _paginationText = string.Empty;
    [ObservableProperty] private GoldExpenseType? _selectedType;

    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private string _dialogError = string.Empty;

    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private GoldExpenseType? _typeToDelete;

    public ObservableCollection<GoldExpenseType> ExpenseTypes { get; } = [];

    public GoldExpenseTypesViewModel(
        IGoldExpenseService expenseService,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _expenseService = expenseService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "أنواع المصاريف";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.ExpenseTypes);
        await LoadTypesAsync();
    }

    private async Task LoadTypesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            _allTypes = (await _expenseService.GetExpenseTypesAsync(activeOnly: false)).ToList();
            ApplyPaging();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل أنواع المصاريف:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyPaging()
    {
        IEnumerable<GoldExpenseType> query = _allTypes;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            query = query.Where(t => t.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var list = query.ToList();

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            list = ColumnFilterEngine.Apply(list, ColumnFilters).ToList();

        MasterDataColumnFilterHelper.ApplyClientPagination(
            list, ExpenseTypes, CurrentPage, PageSize,
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
        await LoadTypesAsync();
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        if (!CanAdd) return;
        _editingId = null;
        IsEditMode = false;
        DialogTitle = "إضافة نوع مصروف";
        EditName = string.Empty;
        EditIsActive = true;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(GoldExpenseType? item)
    {
        if (item is null || !CanEdit) return;
        _editingId = item.Id;
        IsEditMode = true;
        DialogTitle = "تعديل نوع المصروف";
        EditName = item.Name;
        EditIsActive = item.IsActive;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task SaveTypeAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم النوع مطلوب";
            return;
        }

        try
        {
            if (IsEditMode && _editingId.HasValue)
            {
                var existing = _allTypes.FirstOrDefault(t => t.Id == _editingId.Value);
                if (existing is null)
                {
                    DialogError = "النوع غير موجود";
                    return;
                }

                existing.Name = EditName.Trim();
                existing.IsActive = EditIsActive;
                existing.UpdatedBy = _currentUserService.Username;
                await _expenseService.UpdateExpenseTypeAsync(existing);
            }
            else
            {
                await _expenseService.CreateExpenseTypeAsync(new GoldExpenseType
                {
                    Name = EditName.Trim(),
                    IsActive = EditIsActive,
                    CreatedBy = _currentUserService.Username
                });
            }

            IsDialogOpen = false;
            BeautifulMessageDialog.ShowSuccess("تم حفظ النوع");
            await LoadTypesAsync();
        }
        catch (Exception ex)
        {
            DialogError = ex.Message;
        }
    }

    [RelayCommand]
    private void ConfirmDelete(GoldExpenseType? item)
    {
        if (item is null || !CanDelete) return;
        TypeToDelete = item;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        TypeToDelete = null;
    }

    [RelayCommand]
    private async Task ExecuteDeleteAsync()
    {
        if (TypeToDelete is null) return;
        try
        {
            await _expenseService.DeleteExpenseTypeAsync(TypeToDelete.Id, _currentUserService.Username);
            IsDeleteDialogOpen = false;
            TypeToDelete = null;
            BeautifulMessageDialog.ShowSuccess("تم حذف النوع");
            await LoadTypesAsync();
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
            if (_allTypes.Count == 0)
                await LoadTypesAsync();

            var exportData = _allTypes.Select(t => new
            {
                الاسم = t.Name,
                نشط = t.IsActive ? "نعم" : "لا"
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"أنواع_مصاريف_الذهب_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "أنواع المصاريف");
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
            if (_allTypes.Count == 0)
                await LoadTypesAsync();

            var columns = new[] { "الاسم", "نشط" };
            IList<object[]> rows = _allTypes.Select(t => new object[]
            {
                t.Name,
                t.IsActive ? "نعم" : "لا"
            }).ToList();
            _exportService.PrintTable("أنواع مصاريف الذهب", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}
