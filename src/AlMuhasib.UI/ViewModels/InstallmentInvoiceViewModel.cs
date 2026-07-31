using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Shared.Services;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class InstallmentInvoiceViewModel : ViewModelBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IInstallmentService _installmentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly INavigationService _navigationService;
    private readonly IExportService _exportService;
    private readonly IWhatsAppShareService _whatsAppShare;
    private readonly IFeatureFlagService _featureFlags;

    private Invoice? _savedInvoice;
    private List<InvoiceItem> _savedItems = [];
    private InstallmentPlan? _savedPlan;

    // ── Header ─────────────────────────────────────────────
    [ObservableProperty]
    private string _invoiceNumber = string.Empty;

    [ObservableProperty]
    private DateTime _invoiceDate = DateTime.Now;

    // ── Installment Type ───────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCompanyFee))]
    private InstallmentType _selectedInstallmentType = InstallmentType.Manual;

    /// <summary>نسبة الشركة تُحسب فقط لنوع القسط «منصة»</summary>
    public bool ShowCompanyFee => SelectedInstallmentType == InstallmentType.Platform;

    // Customer
    [ObservableProperty]
    private string _customerSearchText = string.Empty;

    [ObservableProperty]
    private Customer? _selectedCustomer;

    public ObservableCollection<Customer> Customers { get; } = [];
    public ObservableCollection<Customer> FilteredCustomers { get; } = [];

    // Warehouse
    [ObservableProperty]
    private Warehouse? _selectedWarehouse;

    public ObservableCollection<Warehouse> Warehouses { get; } = [];

    // CashBox (not used for payment, but for system reference)
    [ObservableProperty]
    private CashBox? _selectedCashBox;

    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    // ── Items ──────────────────────────────────────────────
    public ObservableCollection<InvoiceItemRow> Items { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];

    public ProductPickerViewModel ProductPicker { get; }

    [ObservableProperty]
    private bool _isProductPickerOpen;

    [ObservableProperty]
    private string _barcodeInput = string.Empty;

    // ── Installment Plan Fields ────────────────────────────
    [ObservableProperty]
    private int _numberOfInstallments = 6;

    [ObservableProperty]
    private DateTime _installmentStartDate = DateTime.Now.AddMonths(1);

    [ObservableProperty]
    private string _fileNumber = string.Empty;

    [ObservableProperty]
    private decimal _installmentAmount;

    public ObservableCollection<InstallmentScheduleRow> SchedulePreview { get; } = [];

    // ── Totals ─────────────────────────────────────────────
    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _invoiceDiscountAmount;

    [ObservableProperty]
    private DiscountType _invoiceDiscountType = DiscountType.None;

    [ObservableProperty]
    private decimal _invoiceDiscountValue;

    public IReadOnlyList<DiscountTypeOption> InvoiceDiscountTypeOptions { get; } =
    [
        new(DiscountType.None, "بدون خصم كلي"),
        new(DiscountType.Percentage, "نسبة مئوية (%)"),
        new(DiscountType.FixedAmount, "قيمة ثابتة (د.ع)")
    ];

    [ObservableProperty]
    private DiscountTypeOption? _selectedInvoiceDiscountOption;

    partial void OnSelectedInvoiceDiscountOptionChanged(DiscountTypeOption? value)
    {
        if (value is not null && InvoiceDiscountType != value.Type)
            InvoiceDiscountType = value.Type;
    }

    partial void OnInvoiceDiscountTypeChanged(DiscountType value)
    {
        var match = InvoiceDiscountTypeOptions.FirstOrDefault(o => o.Type == value);
        if (!Equals(SelectedInvoiceDiscountOption, match))
            SelectedInvoiceDiscountOption = match;
        RecalculateTotals();
    }

    [ObservableProperty]
    private decimal _roundingAmount;

    [ObservableProperty]
    private decimal _grandTotal;

    [ObservableProperty]
    private int _totalItemCount;

    [ObservableProperty]
    private decimal _totalQuantity;

    [ObservableProperty]
    private bool _showMenuWeight;

    [ObservableProperty]
    private bool _showProductDiscount;

    [ObservableProperty]
    private string _invoiceWeightSummaryText = string.Empty;

    /// <summary>نسبة الشركة (8%)</summary>
    [ObservableProperty]
    private decimal _companyFeeAmount;

    [ObservableProperty]
    private string _notes = string.Empty;

    // ── State ──────────────────────────────────────────────
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(CanPrintSavedInvoice))]
    private bool _isSaved;

    public bool CanSave => !IsSaved;
    public bool CanPrintSavedInvoice => IsSaved;

    partial void OnIsSavedChanged(bool value)
    {
        PrintInvoiceCommand.NotifyCanExecuteChanged();
        SendInvoiceWhatsAppCommand.NotifyCanExecuteChanged();
    }

    public InstallmentInvoiceViewModel(
        IInvoiceService invoiceService,
        IInstallmentService installmentService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        INavigationService navigationService,
        IExportService exportService,
        IWhatsAppShareService whatsAppShare,
        IInvoiceTemplateService templateService,
        IInvoiceDraftService draftService,
        IInvoiceQueueService queueService,
        IProductPriceService productPriceService,
        IUserPreferencesService userPreferences,
        IFeatureFlagService featureFlags,
        IProductUnitService productUnitService)
    {
        _invoiceService = invoiceService;
        _installmentService = installmentService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _navigationService = navigationService;
        _exportService = exportService;
        _whatsAppShare = whatsAppShare;
        _templateService = templateService;
        _draftService = draftService;
        _queueService = queueService;
        _featureFlags = featureFlags;
        _productUnitService = productUnitService;

        PageTitle = "فاتورة أقساط";

        ProductPicker = new ProductPickerViewModel(
            _unitOfWork,
            productPriceService,
            userPreferences.Current.FeatureFlags.ProductPricingEnabled);
        ProductPicker.Confirmed += OnProductPickerConfirmed;
        ProductPicker.Cancelled += () => IsProductPickerOpen = false;

        Items.CollectionChanged += OnItemsCollectionChanged;
        RefreshFeatureVisibility();
        SelectedInvoiceDiscountOption = InvoiceDiscountTypeOptions[0];
        featureFlags.FlagsChanged += (_, _) => FeatureUiRefresh.Invoke(RefreshFeatureVisibility);
    }

    public override bool HasUnsavedChanges =>
        !IsSaved && Items.Any(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0);

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            LoadPermissions(_currentUserService, "InstallmentInvoice");

            InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Installment);

            var customers = await _unitOfWork.Customers.GetAllAsync();
            Customers.Clear();
            FilteredCustomers.Clear();
            foreach (var c in customers)
            {
                Customers.Add(c);
                FilteredCustomers.Add(c);
            }

            var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
            Warehouses.Clear();
            foreach (var w in warehouses)
                Warehouses.Add(w);
            if (Warehouses.Count > 0)
                SelectedWarehouse = Warehouses[0];

            var cashBoxes = await _unitOfWork.CashBoxes.GetAllAsync();
            CashBoxes.Clear();
            foreach (var cb in cashBoxes)
                CashBoxes.Add(cb);
            if (CashBoxes.Count > 0)
                SelectedCashBox = CashBoxes[0];

            var drivers = await _unitOfWork.Drivers.GetAllAsync();
            Drivers.Clear();
            foreach (var d in drivers.OrderBy(x => x.Name))
                Drivers.Add(d);

            var products = await _unitOfWork.Products.GetAllAsync();
            Products.Clear();
            foreach (var p in products)
                Products.Add(p);

            AddRow();
            GenerateSchedulePreview();

            if (InvoiceNavigationBridge.PendingInstallmentEditInvoiceId is int pendingEditId)
            {
                InvoiceNavigationBridge.PendingInstallmentEditInvoiceId = null;
                await LoadInvoiceForEditAsync(pendingEditId);
            }
            else if (_draftService.HasDraft(DraftKey))
            {
                TryRestoreDraft();
            }

            ApplyDefaultInstallmentCustomerIfAny();
            TryOpenPendingQueuePicker();
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Customer search ────────────────────────────────────
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

    // ── Schedule regeneration triggers ─────────────────────
    partial void OnNumberOfInstallmentsChanged(int value) => GenerateSchedulePreview();
    partial void OnInstallmentStartDateChanged(DateTime value) => GenerateSchedulePreview();

    private void GenerateSchedulePreview()
    {
        SchedulePreview.Clear();

        if (NumberOfInstallments <= 0 || GrandTotal <= 0) return;

        decimal perInstallment = Math.Floor(GrandTotal / NumberOfInstallments);
        InstallmentAmount = perInstallment;

        for (int i = 0; i < NumberOfInstallments; i++)
        {
            decimal amount = (i < NumberOfInstallments - 1)
                ? perInstallment
                : GrandTotal - (perInstallment * (NumberOfInstallments - 1));
            SchedulePreview.Add(new InstallmentScheduleRow
            {
                Number = i + 1,
                DueDate = InstallmentStartDate.AddMonths(i),
                Amount = amount
            });
        }
    }

    // ── Items management ───────────────────────────────────
    [RelayCommand]
    private void AddRow()
    {
        var row = new InvoiceItemRow();
        WireItemRow(row);
        Items.Add(row);
    }

    [RelayCommand]
    private async Task OpenProductPickerAsync()
    {
        try
        {
            await ProductPicker.InitializeAsync(SelectedWarehouse?.Id, InvoicePickerMode.Installment);
            ProductPicker.SeedFromInvoiceItems(Items);
            IsProductPickerOpen = true;
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر فتح اختيار المنتجات:\n{ex.Message}");
        }
    }

    private void OnProductPickerConfirmed()
    {
        InvoiceProductMergeHelper.Merge(
            ProductPicker.BuildResults(),
            Items,
            WireItemRow,
            UnwireItemRow);

        foreach (var row in Items.Where(i => i.ProductId is not null))
            _ = LoadRowUnitsAsync(row);

        RecalculateTotals();
        IsProductPickerOpen = false;
    }

    private void WireItemRow(InvoiceItemRow row)
    {
        row.ProductDiscountFeatureEnabled = ShowProductDiscount;
        row.RefreshProductDiscount();
        row.TotalChanged += RecalculateTotals;
        row.ProductChanged += OnProductChanged;
    }

    private void UnwireItemRow(InvoiceItemRow row)
    {
        row.TotalChanged -= RecalculateTotals;
        row.ProductChanged -= OnProductChanged;
    }

    [RelayCommand]
    private void ProcessBarcode()
    {
        if (!InvoiceBarcodeHelper.TryAddByBarcode(
                BarcodeInput,
                Products,
                Items,
                WireItemRow,
                UnwireItemRow,
                row => OnProductChanged(row),
                out var error))
        {
            BeautifulMessageDialog.ShowWarning(error);
            return;
        }

        BarcodeInput = string.Empty;
        RecalculateTotals();
    }

    [RelayCommand]
    private void IncreaseRowQuantity(InvoiceItemRow? row)
    {
        if (row is null) return;
        row.Quantity += 1;
        RecalculateTotals();
    }

    [RelayCommand]
    private void DecreaseRowQuantity(InvoiceItemRow? row)
    {
        if (row is null || row.Quantity <= 1) return;
        row.Quantity -= 1;
        RecalculateTotals();
    }

    [RelayCommand]
    private void RemoveRow(InvoiceItemRow? row)
    {
        if (row is null) return;
        UnwireItemRow(row);
        Items.Remove(row);
        RecalculateTotals();
    }

    private async void OnProductChanged(InvoiceItemRow row)
    {
        if (row.ProductId is null)
        {
            row.StockInfo = string.Empty;
            row.AvailableStock = 0;
            ClearRowUnitsFor(row);
            return;
        }

        try
        {
            await LoadRowUnitsAsync(row);

            var stocks = await _unitOfWork.WarehouseStocks.FindAsync(s => s.ProductId == row.ProductId.Value);
            var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
            var warehouseDict = warehouses.ToDictionary(w => w.Id, w => w.Name);

            var lines = stocks
                .Where(s => s.Quantity != 0)
                .Select(s => $"{warehouseDict.GetValueOrDefault(s.WarehouseId, "مخزن")}: {s.Quantity:N0}")
                .ToList();

            row.StockInfo = lines.Count > 0 ? string.Join(" | ", lines) : "لا يوجد رصيد";

            if (SelectedWarehouse is not null)
                row.AvailableStock = stocks.FirstOrDefault(s => s.WarehouseId == SelectedWarehouse.Id)?.Quantity ?? 0;
            else
                row.AvailableStock = stocks.Sum(s => s.Quantity);
        }
        catch
        {
            row.StockInfo = string.Empty;
            row.AvailableStock = 0;
        }
    }

    private static void ClearRowUnitsFor(InvoiceItemRow row)
    {
        row.SelectedUnit = null;
        row.AvailableUnits.Clear();
        row.SelectedUnitName = string.Empty;
        row.UnitConversionFactor = 1m;
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RecalculateTotals();
        ScheduleDraftSave();
    }

    private bool _isRecalculating;

    partial void OnGrandTotalChanged(decimal value)
    {
        if (!_isRecalculating)
        {
            UpdateCompanyFee();
            GenerateSchedulePreview();
        }
    }

    private void RecalculateTotals()
    {
        decimal sub = 0m;
        int itemCount = 0;
        decimal totalQty = 0m;
        foreach (var item in Items)
        {
            sub += item.TotalPrice;
            if (!string.IsNullOrWhiteSpace(item.ItemName))
            {
                itemCount++;
                totalQty += item.Quantity;
            }
        }

        Subtotal = sub;
        TotalItemCount = itemCount;
        TotalQuantity = totalQty;
        InvoiceWeightSummaryText = InvoiceWeightHelper.BuildSummaryText(Items);

        if (ShowProductDiscount)
            InvoiceDiscountAmount = ProductDiscountHelper.CalculateInvoiceDiscount(
                InvoiceDiscountType, InvoiceDiscountValue, sub);
        else
            InvoiceDiscountAmount = 0m;

        var (_, _, rounding, grand) = InvoiceTotalsCalculator.Compute(
            Items.Select(i => i.TotalPrice),
            _invoiceService,
            InvoiceType.Installment,
            InvoiceDiscountAmount,
            ShowTransportFee ? TransportFeeAmount : 0m);

        RoundingAmount = rounding;
        _isRecalculating = true;
        GrandTotal = grand;
        _isRecalculating = false;
        UpdateCompanyFee();
        GenerateSchedulePreview();
    }

    partial void OnInvoiceDiscountValueChanged(decimal value) => RecalculateTotals();

    partial void OnSelectedInstallmentTypeChanged(InstallmentType value) => UpdateCompanyFee();

    private void UpdateCompanyFee()
    {
        CompanyFeeAmount = ShowCompanyFee
            ? CompanyFeeHelper.CalculateAmount(GrandTotal)
            : 0;
    }

    // ── Save ───────────────────────────────────────────────
    [RelayCommand]
    private async Task SaveInvoice()
    {
        ErrorMessage = string.Empty;

        // Validation
        if (SelectedCustomer is null && string.IsNullOrWhiteSpace(CustomerSearchText))
        {
            ErrorMessage = "يجب اختيار العميل لفاتورة الأقساط";
            return;
        }

        if (SelectedWarehouse is null)
        {
            ErrorMessage = "يرجى اختيار المخزن";
            return;
        }

        if (NumberOfInstallments <= 0)
        {
            ErrorMessage = "عدد الأقساط يجب أن يكون أكبر من صفر";
            return;
        }

        var validItems = Items
            .Where(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0 && (i.UnitPrice > 0 || i.TotalPrice > 0))
            .ToList();

        if (validItems.Count == 0)
        {
            ErrorMessage = "يجب إضافة عنصر واحد على الأقل بالكمية والسعر";
            return;
        }

        // Stock validation
        if (SelectedWarehouse is not null)
        {
            foreach (var item in validItems.Where(i => i.ProductId.HasValue))
            {
                var stocks = await _unitOfWork.WarehouseStocks.FindAsync(
                    s => s.WarehouseId == SelectedWarehouse.Id && s.ProductId == item.ProductId!.Value);
                var available = stocks.FirstOrDefault()?.Quantity ?? 0;
                if (item.Quantity > available)
                {
                    ErrorMessage = $"الكمية المطلوبة من '{item.ItemName}' ({item.Quantity:N0}) تتجاوز الرصيد المتاح ({available:N0}) في المخزن '{SelectedWarehouse.Name}'";
                    return;
                }
            }
        }

        IsBusy = true;

        try
        {
            // Determine customer
            int? customerId = SelectedCustomer?.Id;

            if (customerId is null && !string.IsNullOrWhiteSpace(CustomerSearchText))
            {
                var newCustomer = new Customer
                {
                    Name = CustomerSearchText.Trim(),
                    FileNumber = string.IsNullOrWhiteSpace(FileNumber) ? null : FileNumber.Trim(),
                    CreatedBy = _currentUserService.Username,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Customers.AddAsync(newCustomer);
                await _unitOfWork.SaveChangesAsync();
                customerId = newCustomer.Id;
            }

            var invoice = new Invoice
            {
                InvoiceNumber = InvoiceNumber,
                InvoiceType = InvoiceType.Installment,
                CustomerId = customerId,
                DriverId = ShowDriverSelection ? SelectedDriver?.Id : null,
                WarehouseId = SelectedWarehouse.Id,
                PaymentMethod = PaymentMethod.Installment,
                CashBoxId = SelectedCashBox?.Id,
                Date = InvoiceDate,
                DiscountAmount = ShowProductDiscount ? InvoiceDiscountAmount : 0m,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                TransportFeeAmount = ShowTransportFee ? Math.Max(0m, TransportFeeAmount) : 0m
            };

            var invoiceItems = new List<InvoiceItem>();
            foreach (var row in validItems)
            {
                int? productId = row.ProductId;
                if (productId is null)
                {
                    var matchedProduct = Products.FirstOrDefault(p =>
                        p.Name.Equals(row.ItemName.Trim(), StringComparison.OrdinalIgnoreCase));
                    productId = matchedProduct?.Id;
                }

                var displayQty = row.Quantity;
                var stockQty = InvoiceCustomFieldsHelper.ToStockQuantity(row);
                var lineGross = displayQty * row.UnitPrice;
                var lineDiscount = ShowProductDiscount ? row.DiscountAmount : 0m;
                if (lineDiscount > Math.Abs(lineGross))
                    lineDiscount = Math.Abs(lineGross);
                var lineTotal = ProductDiscountHelper.CalculateLineTotal(displayQty, row.UnitPrice, lineDiscount);
                var unitPriceForStorage = stockQty == 0 ? row.UnitPrice : lineGross / stockQty;
                var discountForStorage = stockQty == 0 || displayQty == 0
                    ? lineDiscount
                    : lineDiscount * (stockQty / displayQty);

                invoiceItems.Add(new InvoiceItem
                {
                    ProductId = productId,
                    PricingTypeId = row.PricingTypeId,
                    ItemName = row.ItemName.Trim(),
                    Quantity = stockQty,
                    UnitPrice = unitPriceForStorage,
                    DiscountAmount = discountForStorage,
                    TotalPrice = lineTotal,
                    CustomFieldsJson = InvoiceCustomFieldsHelper.ToJson(row)
                });
            }

            Invoice savedInvoice;
            if (_editingInvoiceId is int editId)
            {
                savedInvoice = await _invoiceService.ReplaceInvoiceAsync(editId, invoice, invoiceItems);
                ClearEditingInvoiceId();
            }
            else
            {
                savedInvoice = await _invoiceService.CreateInvoiceAsync(invoice, invoiceItems);
            }

            // Create installment plan
            var plan = await _installmentService.CreatePlanAsync(
                savedInvoice.Id,
                customerId!.Value,
                string.IsNullOrWhiteSpace(FileNumber) ? null : FileNumber.Trim(),
                savedInvoice.NetAmount,
                NumberOfInstallments,
                InstallmentStartDate,
                SelectedInstallmentType);

            IsSaved = true;
            InvoiceNumber = savedInvoice.InvoiceNumber;
            _draftService.ClearDraft(DraftKey);

            _savedInvoice = savedInvoice;
            _savedItems = invoiceItems;
            _savedPlan = plan;

            var successMsg =
                $"تم حفظ فاتورة الأقساط بنجاح\n" +
                $"رقم الفاتورة: {savedInvoice.InvoiceNumber}\n" +
                $"المبلغ الكلي: {savedInvoice.NetAmount:N0} د.ع\n" +
                $"نوع القسط: {(SelectedInstallmentType == InstallmentType.Platform ? "بيع منصة" : "يدوي")}\n";
            if (plan.CompanyFeeAmount > 0)
                successMsg += $"نسبة الشركة (8%): {plan.CompanyFeeAmount:N0} د.ع\n";
            successMsg +=
                $"عدد الأقساط: {NumberOfInstallments}\n" +
                $"مبلغ القسط: {plan.InstallmentAmount:N0} د.ع\n\n" +
                "يمكنك الطباعة أو الإرسال عبر واتساب.";
            BeautifulMessageDialog.ShowSuccess(successMsg);

            await PrintInvoiceAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Print ──────────────────────────────────────────────
    [RelayCommand(CanExecute = nameof(CanPrintSavedInvoice))]
    private async Task PrintInvoiceAsync()
    {
        if (_savedInvoice is null) return;

        if (_savedPlan?.Id > 0)
        {
            var installments = await _installmentService.GetInstallmentsByPlanIdAsync(_savedPlan.Id);
            _savedPlan.Installments = installments.ToList();
        }

        _exportService.PrintInvoice(BuildSavedInvoicePrintModel());
        if (ShowDriverSelection)
        {
            var warehouseCopy = BuildWarehouseCopyPrintModel(BuildSavedInvoicePrintModel());
            _exportService.PrintInvoice(warehouseCopy);
        }
    }

    [RelayCommand(CanExecute = nameof(CanPrintSavedInvoice))]
    private void SendInvoiceWhatsApp()
    {
        if (_savedInvoice is null) return;
        _whatsAppShare.ShareInvoice(
            BuildSavedInvoicePrintModel(),
            SelectedCustomer?.Phone,
            SelectedCustomer?.Name ?? CustomerSearchText);
    }

    private InvoicePrintModel BuildSavedInvoicePrintModel()
    {
        if (_savedInvoice is null)
            throw new InvalidOperationException("لا توجد فاتورة محفوظة");

        return new InvoicePrintModel
        {
            Title = "فاتورة أقساط",
            InvoiceNumber = _savedInvoice.InvoiceNumber,
            Date = _savedInvoice.Date,
            PartyLabel = "العميل",
            PartyName = SelectedCustomer?.Name ?? CustomerSearchText,
            PartyPhone = SelectedCustomer?.Phone,
            PartyAddress = SelectedCustomer?.Address,
            DriverName = ShowDriverSelection ? SelectedDriver?.Name : null,
            WarehouseName = SelectedWarehouse?.Name ?? string.Empty,
            PaymentMethod = "أقساط",
            Notes = _savedInvoice.Notes,
            FileNumber = string.IsNullOrWhiteSpace(FileNumber) ? null : FileNumber,
            Subtotal = Subtotal,
            RoundingAmount = RoundingAmount,
            TransportFeeAmount = ShowTransportFee ? TransportFeeAmount : 0m,
            GrandTotal = GrandTotal,
            CompanyFeeAmount = _savedPlan?.CompanyFeeAmount > 0 ? _savedPlan.CompanyFeeAmount : null,
            NumberOfInstallments = NumberOfInstallments,
            InstallmentAmount = _savedPlan?.InstallmentAmount,
            Items = _savedItems.Select((item, i) => new InvoicePrintItem
            {
                Number = i + 1,
                ItemName = InvoiceCustomFieldsHelper.FormatItemDisplayName(
                    item.ItemName,
                    item.CustomFieldsJson),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList(),
            Schedule = _savedPlan?.Installments?.Count > 0
                ? InstallmentPrintHelpers.ToPrintRows(_savedPlan.Installments)
                : SchedulePreview.Select(s => new InstallmentPrintRow
                {
                    Number = s.Number,
                    DueDate = s.DueDate,
                    Amount = s.Amount,
                    RemainingAmount = s.Amount,
                    StatusText = "معلق"
                }).ToList()
        };
    }

    private static InvoicePrintModel BuildWarehouseCopyPrintModel(InvoicePrintModel source) =>
        new()
        {
            Title = "نسخة مخزن",
            InvoiceNumber = source.InvoiceNumber,
            Date = source.Date,
            PartyLabel = source.PartyLabel,
            PartyName = source.PartyName,
            PartyPhone = source.PartyPhone,
            PartyAddress = source.PartyAddress,
            DriverName = source.DriverName,
            WarehouseName = source.WarehouseName,
            PaymentMethod = source.PaymentMethod,
            Notes = source.Notes,
            FileNumber = source.FileNumber,
            HideAmounts = true,
            Items = source.Items.Select(i => new InvoicePrintItem
            {
                Number = i.Number,
                ItemName = i.ItemName,
                Quantity = i.Quantity,
                UnitPrice = 0,
                TotalPrice = 0
            }).ToList()
        };

    // ── New invoice (reset) ────────────────────────────────
    [RelayCommand]
    private async Task NewInvoice()
    {
        IsSaved = false;
        ClearEditingInvoiceId();
        _savedInvoice = null;
        _savedItems = [];
        _savedPlan = null;
        ErrorMessage = string.Empty;
        Notes = string.Empty;
        TransportFeeAmount = 0m;
        CustomerSearchText = string.Empty;
        SelectedCustomer = null;
        SelectedDriver = null;
        FileNumber = string.Empty;
        NumberOfInstallments = 6;
        InstallmentStartDate = DateTime.Now.AddMonths(1);
        InvoiceDate = DateTime.Now;

        foreach (var item in Items.ToList())
            UnwireItemRow(item);
        Items.Clear();
        AddRow();

        RecalculateTotals();
        InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Installment);
        ApplyDefaultInstallmentCustomerIfAny();
        GenerateSchedulePreview();
    }

    // ══════════════════════════════════════════════════════
    // QUICK ADD CUSTOMER
    // ══════════════════════════════════════════════════════
    [ObservableProperty]
    private bool _isQuickAddCustomerOpen;

    [ObservableProperty]
    private string _quickCustomerName = string.Empty;

    [ObservableProperty]
    private string _quickCustomerPhone = string.Empty;

    [ObservableProperty]
    private string _quickCustomerAddress = string.Empty;

    [ObservableProperty]
    private string _quickCustomerError = string.Empty;

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

            var customers = await _unitOfWork.Customers.GetAllAsync();
            Customers.Clear();
            FilteredCustomers.Clear();
            foreach (var c in customers)
            {
                Customers.Add(c);
                FilteredCustomers.Add(c);
            }

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
