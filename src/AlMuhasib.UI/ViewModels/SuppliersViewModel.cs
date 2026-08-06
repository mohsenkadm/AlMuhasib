using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.ViewModels;

public partial class SuppliersViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPreferencesService _userPreferences;

    public ObservableCollection<Supplier> Suppliers { get; } = [];

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
    private Supplier? _selectedSupplier;

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
    private string _editPhone = string.Empty;

    [ObservableProperty]
    private string _editAddress = string.Empty;

    [ObservableProperty]
    private string _editNotes = string.Empty;

    [ObservableProperty]
    private string _dialogError = string.Empty;

    // Delete confirmation
    [ObservableProperty]
    private bool _isDeleteDialogOpen;

    [ObservableProperty]
    private Supplier? _supplierToDelete;

    private int? _editingSupplierId;
    private System.Timers.Timer? _debounceTimer;

    public SuppliersViewModel(
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences,
        ICustomFieldSettingsService customFieldSettings)
    {
        _unitOfWork = unitOfWork;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _userPreferences = userPreferences;
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.Suppliers);
        PageTitle = "الموردون";
        ConfigureCustomFields(customFieldSettings);
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "Suppliers");
            await LoadCustomFieldDefinitionsAsync();
            await LoadSuppliersAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadSuppliersAsync()
    {
        var filter = string.IsNullOrWhiteSpace(SearchText)
            ? null
            : SearchText.Trim();

        System.Linq.Expressions.Expression<Func<Supplier, bool>>? searchPredicate = filter is null
            ? null
            : s => s.Name.Contains(filter) || (s.Phone != null && s.Phone.Contains(filter));

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
        {
            var (allItems, _) = await _unitOfWork.Suppliers.GetPagedAsync(
                1, int.MaxValue, searchPredicate, q => q.OrderByDescending(s => s.CreatedAt));

            var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
            MasterDataColumnFilterHelper.ApplyClientPagination(
                filtered, Suppliers, CurrentPage, PageSize,
                out var filteredTotal, out var filteredPages, out var filteredText);
            TotalCount = filteredTotal;
            TotalPages = filteredPages;
            PaginationText = filteredText;
            return;
        }

        var (items, totalCount) = await _unitOfWork.Suppliers.GetPagedAsync(
            CurrentPage, PageSize, searchPredicate, q => q.OrderByDescending(s => s.CreatedAt));

        TotalCount = totalCount;
        TotalPages = PaginationHelper.ComputeTotalPages(totalCount, PageSize);
        PaginationText = PaginationHelper.BuildPaginationText(totalCount, CurrentPage, PageSize);

        Suppliers.Clear();
        foreach (var s in items)
            Suppliers.Add(s);
    }

    protected override void OnColumnFiltersChanged()
    {
        CurrentPage = 1;
        _ = LoadSuppliersAsync();
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
                await LoadSuppliersAsync();
            });
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    [RelayCommand]
    private async Task FirstPage() { CurrentPage = 1; await LoadSuppliersAsync(); }

    [RelayCommand]
    private async Task PreviousPage() { if (CurrentPage > 1) { CurrentPage--; await LoadSuppliersAsync(); } }

    [RelayCommand]
    private async Task NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; await LoadSuppliersAsync(); } }

    [RelayCommand]
    private async Task LastPage() { CurrentPage = TotalPages; await LoadSuppliersAsync(); }

    [RelayCommand]
    private async Task Refresh()
    {
        CurrentPage = 1;
        SearchText = string.Empty;
        await LoadSuppliersAsync();
    }

    [RelayCommand]
    private async Task OpenAddDialog()
    {
        _editingSupplierId = null;
        IsEditMode = false;
        DialogTitle = "إضافة مورد جديد";
        EditName = string.Empty;
        EditPhone = string.Empty;
        EditAddress = string.Empty;
        EditNotes = string.Empty;
        DialogError = string.Empty;
        await ResetCustomFieldEditorsAsync(null);
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task OpenEditDialog(Supplier supplier)
    {
        if (supplier is null) return;
        _editingSupplierId = supplier.Id;
        IsEditMode = true;
        DialogTitle = "تعديل بيانات المورد";
        EditName = supplier.Name;
        EditPhone = supplier.Phone ?? string.Empty;
        EditAddress = supplier.Address ?? string.Empty;
        EditNotes = supplier.Notes ?? string.Empty;
        DialogError = string.Empty;
        await ResetCustomFieldEditorsAsync(supplier.CustomFieldsJson);
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveSupplier()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            DialogError = "اسم المورد مطلوب";
            return;
        }

        DialogError = string.Empty;

        try
        {
            if (IsEditMode && _editingSupplierId.HasValue)
            {
                var supplier = await _unitOfWork.Suppliers.GetByIdAsync(_editingSupplierId.Value);
                if (supplier is null) return;

                supplier.Name = EditName.Trim();
                supplier.Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim();
                supplier.Address = string.IsNullOrWhiteSpace(EditAddress) ? null : EditAddress.Trim();
                supplier.Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim();
                supplier.CustomFieldsJson = SerializeCustomFieldsFromEditors();
                supplier.UpdatedAt = DateTime.UtcNow;
                supplier.UpdatedBy = _currentUserService.Username;

                _unitOfWork.Suppliers.Update(supplier);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                var supplier = new Supplier
                {
                    Name = EditName.Trim(),
                    Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim(),
                    Address = string.IsNullOrWhiteSpace(EditAddress) ? null : EditAddress.Trim(),
                    Notes = string.IsNullOrWhiteSpace(EditNotes) ? null : EditNotes.Trim(),
                    CustomFieldsJson = SerializeCustomFieldsFromEditors(),
                    CreatedBy = _currentUserService.Username
                };

                await _unitOfWork.Suppliers.AddAsync(supplier);
                await _unitOfWork.SaveChangesAsync();
            }

            IsDialogOpen = false;
            await LoadSuppliersAsync();
        }
        catch (Exception ex)
        {
            DialogError = $"حدث خطأ: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CancelDialog() => IsDialogOpen = false;

    [RelayCommand]
    private void ConfirmDelete(Supplier supplier)
    {
        if (supplier is null) return;
        SupplierToDelete = supplier;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private async Task ExecuteDelete()
    {
        if (SupplierToDelete is null) return;
        try
        {
            _unitOfWork.Suppliers.SoftDelete(SupplierToDelete, _currentUserService.Username);
            await _unitOfWork.SaveChangesAsync();
            IsDeleteDialogOpen = false;
            SupplierToDelete = null;
            await LoadSuppliersAsync();
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
        SupplierToDelete = null;
    }

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            var (allItems, _) = await _unitOfWork.Suppliers.GetPagedAsync(1, int.MaxValue);
            var exportData = allItems.Select(s => new
            {
                الاسم = s.Name,
                الهاتف = s.Phone ?? "",
                العنوان = s.Address ?? "",
                ملاحظات = s.Notes ?? "",
                تاريخ_الإنشاء = s.CreatedAt.ToString("yyyy/MM/dd")
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"الموردون_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "الموردون");
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
            var (allItems, _) = await _unitOfWork.Suppliers.GetPagedAsync(1, int.MaxValue);
            var columns = new[] { "الاسم", "الهاتف", "العنوان", "ملاحظات", "تاريخ الإنشاء" };
            IList<object[]> rows = allItems.Select(s => new object[]
            {
                s.Name,
                s.Phone ?? "",
                s.Address ?? "",
                s.Notes ?? "",
                s.CreatedAt.ToString("yyyy/MM/dd")
            }).ToList();
            _exportService.PrintTable("قائمة الموردين", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.Suppliers, value);
}
