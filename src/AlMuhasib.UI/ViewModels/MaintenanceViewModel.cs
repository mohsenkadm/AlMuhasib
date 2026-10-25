using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace AlMuhasib.UI.ViewModels;

public partial class MaintenanceViewModel : ReportViewModelBase
{
    private readonly IMaintenanceService _maintenanceService;
    private readonly IProductService _productService;
    private int? _editingId;
    private int? _lastSavedId;

    // ── Report stats / charts / filters ───────────────────
    [ObservableProperty] private string _totalCount = "0";
    [ObservableProperty] private string _workingCount = "0";
    [ObservableProperty] private string _notWorkingCount = "0";
    [ObservableProperty] private string _underRepairCount = "0";
    [ObservableProperty] private string _deliveredCount = "0";
    [ObservableProperty] private string _totalCostAmount = "0";
    [ObservableProperty] private string _totalProfit = "0";

    [ObservableProperty] private Customer? _filterCustomer;
    [ObservableProperty] private string _filterCustomerSearchText = string.Empty;
    [ObservableProperty] private MaintenanceStatus? _selectedStatusFilter;
    [ObservableProperty] private string _productFilter = string.Empty;
    [ObservableProperty] private string _serialFilter = string.Empty;

    public ObservableCollection<Customer> FilterCustomers { get; } = [];
    public ObservableCollection<Customer> FilteredFilterCustomers { get; } = [];
    public IReadOnlyList<MaintenanceStatusOption> StatusFilterOptions { get; } = MaintenanceStatusHelper.FilterOptions;
    public IReadOnlyList<MaintenanceStatusOption> StatusOptions { get; } = MaintenanceStatusHelper.All;

    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];
    [ObservableProperty] private ISeries[] _statusSeries = [];
    [ObservableProperty] private Axis[] _statusXAxes = [];
    [ObservableProperty] private Axis[] _statusYAxes = [];

    private List<MaintenanceReportRow> _allRows = [];
    public ObservableCollection<MaintenanceReportRow> Rows { get; } = [];

    // ── Form dialog ───────────────────────────────────────
    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Customer> FilteredCustomers { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<Product> FilteredProducts { get; } = [];
    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    [ObservableProperty] private bool _isFormDialogOpen;
    [ObservableProperty] private string _formDialogTitle = "صيانة جهاز جديد";
    [ObservableProperty] private string _recordNumber = string.Empty;
    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _customerSearchText = string.Empty;
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private string _productSearchText = string.Empty;
    [ObservableProperty] private string _productName = string.Empty;
    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private string _previousSerialNumber = string.Empty;
    [ObservableProperty] private decimal _costAmount;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private MaintenanceStatus _status = MaintenanceStatus.UnderRepair;
    [ObservableProperty] private DateTime? _warrantyEndDate;
    [ObservableProperty] private CashBox? _selectedCashBox;
    [ObservableProperty] private string _formError = string.Empty;
    [ObservableProperty] private bool _isDailyChartEmpty = true;
    [ObservableProperty] private bool _isStatusChartEmpty = true;

    [ObservableProperty] private bool _isQuickAddCustomerOpen;
    [ObservableProperty] private string _quickCustomerName = string.Empty;
    [ObservableProperty] private string _quickCustomerPhone = string.Empty;
    [ObservableProperty] private string _quickCustomerAddress = string.Empty;
    [ObservableProperty] private string _quickCustomerError = string.Empty;

    [ObservableProperty] private bool _isReplaceSerialOpen;
    [ObservableProperty] private int? _replaceSerialTargetId;
    [ObservableProperty] private string _replaceSerialTargetNumber = string.Empty;
    [ObservableProperty] private string _replaceCurrentSerial = string.Empty;
    [ObservableProperty] private string _replaceNewSerial = string.Empty;
    [ObservableProperty] private string _replaceSerialError = string.Empty;

    public MaintenanceViewModel(
        IMaintenanceService maintenanceService,
        IProductService productService,
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        _maintenanceService = maintenanceService;
        _productService = productService;
        PageTitle = "صيانة الأجهزة";
        DateFrom = DateTime.Today.AddMonths(-1);
        DateTo = DateTime.Today;
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, ScreenPermissionRegistry.Maintenance);
        await LoadLookupsAsync();
        await LoadDataAsync();
    }

    private async Task LoadLookupsAsync()
    {
        Customers.Clear();
        FilteredCustomers.Clear();
        FilterCustomers.Clear();
        FilteredFilterCustomers.Clear();
        foreach (var c in (await _unitOfWork.Customers.GetAllAsync()).OrderBy(c => c.Name))
        {
            Customers.Add(c);
            FilteredCustomers.Add(c);
            FilterCustomers.Add(c);
            FilteredFilterCustomers.Add(c);
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

    // ── Filter bar bindings ───────────────────────────────
    partial void OnFilterCustomerChanged(Customer? value)
    {
        if (value is not null)
            FilterCustomerSearchText = value.Name;
    }

    partial void OnFilterCustomerSearchTextChanged(string value)
    {
        if (FilterCustomer is not null && FilterCustomer.Name == value)
            return;
        FilterCustomer = null;
        CustomerComboBoxFilter.Apply(FilterCustomers, FilteredFilterCustomers, value);
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

    // ── Report load / export / print ──────────────────────
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetMaintenanceReportAsync(
                DateFrom, DateTo, FilterCustomer?.Id, SelectedStatusFilter,
                string.IsNullOrWhiteSpace(ProductFilter) ? null : ProductFilter.Trim(),
                string.IsNullOrWhiteSpace(SerialFilter) ? null : SerialFilter.Trim());

            TotalCount = result.TotalCount.ToString("N0");
            WorkingCount = result.WorkingCount.ToString("N0");
            NotWorkingCount = result.NotWorkingCount.ToString("N0");
            UnderRepairCount = result.UnderRepairCount.ToString("N0");
            DeliveredCount = result.DeliveredCount.ToString("N0");
            TotalCostAmount = FormatCurrency(result.TotalCostAmount);
            TotalProfit = FormatCurrency(result.TotalProfit);

            IsDailyChartEmpty = result.DailyChart.Count == 0;
            if (!IsDailyChartEmpty)
            {
                DailySeries = [ChartThemeConfig.Line(result.DailyChart.Select(d => d.Amount).ToArray(), "تكاليف الصيانة", 0)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            else
            {
                DailySeries = [];
                DailyXAxes = [];
                DailyYAxes = [];
            }

            var statusValues = result.StatusChart.Select(s => s.Amount).ToArray();
            IsStatusChartEmpty = statusValues.Length == 0 || statusValues.All(v => v <= 0);
            if (!IsStatusChartEmpty)
            {
                StatusSeries = [ChartThemeConfig.Column(statusValues, "حسب الحالة", 1)];
                StatusXAxes = [ChartThemeConfig.CreateXAxis(result.StatusChart.Select(s => s.Name).ToArray())];
                StatusYAxes = [ChartThemeConfig.CreateYAxis(suffix: "")];
            }
            else
            {
                StatusSeries = [];
                StatusXAxes = [];
                StatusYAxes = [];
            }

            _allRows = result.Rows;
            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "صيانة_الأجهزة.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "الرقم", "التاريخ", "الزبون", "المنتج", "سيريال السابق", "سيريال الجديد", "المبلغ", "الحالة", "انتهاء الضمان", "حالة الضمان", "المتبقي", "ملاحظات" };
        var rows = _allRows.Select(r => new object[]
        {
            r.RecordNumber, r.Date.ToString("yyyy/MM/dd"), r.CustomerName, r.ProductName,
            r.PreviousSerialNumber ?? "", r.SerialNumber,
            r.CostAmount, r.StatusLabel, r.WarrantyEndDate?.ToString("yyyy/MM/dd") ?? "",
            r.WarrantyStatusLabel, r.WarrantyRemainingLabel, r.Notes ?? ""
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "الصيانة", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "الرقم", "التاريخ", "الزبون", "المنتج", "سيريال السابق", "سيريال الجديد", "المبلغ", "الحالة", "الضمان" };
        var rows = _allRows.Select(r => new object[]
        {
            r.RecordNumber, r.Date.ToString("yyyy/MM/dd"), r.CustomerName, r.ProductName,
            r.PreviousSerialNumber ?? "", r.SerialNumber,
            r.CostAmount.ToString("N0"), r.StatusLabel, WarrantyStatusHelper.CombinedLabel(r.WarrantyEndDate)
        }).ToList();
        _exportService.PrintTable("صيانة الأجهزة", cols, rows);
    }

    [RelayCommand]
    private void PrintReceipt(MaintenanceReportRow? row)
    {
        if (row is null) return;
        var columns = new[] { "الحقل", "القيمة" };
        var rows = new List<object[]>
        {
            new object[] { "رقم السجل", row.RecordNumber },
            new object[] { "التاريخ", row.Date.ToString("yyyy/MM/dd") },
            new object[] { "الزبون", row.CustomerName },
            new object[] { "المنتج", row.ProductName },
            new object[] { "سيريال الجهاز", row.SerialNumber },
        };
        if (!string.IsNullOrWhiteSpace(row.PreviousSerialNumber))
            rows.Add(new object[] { "السيريال السابق", row.PreviousSerialNumber });
        rows.Add(new object[] { "المبلغ", row.CostAmount.ToString("N0") });
        rows.Add(new object[] { "الحالة", row.StatusLabel });
        if (row.WarrantyEndDate.HasValue)
        {
            rows.Add(new object[] { "تاريخ الضمان", row.WarrantyEndDate.Value.ToString("yyyy/MM/dd") });
            rows.Add(new object[] { "حالة الضمان", row.WarrantyStatusLabel });
            rows.Add(new object[] { "المتبقي", row.WarrantyRemainingLabel });
        }
        if (!string.IsNullOrWhiteSpace(row.Notes))
            rows.Add(new object[] { "ملاحظات", row.Notes });

        _exportService.PrintTable($"وصل صيانة — {row.RecordNumber}", columns, rows);
    }

    [RelayCommand]
    private async Task PrintReceiptFromFormAsync()
    {
        var id = _editingId ?? _lastSavedId;
        if (id is int recordId)
        {
            var existing = _allRows.FirstOrDefault(r => r.Id == recordId);
            if (existing is not null)
            {
                PrintReceipt(existing);
                return;
            }

            var full = await _maintenanceService.GetByIdAsync(recordId);
            if (full is not null)
            {
                PrintReceipt(ToReportRow(full));
                return;
            }
        }

        FormError = "احفظ السجل أولاً ثم اطبع الوصل";
    }

    private static MaintenanceReportRow ToReportRow(MaintenanceRecord m) => new()
    {
        Id = m.Id,
        RecordNumber = m.RecordNumber,
        Date = m.Date,
        CustomerName = m.Customer?.Name ?? "—",
        ProductName = m.ProductName,
        SerialNumber = m.SerialNumber,
        PreviousSerialNumber = m.PreviousSerialNumber,
        CostAmount = m.CostAmount,
        Status = m.Status,
        StatusLabel = MaintenanceStatusHelper.ToArabic(m.Status),
        WarrantyEndDate = m.WarrantyEndDate,
        WarrantyStatusLabel = WarrantyStatusHelper.StatusLabel(m.WarrantyEndDate),
        WarrantyRemainingLabel = WarrantyStatusHelper.RemainingLabel(m.WarrantyEndDate),
        Notes = m.Notes,
        Currency = m.Currency
    };

    // ── Form dialog ───────────────────────────────────────
    [RelayCommand]
    private async Task OpenNewRecordAsync()
    {
        await ResetFormAsync();
        FormDialogTitle = "صيانة جهاز جديد";
        IsFormDialogOpen = true;
    }

    [RelayCommand]
    private void CancelFormDialog()
    {
        IsFormDialogOpen = false;
        FormError = string.Empty;
        _editingId = null;
    }

    private async Task ResetFormAsync()
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
        PreviousSerialNumber = string.Empty;
        CostAmount = 0;
        Notes = string.Empty;
        Status = MaintenanceStatus.UnderRepair;
        WarrantyEndDate = null;
        FormError = string.Empty;
        SelectedCashBox ??= CashBoxes.FirstOrDefault();
        FormDialogTitle = "صيانة جهاز جديد";
    }

    [RelayCommand]
    private async Task EditRecord(MaintenanceReportRow? row)
    {
        if (row is null) return;
        var full = await _maintenanceService.GetByIdAsync(row.Id);
        if (full is null)
        {
            BeautifulMessageDialog.ShowError("سجل الصيانة غير موجود");
            return;
        }

        _editingId = full.Id;
        RecordNumber = full.RecordNumber;
        Date = full.Date;
        SelectedCustomer = Customers.FirstOrDefault(c => c.Id == full.CustomerId);
        CustomerSearchText = SelectedCustomer?.Name ?? full.Customer?.Name ?? string.Empty;
        SelectedProduct = full.ProductId is int pid ? Products.FirstOrDefault(p => p.Id == pid) : null;
        ProductName = full.ProductName;
        ProductSearchText = full.ProductName;
        SerialNumber = full.SerialNumber;
        PreviousSerialNumber = full.PreviousSerialNumber ?? string.Empty;
        CostAmount = full.CostAmount;
        Notes = full.Notes ?? string.Empty;
        Status = full.Status;
        WarrantyEndDate = full.WarrantyEndDate;
        SelectedCashBox = full.CashBoxId is int cid
            ? CashBoxes.FirstOrDefault(c => c.Id == cid)
            : SelectedCashBox;
        FormError = string.Empty;
        FormDialogTitle = $"تعديل سجل الصيانة {full.RecordNumber}";
        IsFormDialogOpen = true;
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

        var name = (string.IsNullOrWhiteSpace(ProductName) ? ProductSearchText : ProductName)?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            FormError = "أدخل اسم المنتج";
            return;
        }

        try
        {
            IsBusy = true;
            var product = await EnsureProductExistsAsync(name);
            var wasNew = _editingId is null;

            var record = new MaintenanceRecord
            {
                Id = _editingId ?? 0,
                RecordNumber = RecordNumber,
                Date = Date,
                CustomerId = SelectedCustomer.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                SerialNumber = SerialNumber?.Trim() ?? string.Empty,
                PreviousSerialNumber = string.IsNullOrWhiteSpace(PreviousSerialNumber)
                    ? null
                    : PreviousSerialNumber.Trim(),
                CostAmount = CostAmount,
                Notes = Notes,
                Status = Status,
                WarrantyEndDate = WarrantyEndDate,
                CashBoxId = SelectedCashBox?.Id,
                Currency = SelectedCashBox?.Currency ?? AccountingCurrency.IQD,
                FxRate = 1m
            };

            var saved = await _maintenanceService.SaveAsync(record);
            _lastSavedId = saved.Id;
            BeautifulMessageDialog.ShowSuccess($"تم حفظ سجل الصيانة {saved.RecordNumber}");
            await LoadDataAsync();

            // يبقى الدايلوك مفتوحاً بعد الإضافة ويستعد لسجل جديد
            if (wasNew)
                await ResetFormAsync();
            else
            {
                _editingId = saved.Id;
                FormDialogTitle = $"تعديل سجل الصيانة {saved.RecordNumber}";
                PreviousSerialNumber = saved.PreviousSerialNumber ?? string.Empty;
                SerialNumber = saved.SerialNumber;
            }

            IsFormDialogOpen = true;
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

    private async Task<Product> EnsureProductExistsAsync(string name)
    {
        var existing = SelectedProduct
            ?? Products.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            return existing;

        var categories = (await _unitOfWork.Categories.GetAllAsync()).ToList();
        var defaultCategory = categories.FirstOrDefault(c =>
                c.Name.Equals("عام", StringComparison.OrdinalIgnoreCase))
            ?? categories.FirstOrDefault();

        if (defaultCategory is null)
        {
            defaultCategory = new Category
            {
                Name = "عام",
                CreatedBy = _currentUserService.Username,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Categories.AddAsync(defaultCategory);
            await _unitOfWork.SaveChangesAsync();
        }

        var created = await _productService.CreateAsync(new Product
        {
            Name = name,
            CategoryId = defaultCategory.Id,
            ShowInCashier = true
        });

        Products.Add(created);
        if (!FilteredProducts.Any(p => p.Id == created.Id))
            FilteredProducts.Add(created);
        SelectedProduct = created;
        ProductName = created.Name;
        ProductSearchText = created.Name;
        return created;
    }

    [RelayCommand]
    private async Task DeleteRecord(MaintenanceReportRow? row)
    {
        if (row is null) return;
        if (!BeautifulMessageDialog.ShowConfirm($"حذف سجل الصيانة {row.RecordNumber}؟", "تأكيد الحذف"))
            return;

        try
        {
            await _maintenanceService.DeleteAsync(row.Id);
            if (_editingId == row.Id)
                CancelFormDialog();
            await LoadDataAsync();
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
    private void OpenReplaceSerial(MaintenanceReportRow? row)
    {
        if (row is null) return;
        ReplaceSerialTargetId = row.Id;
        ReplaceSerialTargetNumber = row.RecordNumber;
        ReplaceCurrentSerial = row.SerialNumber ?? string.Empty;
        ReplaceNewSerial = string.Empty;
        ReplaceSerialError = string.Empty;
        IsReplaceSerialOpen = true;
    }

    [RelayCommand]
    private void CancelReplaceSerial()
    {
        IsReplaceSerialOpen = false;
        ReplaceSerialTargetId = null;
        ReplaceSerialTargetNumber = string.Empty;
        ReplaceNewSerial = string.Empty;
        ReplaceSerialError = string.Empty;
    }

    [RelayCommand]
    private async Task ConfirmReplaceSerialAsync()
    {
        if (ReplaceSerialTargetId is not int id) return;
        ReplaceSerialError = string.Empty;
        if (string.IsNullOrWhiteSpace(ReplaceNewSerial))
        {
            ReplaceSerialError = "أدخل السيريال الجديد";
            return;
        }

        try
        {
            IsBusy = true;
            var saved = await _maintenanceService.ReplaceSerialAsync(id, ReplaceNewSerial);
            IsReplaceSerialOpen = false;
            ReplaceSerialTargetId = null;
            if (_editingId == saved.Id)
            {
                SerialNumber = saved.SerialNumber;
                PreviousSerialNumber = saved.PreviousSerialNumber ?? string.Empty;
            }
            await LoadDataAsync();
            BeautifulMessageDialog.ShowSuccess("تم استبدال السيريال");
        }
        catch (Exception ex)
        {
            ReplaceSerialError = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

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
