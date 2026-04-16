using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class CategoriesViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<Category> Categories { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 20;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private Category? _selectedCategory;

    // Dialog state
    [ObservableProperty]
    private bool _isDialogOpen;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _dialogTitle = string.Empty;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _dialogError = string.Empty;

    // Delete confirmation
    [ObservableProperty]
    private bool _isDeleteDialogOpen;

    [ObservableProperty]
    private Category? _categoryToDelete;

    private int? _editingCategoryId;
    private System.Timers.Timer? _debounceTimer;

    public CategoriesViewModel(
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "تصنيفات المنتجات";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "Categories");
            await LoadCategoriesAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadCategoriesAsync()
    {
        var filter = string.IsNullOrWhiteSpace(SearchText)
            ? null
            : SearchText.Trim();

        var (items, totalCount) = await _unitOfWork.Categories.GetPagedAsync(
            CurrentPage, PageSize,
            filter is null ? null : c => c.Name.Contains(filter),
            q => q.OrderByDescending(c => c.CreatedAt));

        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);
        if (TotalPages == 0) TotalPages = 1;

        Categories.Clear();
        foreach (var c in items)
            Categories.Add(c);
    }

    partial void OnSearchTextChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(400);
        _debounceTimer.Elapsed += async (_, _) =>
        {
            _debounceTimer?.Stop();
            CurrentPage = 1;
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadCategoriesAsync();
            });
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    [RelayCommand]
    private async Task FirstPage() { CurrentPage = 1; await LoadCategoriesAsync(); }

    [RelayCommand]
    private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadCategoriesAsync(); } }

    [RelayCommand]
    private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadCategoriesAsync(); } }

    [RelayCommand]
    private async Task LastPage() { CurrentPage = TotalPages; await LoadCategoriesAsync(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        SearchText = string.Empty;
        await LoadCategoriesAsync();
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _editingCategoryId = null;
        IsEditMode = false;
        DialogTitle = "إضافة تصنيف جديد";
        EditName = string.Empty;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Category category)
    {
        if (category is null) return;
        _editingCategoryId = category.Id;
        IsEditMode = true;
        DialogTitle = "تعديل التصنيف";
        EditName = category.Name;
        DialogError = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveCategory()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم التصنيف مطلوب";
            return;
        }

        DialogError = string.Empty;

        try
        {
            if (IsEditMode && _editingCategoryId.HasValue)
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(_editingCategoryId.Value);
                if (category is null) return;

                category.Name = EditName.Trim();
                category.UpdatedAt = DateTime.UtcNow;
                category.UpdatedBy = _currentUserService.Username;

                _unitOfWork.Categories.Update(category);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                var category = new Category
                {
                    Name = EditName.Trim(),
                    CreatedBy = _currentUserService.Username
                };

                await _unitOfWork.Categories.AddAsync(category);
                await _unitOfWork.SaveChangesAsync();
            }

            IsDialogOpen = false;
            await LoadCategoriesAsync();
        }
        catch (Exception ex)
        {
            DialogError = $"حدث خطأ: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private void ConfirmDelete(Category category)
    {
        if (category is null) return;
        CategoryToDelete = category;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private async Task ExecuteDelete()
    {
        if (CategoryToDelete is null) return;
        try
        {
            _unitOfWork.Categories.SoftDelete(CategoryToDelete, _currentUserService.Username);
            await _unitOfWork.SaveChangesAsync();
            IsDeleteDialogOpen = false;
            CategoryToDelete = null;
            await LoadCategoriesAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الحذف: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CancelDelete()
    {
        IsDeleteDialogOpen = false;
        CategoryToDelete = null;
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var (allItems, _) = await _unitOfWork.Categories.GetPagedAsync(1, int.MaxValue);
            var exportData = allItems.Select(c => new
            {
                الاسم = c.Name,
                تاريخ_الإنشاء = c.CreatedAt.ToString("yyyy/MM/dd")
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"التصنيفات_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "التصنيفات");
                BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء التصدير: {ex.Message}");
        }
    }
}
