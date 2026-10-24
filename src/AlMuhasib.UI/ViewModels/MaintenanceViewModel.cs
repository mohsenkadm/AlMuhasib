using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class MaintenanceViewModel : ViewModelBase
{
    private readonly IMaintenanceService _maintenanceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private int? _editingId;

    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Customer> FilteredCustomers { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<Product> FilteredProducts { get; } = [];
    public ObservableCollection<CashBox> CashBoxes { get; } = [];
    public ObservableCollection<MaintenanceRecord> RecentRecords { get; } = [];
    public IReadOnlyList<MaintenanceStatusOption> StatusOptions { get; } = MaintenanceStatusHelper.All;

    [ObservableProperty] private string _recordNumber = string.Empty;
    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _customerSearchText = string.Empty;
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private string _productSearchText = string.Empty;
    [ObservableProperty] private string _productName = string.Empty;
    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private decimal _costAmount;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private MaintenanceStatus _status = MaintenanceStatus.UnderRepair;
    [ObservableProperty] private DateTime? _warrantyEndDate;
    [ObservableProperty] private CashBox? _selectedCashBox;
    [ObservableProperty] private bool _showWarrantyFields;
    [ObservableProperty] private string _formError = string.Empty;

    [ObservableProperty] private bool _isQuickAddCustomerOpen;
    [ObservableProperty] private string _quickCustomerName = string.Empty;
    [ObservableProperty] private string _quickCustomerPhone = string.Empty;
    [ObservableProperty] private string _quickCustomerAddress = string.Empty;
    [ObservableProperty] private string _quickCustomerError = string.Empty;

    public MaintenanceViewModel(
        IMaintenanceService maintenanceService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _maintenanceService = maintenanceService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        PageTitle = "صيانة الأجهزة";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, ScreenPermissionRegistry.Maintenance);
        await LoadLookupsAsync();
        await LoadRecentAsync();
        await NewRecordAsync();
    }

    private async Task LoadLookupsAsync()
    {
        Customers.Clear();
        FilteredCustomers.Clear();
        foreach (var c in (await _unitOfWork.Customers.GetAllAsync()).OrderBy(c => c.Name))
        {
            Customers.Add(c);
            FilteredCustomers.Add(c);
        }

        Products.Clear();
        FilteredProducts.Clear();
        foreach (var p in (await _unitOfWork.Products.GetAllAsync()).OrderBy(p => p.Name))
        {
            Products.Add(p);
            FilteredProducts.Add(p);
        }

        CashBoxes.Clear();
        foreach (var cb in await _unitOfWork.CashBoxes.GetAllAsync())
            CashBoxes.Add(cb);
        SelectedCashBox ??= CashBoxes.FirstOrDefault();
    }

    private async Task LoadRecentAsync()
    {
        RecentRecords.Clear();
        foreach (var r in await _maintenanceService.GetRecentAsync(40))
            RecentRecords.Add(r);
    }

    partial void OnStatusChanged(MaintenanceStatus value)
    {
        ShowWarrantyFields = value == MaintenanceStatus.Delivered;
        if (!ShowWarrantyFields)
            WarrantyEndDate = null;
    }

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value is not null)
            CustomerSearchText = value.Name;
    }

    partial void OnCustomerSearchTextChanged(string value)
    {
        if (SelectedCustomer is not null && SelectedCustomer.Name == value)
            return;
        SelectedCustomer = null;
        CustomerComboBoxFilter.Apply(Customers, FilteredCustomers, value);
    }

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value is null) return;
        ProductName = value.Name;
        ProductSearchText = value.Name;
    }

    partial void OnProductSearchTextChanged(string value)
    {
        if (SelectedProduct is not null && SelectedProduct.Name == value)
            return;
        SelectedProduct = null;
        if (string.IsNullOrWhiteSpace(ProductName) || ProductName == ProductSearchText)
            ProductName = value ?? string.Empty;

        FilteredProducts.Clear();
        var term = value?.Trim() ?? string.Empty;
        var source = string.IsNullOrEmpty(term)
            ? Products.AsEnumerable()
            : Products.Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                                  || (!string.IsNullOrEmpty(p.Barcode) && p.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase)));
        foreach (var p in source.Take(80))
            FilteredProducts.Add(p);
    }

    [RelayCommand]
    private async Task NewRecordAsync()
    {
        _editingId = null;
        RecordNumber = await _maintenanceService.GetNextRecordNumberAsync();
        Date = DateTime.Today;
        SelectedCustomer = null;
        CustomerSearchText = string.Empty;
        SelectedProduct = null;
        ProductSearchText = string.Empty;
        ProductName = string.Empty;
        SerialNumber = string.Empty;
        CostAmount = 0;
        Notes = string.Empty;
        Status = MaintenanceStatus.UnderRepair;
        WarrantyEndDate = null;
        ShowWarrantyFields = false;
        FormError = string.Empty;
        SelectedCashBox ??= CashBoxes.FirstOrDefault();
    }

    [RelayCommand]
    private async Task EditRecord(MaintenanceRecord? record)
    {
        if (record is null) return;
        var full = await _maintenanceService.GetByIdAsync(record.Id) ?? record;
        _editingId = full.Id;
        RecordNumber = full.RecordNumber;
        Date = full.Date;
        SelectedCustomer = Customers.FirstOrDefault(c => c.Id == full.CustomerId);
        CustomerSearchText = SelectedCustomer?.Name ?? full.Customer?.Name ?? string.Empty;
        SelectedProduct = full.ProductId is int pid ? Products.FirstOrDefault(p => p.Id == pid) : null;
        ProductName = full.ProductName;
        ProductSearchText = full.ProductName;
        SerialNumber = full.SerialNumber;
        CostAmount = full.CostAmount;
        Notes = full.Notes ?? string.Empty;
        Status = full.Status;
        WarrantyEndDate = full.WarrantyEndDate;
        ShowWarrantyFields = full.Status == MaintenanceStatus.Delivered;
        SelectedCashBox = full.CashBoxId is int cid
            ? CashBoxes.FirstOrDefault(c => c.Id == cid)
            : SelectedCashBox;
        FormError = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        FormError = string.Empty;
        if (SelectedCustomer is null)
        {
            FormError = "اختر الزبون";
            return;
        }

        var name = string.IsNullOrWhiteSpace(ProductName) ? ProductSearchText : ProductName;
        if (string.IsNullOrWhiteSpace(name))
        {
            FormError = "أدخل اسم المنتج";
            return;
        }

        try
        {
            IsBusy = true;
            var record = new MaintenanceRecord
            {
                Id = _editingId ?? 0,
                RecordNumber = RecordNumber,
                Date = Date,
                CustomerId = SelectedCustomer.Id,
                ProductId = SelectedProduct?.Id,
                ProductName = name.Trim(),
                SerialNumber = SerialNumber?.Trim() ?? string.Empty,
                CostAmount = CostAmount,
                Notes = Notes,
                Status = Status,
                WarrantyEndDate = ShowWarrantyFields ? WarrantyEndDate : null,
                CashBoxId = SelectedCashBox?.Id,
                Currency = SelectedCashBox?.Currency ?? AccountingCurrency.IQD,
                FxRate = 1m
            };

            var saved = await _maintenanceService.SaveAsync(record);
            BeautifulMessageDialog.ShowSuccess($"تم حفظ سجل الصيانة {saved.RecordNumber}");
            await LoadRecentAsync();
            await NewRecordAsync();
        }
        catch (Exception ex)
        {
            FormError = ex.Message;
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteRecord(MaintenanceRecord? record)
    {
        if (record is null) return;
        if (!BeautifulMessageDialog.ShowConfirm($"حذف سجل الصيانة {record.RecordNumber}؟", "تأكيد الحذف"))
            return;

        try
        {
            await _maintenanceService.DeleteAsync(record.Id);
            if (_editingId == record.Id)
                await NewRecordAsync();
            await LoadRecentAsync();
            BeautifulMessageDialog.ShowSuccess("تم الحذف");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenQuickAddCustomer()
    {
        QuickCustomerName = string.Empty;
        QuickCustomerPhone = string.Empty;
        QuickCustomerAddress = string.Empty;
        QuickCustomerError = string.Empty;
        IsQuickAddCustomerOpen = true;
    }

    [RelayCommand]
    private void CancelQuickAddCustomer() => IsQuickAddCustomerOpen = false;

    [RelayCommand]
    private async Task SaveQuickCustomer()
    {
        if (string.IsNullOrWhiteSpace(QuickCustomerName))
        {
            QuickCustomerError = "اسم العميل مطلوب";
            return;
        }

        try
        {
            var newCustomer = new Customer
            {
                Name = QuickCustomerName.Trim(),
                Phone = string.IsNullOrWhiteSpace(QuickCustomerPhone) ? null : QuickCustomerPhone.Trim(),
                Address = string.IsNullOrWhiteSpace(QuickCustomerAddress) ? null : QuickCustomerAddress.Trim(),
                CreatedBy = _currentUserService.Username,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Customers.AddAsync(newCustomer);
            await _unitOfWork.SaveChangesAsync();
            await LoadLookupsAsync();
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == newCustomer.Id);
            IsQuickAddCustomerOpen = false;
            BeautifulMessageDialog.ShowSuccess($"تم إضافة العميل '{newCustomer.Name}' بنجاح");
        }
        catch (Exception ex)
        {
            QuickCustomerError = $"خطأ: {ex.Message}";
        }
    }
}
