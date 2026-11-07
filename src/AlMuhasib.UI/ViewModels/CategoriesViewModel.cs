using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.ViewModels;

public partial class CategoriesViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPreferencesService _userPreferences;
    private readonly IBranchService _branchService;
    private readonly ICategoryService _categoryService;

    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<ProductBranchOption> BranchOptions { get; } = [];

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
    private string _paginationText = string.Empty;

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private bool _isCardView;

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

    [ObservableProperty]
    private string _selectedBranchesSummary = string.Empty;

    [ObservableProperty]
    private bool _isBranchPickerOpen;

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
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences,
        IBranchService branchService,
        ICategoryService categoryService)
    {
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _userPreferences = userPreferences;
        _branchService = branchService;
        _categoryService = categoryService;
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.Categories);
        PageTitle = "تصنيفات المنتجات";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "Categories");
            await LoadBranchesAsync();
            await LoadCategoriesAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadBranchesAsync()
    {
        BranchOptions.Clear();
        var branches = await _branchService.GetActiveAsync();
        foreach (var b in branches.OrderByDescending(x => x.IsMain).ThenBy(x => x.Name))
        {
            var opt = new ProductBranchOption
            {
                BranchId = b.Id,
                Name = b.Name,
                Code = b.Code,
                IsMain = b.IsMain
            };
            opt.SelectionChanged += RefreshSelectedBranchesSummary;
            BranchOptions.Add(opt);
        }
    }

    private void RefreshSelectedBranchesSummary()
    {
        var selected = BranchOptions.Where(o => o.IsSelected).ToList();
        SelectedBranchesSummary = selected.Count == 0
            ? "لم يُختر أي فرع"
            : string.Join(" · ", selected.Select(o => o.Name));
    }

    private void ResetBranchSelectionForNew()
    {
        foreach (var opt in BranchOptions)
            opt.SetSelectedSilent(opt.IsMain || BranchOptions.Count == 1);
        if (!BranchOptions.Any(o => o.IsSelected) && BranchOptions.Count > 0)
            BranchOptions[0].SetSelectedSilent(true);
        RefreshSelectedBranchesSummary();
    }

    private async Task ApplyBranchSelectionForEditAsync(int categoryId)
    {
        var ids = await _categoryService.GetBranchIdsForCategoryAsync(categoryId);
        var idSet = ids.ToHashSet();
        foreach (var opt in BranchOptions)
            opt.SetSelectedSilent(idSet.Contains(opt.BranchId));
        if (!BranchOptions.Any(o => o.IsSelected))
            ResetBranchSelectionForNew();
        else
            RefreshSelectedBranchesSummary();
    }

    private async Task LoadCategoriesAsync()
    {
        var filter = string.IsNullOrWhiteSpace(SearchText)
            ? null
            : SearchText.Trim();

        System.Linq.Expressions.Expression<Func<Category, bool>>? searchPredicate = filter is null
            ? null
            : c => c.Name.Contains(filter);

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
        {
            var (allItems, _) = await _unitOfWork.Categories.GetPagedAsync(
                1, int.MaxValue, searchPredicate, q => q.OrderByDescending(c => c.CreatedAt));

            var allList = allItems as IList<Category> ?? allItems.ToList();
            await ApplyBranchNamesAsync(allList);

            var filtered = ColumnFilterEngine.Apply(allList, ColumnFilters).ToList();
            MasterDataColumnFilterHelper.ApplyClientPagination(
                filtered, Categories, CurrentPage, PageSize,
                out var filteredTotal, out var filteredPages, out var filteredText);
            TotalCount = filteredTotal;
            TotalPages = filteredPages;
            PaginationText = filteredText;
            return;
        }

        var (items, totalCount) = await _unitOfWork.Categories.GetPagedAsync(
            CurrentPage, PageSize, searchPredicate, q => q.OrderByDescending(c => c.CreatedAt));

        TotalCount = totalCount;
        TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
        PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

        var pageItems = items as IList<Category> ?? items.ToList();
        await ApplyBranchNamesAsync(pageItems);

        Categories.Clear();
        foreach (var c in pageItems)
            Categories.Add(c);
    }

    private async Task ApplyBranchNamesAsync(IEnumerable<Category> categories)
    {
        var list = categories as IList<Category> ?? categories.ToList();
        if (list.Count == 0) return;

        try
        {
            var map = await _categoryService.GetBranchNamesByCategoryIdsAsync(list.Select(c => c.Id));
            foreach (var c in list)
                c.BranchesDisplay = map.TryGetValue(c.Id, out var names) && !string.IsNullOrWhiteSpace(names)
                    ? names
                    : "—";
        }
        catch
        {
            foreach (var c in list)
                c.BranchesDisplay = "—";
        }
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        _ = LoadCategoriesAsync();
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
        ResetBranchSelectionForNew();
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task OpenEditDialog(Category category)
    {
        if (category is null) return;
        _editingCategoryId = category.Id;
        IsEditMode = true;
        DialogTitle = "تعديل التصنيف";
        EditName = category.Name;
        DialogError = string.Empty;
        await ApplyBranchSelectionForEditAsync(category.Id);
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenBranchPicker() => IsBranchPickerOpen = true;

    [RelayCommand]
    private void CloseBranchPicker()
    {
        if (!BranchOptions.Any(o => o.IsSelected))
        {
            DialogError = "يجب اختيار فرع واحد على الأقل";
            return;
        }
        DialogError = string.Empty;
        IsBranchPickerOpen = false;
    }

    [RelayCommand]
    private async Task SaveCategory()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم التصنيف مطلوب";
            return;
        }

        var selectedBranchIds = BranchOptions.Where(o => o.IsSelected).Select(o => o.BranchId).ToList();
        if (selectedBranchIds.Count == 0)
        {
            DialogError = "يجب اختيار فرع واحد على الأقل";
            return;
        }

        DialogError = string.Empty;

        try
        {
            if (IsEditMode && _editingCategoryId.HasValue)
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(_editingCategoryId.Value);
                if (category is null) return;

                var name = EditName.Trim();
                if (await _unitOfWork.Categories.AnyAsync(c => c.Name == name && c.Id != category.Id))
                {
                    DialogError = "اسم التصنيف موجود مسبقاً";
                    return;
                }

                category.Name = name;
                category.UpdatedAt = DateTime.UtcNow;
                category.UpdatedBy = _currentUserService.Username;

                _unitOfWork.Categories.Update(category);
                await _unitOfWork.SaveChangesAsync();
                await _categoryService.SetCategoryBranchesAsync(category.Id, selectedBranchIds);
            }
            else
            {
                var name = EditName.Trim();
                if (await _unitOfWork.Categories.AnyAsync(c => c.Name == name))
                {
                    DialogError = "اسم التصنيف موجود مسبقاً";
                    return;
                }

                int categoryId;
                var softDeleted = await _unitOfWork.Categories.FindSoftDeletedFirstAsync(c => c.Name == name);
                if (softDeleted is not null)
                {
                    softDeleted.RestoreFromSoftDelete(_currentUserService.Username);
                    softDeleted.Name = name;
                    _unitOfWork.Categories.Update(softDeleted);
                    await _unitOfWork.SaveChangesAsync();
                    categoryId = softDeleted.Id;
                }
                else
                {
                    var category = new Category
                    {
                        Name = name,
                        CreatedBy = _currentUserService.Username
                    };

                    await _unitOfWork.Categories.AddAsync(category);
                    await _unitOfWork.SaveChangesAsync();
                    categoryId = category.Id;
                }

                await _categoryService.SetCategoryBranchesAsync(categoryId, selectedBranchIds);
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
            var allList = allItems as IList<Category> ?? allItems.ToList();
            await ApplyBranchNamesAsync(allList);
            var exportData = allList.Select(c => new
            {
                الاسم = c.Name,
                الفروع = c.BranchesDisplay,
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

    [RelayCommand]
    private async Task PrintTable()
    {
        try
        {
            var (allItems, _) = await _unitOfWork.Categories.GetPagedAsync(1, int.MaxValue);
            var allList = allItems as IList<Category> ?? allItems.ToList();
            await ApplyBranchNamesAsync(allList);
            var columns = new[] { "الاسم", "الفروع", "تاريخ الإنشاء" };
            IList<object[]> rows = allList.Select(c => new object[]
            {
                c.Name,
                c.BranchesDisplay,
                c.CreatedAt.ToString("yyyy/MM/dd")
            }).ToList();
            _exportService.PrintTable("قائمة التصنيفات", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.Categories, value);
}
