using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Threading;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class SalesInvoiceViewModel : ViewModelBase, IProductQuickSearchHost
{
    private readonly IInvoiceService _invoiceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly INavigationService _navigationService;
    private readonly IExportService _exportService;
    private readonly IWhatsAppShareService _whatsAppShare;
    private readonly IInvoiceDraftService _draftService;
    private readonly IRecentActivityService _recentActivity;
    private readonly IPartyQuickDetailService _partyQuickDetail;
    private readonly IProductQuickDetailService _productQuickDetail;
    private readonly ICustomerCreditService _customerCreditService;
    private DispatcherTimer? _draftSaveTimer;
    private const string DraftKey = "sales-invoice";

    // saved invoice reference for printing
    private Invoice? _savedInvoice;
    private List<InvoiceItem> _savedItems = [];
    private int? _relatedInvoiceId;

    // ── Header ─────────────────────────────────────────────
    [ObservableProperty]
    private string _invoiceNumber = string.Empty;

    [ObservableProperty]
    private DateTime _invoiceDate = DateTime.Now;

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

    // Payment — three-way: Cash / Credit / Installment
    [ObservableProperty]
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;

    /// <summary>تاريخ استحقاق التسديد — يظهر فقط عند اختيار الآجل</summary>
    [ObservableProperty]
    private DateTime? _creditDueDate = DateTime.Today.AddMonths(1);

    [ObservableProperty]
    private CashBox? _selectedCashBox;

    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    // ── Items ──────────────────────────────────────────────
    public ObservableCollection<InvoiceItemRow> Items { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];

    public ProductPickerViewModel ProductPicker { get; }
    public ProductQuickSearchCatalog QuickSearchCatalog { get; }

    [ObservableProperty]
    private bool _isProductPickerOpen;

    [ObservableProperty]
    private string _barcodeInput = string.Empty;

    // ── Footer / Totals ────────────────────────────────────
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
    private string _invoiceWeightSummaryText = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    // ── State ──────────────────────────────────────────────
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InvoiceWarningsBanner))]
    [NotifyPropertyChangedFor(nameof(ShowCustomerAndPayment))]
    private bool _isReturnMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InvoiceWarningsBanner))]
    [NotifyPropertyChangedFor(nameof(ShowCustomerAndPayment))]
    [NotifyPropertyChangedFor(nameof(ShowCashBox))]
    private bool _isDamageMode;

    /// <summary>إخفاء العميل وطريقة الدفع في وضع التلف.</summary>
    public bool ShowCustomerAndPayment => !IsDamageMode;

    public bool ShowCashBox => IsCashPayment && !IsDamageMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(CanPrintSavedInvoice))]
    private bool _isSaved;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInvoiceWarnings))]
    private string _invoiceWarningsBanner = string.Empty;

    public bool HasInvoiceWarnings => !string.IsNullOrWhiteSpace(InvoiceWarningsBanner);

    public bool CanSave => !IsSaved;

    /// <summary>فاتورة محفوظة وجاهزة للطباعة/واتساب (منفصل عن صلاحية CanPrint في ViewModelBase).</summary>
    public bool CanPrintSavedInvoice => IsSaved;

    partial void OnIsSavedChanged(bool value)
    {
        PrintInvoiceCommand.NotifyCanExecuteChanged();
        SendInvoiceWhatsAppCommand.NotifyCanExecuteChanged();
    }

    // Helpers for payment visibility
    public bool IsCashPayment => SelectedPaymentMethod == PaymentMethod.Cash;
    public bool IsCreditPayment => SelectedPaymentMethod == PaymentMethod.Credit;
    public bool IsInstallmentPayment => SelectedPaymentMethod == PaymentMethod.Installment;

    [ObservableProperty] private decimal _creditPaidAmount;
    [ObservableProperty] private decimal _creditRemainingAmount;

    partial void OnCreditPaidAmountChanged(decimal value)
    {
        if (!IsCreditPayment) return;
        var paid = Math.Clamp(value, 0m, GrandTotal);
        if (paid != value)
        {
            CreditPaidAmount = paid;
            return;
        }

        CreditRemainingAmount = Math.Max(0m, GrandTotal - paid);
    }

    public SalesInvoiceViewModel(
        IInvoiceService invoiceService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        INavigationService navigationService,
        IExportService exportService,
        IWhatsAppShareService whatsAppShare,
        IInvoiceDraftService draftService,
        IRecentActivityService recentActivity,
        IInvoiceTemplateService templateService,
        IInvoiceQueueService queueService,
        IProductPriceService productPriceService,
        IPricingTypeService pricingTypeService,
        IUserPreferencesService userPreferences,
        IFeatureFlagService featureFlags,
        IProductUnitService productUnitService,
        IProductBatchService productBatchService,
        IProductSerialService productSerialService,
        IProductSizeService productSizeService,
        IProductColorService productColorService,
        ILoyaltyService loyaltyService,
        IProductOfferService productOfferService,
        ISalesRepService salesRepService,
        IPartyQuickDetailService partyQuickDetail,
        IProductQuickDetailService productQuickDetail,
        ICustomerCreditService customerCreditService)
    {
        _invoiceService = invoiceService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _navigationService = navigationService;
        _exportService = exportService;
        _whatsAppShare = whatsAppShare;
        _draftService = draftService;
        _recentActivity = recentActivity;
        _partyQuickDetail = partyQuickDetail;
        _productQuickDetail = productQuickDetail;
        _customerCreditService = customerCreditService;
        _templateService = templateService;
        _queueService = queueService;
        _productPriceService = productPriceService;
        _pricingTypeService = pricingTypeService;

        PageTitle = "فاتورة مبيعات";

        ProductPicker = new ProductPickerViewModel(
            _unitOfWork,
            productPriceService,
            userPreferences.Current.FeatureFlags.ProductPricingEnabled);
        ProductPicker.Confirmed += OnProductPickerConfirmed;
        ProductPicker.Cancelled += () => IsProductPickerOpen = false;
        QuickSearchCatalog = new ProductQuickSearchCatalog(_unitOfWork, productPriceService);

        Items.CollectionChanged += OnItemsCollectionChanged;
        ConfigureFeatureServices(
            featureFlags, productUnitService, productBatchService, productSerialService,
            productSizeService, productColorService, salesRepService);
        ConfigureLoyaltyService(loyaltyService);
        ConfigureProductOfferService(productOfferService);
        SelectedInvoiceDiscountOption = InvoiceDiscountTypeOptions[0];
    }

    private void ScheduleDraftSave()
    {
        if (IsSaved) return;
        _draftSaveTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _draftSaveTimer.Stop();
        _draftSaveTimer.Tick -= OnDraftSaveTick;
        _draftSaveTimer.Tick += OnDraftSaveTick;
        _draftSaveTimer.Start();
    }

    private void OnDraftSaveTick(object? sender, EventArgs e)
    {
        _draftSaveTimer?.Stop();
        if (IsSaved || !Items.Any(i => !string.IsNullOrWhiteSpace(i.ItemName))) return;
        _draftService.SaveDraft(DraftKey, BuildDraft());
    }

    private SalesInvoiceDraft BuildDraft() => new()
    {
        InvoiceNumber = InvoiceNumber,
        InvoiceDate = InvoiceDate,
        CustomerId = SelectedCustomer?.Id,
        DriverId = ShowDriverSelection ? SelectedDriver?.Id : null,
        SalesRepresentativeId = ShowSalesRepSelection ? SelectedSalesRepresentative?.Id : null,
        WarehouseId = SelectedWarehouse?.Id,
        PaymentMethod = SelectedPaymentMethod,
        CreditDueDate = CreditDueDate,
        CashBoxId = SelectedCashBox?.Id,
        Notes = Notes,
        PaidAmount = CreditPaidAmount,
        Lines = Items.Where(i => !string.IsNullOrWhiteSpace(i.ItemName))
            .Select(InvoiceDraftLineMapper.ToDraftLine)
            .ToList()
    };

    private void ApplyDraft(SalesInvoiceDraft draft)
    {
        InvoiceDate = draft.InvoiceDate;
        SelectedPaymentMethod = draft.PaymentMethod;
        CreditDueDate = draft.CreditDueDate;
        Notes = draft.Notes ?? string.Empty;
        CreditPaidAmount = draft.PaidAmount;
        if (draft.CustomerId.HasValue)
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == draft.CustomerId);
        if (draft.DriverId.HasValue && ShowDriverSelection)
            SelectedDriver = Drivers.FirstOrDefault(d => d.Id == draft.DriverId);
        if (draft.SalesRepresentativeId.HasValue && ShowSalesRepSelection)
            SelectedSalesRepresentative = SalesRepresentatives.FirstOrDefault(r => r.Id == draft.SalesRepresentativeId);
        if (draft.WarehouseId.HasValue)
            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == draft.WarehouseId);
        if (draft.CashBoxId.HasValue)
            SelectedCashBox = CashBoxes.FirstOrDefault(c => c.Id == draft.CashBoxId);

        foreach (var existing in Items.ToList())
            UnwireItemRow(existing);
        Items.Clear();

        foreach (var line in draft.Lines)
        {
            var row = InvoiceDraftLineMapper.ToRow(line, Products);
            ApplyActiveLabelsToRow(row);
            WireItemRow(row);
            Items.Add(row);
            _ = LoadRowFeatureDataAsync(row);
        }

        if (Items.Count == 0)
            AddRow();

        RecalculateTotals();
    }

    public override bool HasUnsavedChanges =>
        !IsSaved && Items.Any(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0);

    partial void OnSelectedPaymentMethodChanged(PaymentMethod value)
    {
        OnPropertyChanged(nameof(IsCashPayment));
        OnPropertyChanged(nameof(IsCreditPayment));
        OnPropertyChanged(nameof(IsInstallmentPayment));
        OnPropertyChanged(nameof(ShowCashBox));
        // Reset CreditDueDate when switching away from Credit
        if (value != PaymentMethod.Credit)
        {
            CreditDueDate = null;
            CreditPaidAmount = 0m;
            CreditRemainingAmount = 0m;
        }
        else
        {
            CreditDueDate = DateTime.Today.AddMonths(1);
            CreditPaidAmount = 0m;
            CreditRemainingAmount = GrandTotal;
        }
        _ = RefreshLoyaltyQuoteAsync();
        RefreshInvoiceWarnings();
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            LoadPermissions(_currentUserService, "SaleInvoice");

            InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Sale);

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

            var reps = await _unitOfWork.SalesRepresentatives.GetAllAsync();
            SalesRepresentatives.Clear();
            foreach (var r in reps.Where(x => x.IsActive).OrderBy(x => x.Name))
                SalesRepresentatives.Add(r);

            var products = await _unitOfWork.Products.GetAllAsync();
            Products.Clear();
            foreach (var p in products)
                Products.Add(p);

            await QuickSearchCatalog.LoadAsync(
                Products,
                InvoicePickerMode.Sale,
                ShowProductPricing);

            if (ShowProductPricing)
                await InvoiceBulkPricingHelper.LoadBulkPricingTypesAsync(_pricingTypeService, BulkPricingTypes);

            AddRow();

            if (InvoiceNavigationBridge.PendingSalesReturnFromInvoiceId is int pendingReturnId)
            {
                InvoiceNavigationBridge.PendingSalesReturnFromInvoiceId = null;
                InvoiceNavigationBridge.PendingSalesReturnMode = false;
                await LoadAsReturnFromInvoiceAsync(pendingReturnId);
            }
            else if (InvoiceNavigationBridge.PendingSalesReturnMode)
            {
                InvoiceNavigationBridge.PendingSalesReturnMode = false;
                await EnterReturnModeAsync();
            }
            else if (InvoiceNavigationBridge.PendingDamageMode)
            {
                InvoiceNavigationBridge.PendingDamageMode = false;
                await EnterDamageModeAsync();
            }
            else if (InvoiceNavigationBridge.PendingSalesCopyInvoiceId is int pendingCopyId)
            {
                InvoiceNavigationBridge.PendingSalesCopyInvoiceId = null;
                await CopyFromInvoiceAsync(pendingCopyId);
            }
            else if (InvoiceNavigationBridge.PendingSalesEditInvoiceId is int pendingEditId)
            {
                InvoiceNavigationBridge.PendingSalesEditInvoiceId = null;
                await LoadInvoiceForEditAsync(pendingEditId);
            }
            else if (_draftService.HasDraft(DraftKey))
            {
                var savedAt = _draftService.GetDraftSavedAt(DraftKey);
                var when = savedAt.HasValue ? savedAt.Value.ToString("yyyy/MM/dd HH:mm") : "";
                if (BeautifulMessageDialog.ShowConfirm(
                        $"يوجد مسودة فاتورة مبيعات محفوظة ({when}).\nهل تريد استعادتها؟"))
                {
                    var draft = _draftService.LoadDraft<SalesInvoiceDraft>(DraftKey);
                    if (draft is not null)
                        ApplyDraft(draft);
                }
            }

            ApplyDefaultCustomerIfAny();
            TryOpenPendingQueuePicker();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task CopyFromInvoiceAsync(int invoiceId)
    {
        var invoice = await _invoiceService.GetByIdWithDetailsAsync(invoiceId);
        if (invoice is null)
        {
            BeautifulMessageDialog.ShowWarning("تعذر تحميل الفاتورة للنسخ");
            return;
        }

        IsSaved = false;
        InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Sale);
        InvoiceDate = DateTime.Now;
        Notes = string.IsNullOrWhiteSpace(invoice.Notes) ? string.Empty : $"{invoice.Notes} (نسخة)";

        if (invoice.CustomerId.HasValue)
        {
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == invoice.CustomerId);
            if (SelectedCustomer is not null)
                CustomerSearchText = SelectedCustomer.Name;
        }

        if (ShowDriverSelection && invoice.DriverId.HasValue)
            SelectedDriver = Drivers.FirstOrDefault(d => d.Id == invoice.DriverId);
        else
            SelectedDriver = null;

        if (ShowSalesRepSelection && invoice.SalesRepresentativeId.HasValue)
            SelectedSalesRepresentative = SalesRepresentatives.FirstOrDefault(r => r.Id == invoice.SalesRepresentativeId);
        else
            SelectedSalesRepresentative = null;

        if (invoice.WarehouseId > 0)
            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == invoice.WarehouseId);

        SelectedPaymentMethod = invoice.PaymentMethod == PaymentMethod.Installment
            ? PaymentMethod.Cash
            : invoice.PaymentMethod;

        if (SelectedPaymentMethod == PaymentMethod.Credit)
            CreditDueDate = invoice.CreditDueDate ?? DateTime.Today.AddMonths(1);

        foreach (var row in Items.ToList())
            UnwireItemRow(row);
        Items.Clear();

        foreach (var item in invoice.Items)
        {
            var row = new InvoiceItemRow
            {
                ProductId = item.ProductId,
                PricingTypeId = item.PricingTypeId,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            };
            ApplyActiveLabelsToRow(row);
            InvoiceCustomFieldsHelper.ApplyFromJson(row, item.CustomFieldsJson, ActiveCustomFieldLabels);
            WireItemRow(row);
            Items.Add(row);
            _ = LoadRowFeatureDataAsync(row);
        }

        if (!Items.Any())
            AddRow();

        RecalculateTotals();
        BeautifulMessageDialog.ShowSuccess($"تم نسخ {invoice.Items.Count} بند من الفاتورة {invoice.InvoiceNumber}");
    }

    public async Task EnterReturnModeAsync(string? reference = null)
    {
        if (_featureFlags is not null && !_featureFlags.SalesReturns)
        {
            BeautifulMessageDialog.ShowWarning("فعّل «مرتجع مبيعات» من إعدادات الميزات أولاً");
            return;
        }

        IsReturnMode = true;
        PageTitle = "مرتجع مبيعات";
        SelectedPaymentMethod = PaymentMethod.Cash;
        if (!string.IsNullOrWhiteSpace(reference) && string.IsNullOrWhiteSpace(Notes))
            Notes = $"مرتجع مبيعات — مرجع {reference}";
        InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.SaleReturn);
        RefreshInvoiceWarnings();
    }

    public async Task EnterDamageModeAsync()
    {
        if (_featureFlags is not null && !_featureFlags.DamageInvoices)
        {
            BeautifulMessageDialog.ShowWarning("فعّل «فاتورة التلف» من إعدادات الميزات أولاً");
            return;
        }

        IsDamageMode = true;
        IsReturnMode = false;
        PageTitle = "فاتورة تلف";
        SelectedPaymentMethod = PaymentMethod.Cash;
        SelectedCustomer = null;
        CustomerSearchText = string.Empty;
        SelectedCashBox = null;
        CreditDueDate = null;
        CreditPaidAmount = 0m;
        CreditRemainingAmount = 0m;
        InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Damage);
        OnPropertyChanged(nameof(ShowCustomerAndPayment));
        RefreshInvoiceWarnings();
    }

    public async Task LoadAsReturnFromInvoiceAsync(int invoiceId)
    {
        if (_featureFlags is not null && !_featureFlags.SalesReturns)
        {
            BeautifulMessageDialog.ShowWarning("فعّل «مرتجع مبيعات» من إعدادات الميزات أولاً");
            return;
        }

        var source = await _invoiceService.GetByIdWithDetailsAsync(invoiceId);
        var refNumber = source?.InvoiceNumber ?? invoiceId.ToString();
        await CopyFromInvoiceAsync(invoiceId);
        _relatedInvoiceId = invoiceId;
        await EnterReturnModeAsync(refNumber);

        foreach (var row in Items.Where(i => i.Quantity != 0).ToList())
            row.Quantity = Math.Abs(row.Quantity);

        RecalculateTotals();
        RefreshInvoiceWarnings();
        BeautifulMessageDialog.ShowInfo("وضع المرتجع: راجع الكميات ثم احفظ لإرجاع البضاعة للمخزن واسترداد النقد.");
    }

    partial void OnGrandTotalChanged(decimal value) => RefreshInvoiceWarnings();

    private void RefreshInvoiceWarnings()
    {
        var warnings = new List<string>();
        if (IsReturnMode)
            warnings.Add("وضع مرتجع مبيعات — سيتم زيادة المخزون وخصم المبلغ من القاصة عند الحفظ النقدي.");
        if (IsDamageMode)
            warnings.Add("فاتورة تلف — سيتم إنقاص الكمية من المخزن عند الحفظ دون التأثير على الصندوق.");
        if (IsCreditPayment && SelectedCustomer?.MaxCreditLimit is > 0)
            warnings.Add($"حد ائتمان العميل: {SelectedCustomer.MaxCreditLimit:N0} د.ع — سيُفحص عند الحفظ.");
        if (!IsReturnMode && !IsDamageMode && Items.Any(i => i.ProductId is > 0 && i.Quantity > 0 && i.UnitPrice > 0))
            warnings.Add("F4 إضافة منتج · F5 فحص الربح · Ctrl+S حفظ · F7 حاسبة العملة · Esc إلغاء المسودة.");
        InvoiceWarningsBanner = string.Join("  |  ", warnings);
    }

    // ── Customer search ────────────────────────────────────
    partial void OnSelectedCustomerChanged(Customer? value)
    {
        // When user picks from dropdown, update search text to show selected name
        if (value is not null)
            CustomerSearchText = value.Name;
        ApplyCustomerDefaultSalesRep(value);
        _ = RefreshLoyaltyQuoteAsync();
        RefreshInvoiceWarnings();
    }

    partial void OnCustomerSearchTextChanged(string value)
    {
        if (SelectedCustomer is not null && SelectedCustomer.Name == value)
            return;

        SelectedCustomer = null;
        CustomerComboBoxFilter.Apply(Customers, FilteredCustomers, value);
    }

    // ── Items management ───────────────────────────────────
    [RelayCommand]
    private void AddRow()
    {
        var row = new InvoiceItemRow();
        ApplyActiveLabelsToRow(row);
        WireItemRow(row);
        Items.Add(row);
    }

    [RelayCommand]
    private async Task OpenProductPickerAsync()
    {
        try
        {
            await ProductPicker.InitializeAsync(SelectedWarehouse?.Id, InvoicePickerMode.Sale);
            ProductPicker.SeedFromInvoiceItems(Items);
            IsProductPickerOpen = true;
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر فتح اختيار المنتجات:\n{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CheckProfitAsync()
    {
        var productRows = Items
            .Where(i => i.ProductId is > 0 && !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity != 0)
            .ToList();
        if (productRows.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("أضف مواد إلى الفاتورة أولاً ثم افحص الربح.");
            return;
        }

        try
        {
            var vm = new InvoiceProfitCheckViewModel(
                _unitOfWork,
                _productPriceService,
                ShowProductPricing,
                ShowProductDiscount);
            await vm.LoadAsync(productRows, InvoiceDiscountType, InvoiceDiscountValue);

            var owner = Application.Current.MainWindow;
            var result = InvoiceProfitCheckDialog.Show(owner, vm);
            if (result == true && ShowProductDiscount)
            {
                InvoiceDiscountType = vm.DiscountType;
                InvoiceDiscountValue = vm.DiscountValue;
                RecalculateTotals();
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر فحص ربح الفاتورة:\n{ex.Message}");
        }
    }

    private async void OnProductPickerConfirmed()
    {
        var picks = ProductPicker.BuildResults();
        IsProductPickerOpen = false;

        foreach (var pick in picks.Where(p => p.Quantity > 0))
        {
            var handled = await TryPromptClothingSizesAsync(
                pick.Product,
                pick.SuggestedUnitPrice,
                pick.PricingTypeId,
                pick.PricingTypeName,
                seedQuantities: null);
            if (handled)
                continue;

            InvoiceProductMergeHelper.Merge(
                [pick],
                Items,
                WireItemRow,
                UnwireItemRow);
        }

        foreach (var row in Items.Where(i => i.ProductId is > 0).ToList())
            await LoadRowFeatureDataAsync(row);

        RecalculateTotals();
    }

    private void WireItemRow(InvoiceItemRow row)
    {
        row.ProductDiscountFeatureEnabled = ShowProductDiscount;
        row.RefreshProductDiscount();
        row.TotalChanged += OnItemRowTotalChanged;
        row.ProductChanged += OnProductChanged;
    }

    private void UnwireItemRow(InvoiceItemRow row)
    {
        row.TotalChanged -= OnItemRowTotalChanged;
        row.ProductChanged -= OnProductChanged;
    }

    private void OnItemRowTotalChanged()
    {
        RecalculateTotals();
        if (_isApplyingOffers) return;
        _ = RefreshOfferGiftsAsync().ContinueWith(_ =>
            System.Windows.Application.Current.Dispatcher.Invoke(RecalculateTotals));
    }

    [RelayCommand]
    private async Task ProcessBarcodeAsync()
    {
        InvoiceItemRow? updatedRow = null;
        if (!InvoiceBarcodeHelper.TryAddByBarcode(
                BarcodeInput,
                Products,
                Items,
                WireItemRow,
                UnwireItemRow,
                row => updatedRow = row,
                out var error))
        {
            BeautifulMessageDialog.ShowWarning(error);
            return;
        }

        BarcodeInput = string.Empty;

        if (updatedRow?.SelectedProduct is Product product)
        {
            var handled = await TryPromptClothingSizesAsync(
                product,
                updatedRow.UnitPrice,
                updatedRow.PricingTypeId,
                updatedRow.PricingTypeName,
                replaceRow: updatedRow.ProductSizeId is null ? updatedRow : null);
            if (handled)
            {
                RecalculateTotals();
                return;
            }
        }

        if (updatedRow is not null)
            await RefreshProductRowAsync(updatedRow);

        RecalculateTotals();
    }

    [RelayCommand]
    private void IncreaseRowQuantity(InvoiceItemRow? row)
    {
        if (row is null || row.IsOfferGift) return;
        row.Quantity += 1;
        RecalculateTotals();
        _ = RefreshOfferGiftsAsync().ContinueWith(_ =>
            System.Windows.Application.Current.Dispatcher.Invoke(RecalculateTotals));
    }

    [RelayCommand]
    private void DecreaseRowQuantity(InvoiceItemRow? row)
    {
        if (row is null || row.IsOfferGift || row.Quantity <= 1) return;
        row.Quantity -= 1;
        RecalculateTotals();
        _ = RefreshOfferGiftsAsync().ContinueWith(_ =>
            System.Windows.Application.Current.Dispatcher.Invoke(RecalculateTotals));
    }

    [RelayCommand]
    private void RemoveRow(InvoiceItemRow? row)
    {
        if (row is null) return;
        if (row.IsOfferGift) return; // تُدار تلقائياً مع العروض
        UnwireItemRow(row);
        Items.Remove(row);
        RecalculateTotals();
        _ = RefreshOfferGiftsAsync().ContinueWith(_ =>
            System.Windows.Application.Current.Dispatcher.Invoke(RecalculateTotals));
    }

    private async void OnProductChanged(InvoiceItemRow row)
    {
        if (row.IsOfferGift) return;
        await RefreshProductRowAsync(row);
        await RefreshOfferGiftsAsync();
        RecalculateTotals();
    }

    private async Task RefreshProductRowAsync(InvoiceItemRow row)
    {
        if (row.ProductId is null)
        {
            row.StockInfo = string.Empty;
            row.AvailableStock = 0;
            row.AvailablePricingOptions.Clear();
            row.SetSelectedPricingOptionWithoutPrice(null);
            return;
        }

        try
        {
            if (ShowClothingSizes
                && row.SelectedProduct is Product product
                && row.ProductSizeId is null
                && string.IsNullOrWhiteSpace(row.SizeName)
                && _productSizeService is not null
                && await _productSizeService.HasSizesAsync(product.Id))
            {
                await TryPromptClothingSizesAsync(
                    product,
                    row.UnitPrice,
                    row.PricingTypeId,
                    row.PricingTypeName,
                    replaceRow: row);
                return;
            }

            var stocks = await _unitOfWork.WarehouseStocks.FindAsync(s => s.ProductId == row.ProductId.Value);
            var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
            var warehouseDict = warehouses.ToDictionary(w => w.Id, w => w.Name);

            var lines = stocks
                .Where(s => s.Quantity != 0 && warehouseDict.ContainsKey(s.WarehouseId))
                .Select(s => $"{warehouseDict[s.WarehouseId]}: {s.Quantity:N0}")
                .ToList();

            row.StockInfo = lines.Count > 0 ? string.Join(" | ", lines) : "لا يوجد رصيد";

            if (SelectedWarehouse is not null)
                row.AvailableStock = stocks.FirstOrDefault(s => s.WarehouseId == SelectedWarehouse.Id)?.Quantity ?? 0;
            else
                row.AvailableStock = stocks.Where(s => warehouseDict.ContainsKey(s.WarehouseId)).Sum(s => s.Quantity);

            await LoadRowFeatureDataAsync(row);
        }
        catch { row.StockInfo = string.Empty; }
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RecalculateTotals();
        ScheduleDraftSave();
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

        var totalDiscount = InvoiceDiscountAmount + (ShowLoyaltyPanel ? Math.Max(0m, LoyaltyDiscountAmount) : 0m);

        var (computedSub, discount, rounding, grand) = InvoiceTotalsCalculator.Compute(
            Items.Select(i => i.TotalPrice),
            _invoiceService,
            InvoiceType.Sale,
            totalDiscount,
            ShowTransportFee ? TransportFeeAmount : 0m);
        _ = computedSub;
        _ = discount;

        RoundingAmount = rounding;
        GrandTotal = grand;

        if (IsCreditPayment)
        {
            if (CreditPaidAmount < 0m)
                CreditPaidAmount = 0m;
            if (CreditPaidAmount > grand)
                CreditPaidAmount = grand;
            CreditRemainingAmount = Math.Max(0m, grand - CreditPaidAmount);
        }
        else
        {
            CreditPaidAmount = 0m;
            CreditRemainingAmount = 0m;
        }
    }

    partial void OnInvoiceDiscountValueChanged(decimal value) => RecalculateTotals();
    partial void OnTransportFeeAmountChanged(decimal value) => RecalculateTotals();

    // ── Save ───────────────────────────────────────────────
    [RelayCommand]
    private async Task SaveInvoice()
    {
        ErrorMessage = string.Empty;

        // Validation
        if (SelectedWarehouse is null)
        {
            ErrorMessage = "يرجى اختيار المخزن";
            return;
        }

        if (IsCashPayment && SelectedCashBox is null && !IsDamageMode)
        {
            ErrorMessage = "يرجى اختيار القاصة";
            return;
        }

        if (IsDamageMode && _featureFlags is not null && !_featureFlags.DamageInvoices)
        {
            ErrorMessage = "فعّل «فاتورة التلف» من إعدادات الميزات أولاً";
            return;
        }

        if (IsCreditPayment && CreditDueDate is null)
        {
            ErrorMessage = "يرجى تحديد تاريخ استحقاق التسديد للدفع الآجل";
            return;
        }

        if (IsCreditPayment && CreditDueDate.HasValue && CreditDueDate.Value.Date <= InvoiceDate.Date)
        {
            ErrorMessage = "تاريخ الاستحقاق يجب أن يكون بعد تاريخ الفاتورة";
            return;
        }

        if (IsCreditPayment && CreditPaidAmount < 0m)
        {
            ErrorMessage = "المبلغ المدفوع لا يمكن أن يكون سالباً";
            return;
        }

        if (IsCreditPayment && CreditPaidAmount > GrandTotal)
        {
            ErrorMessage = "المبلغ المدفوع لا يمكن أن يتجاوز إجمالي الفاتورة";
            return;
        }

        if (IsInstallmentPayment)
        {
            ErrorMessage = "لفواتير الأقساط استخدم شاشة «فاتورة أقساط» من القائمة الجانبية";
            return;
        }

        var validItems = Items
            .Where(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity != 0
                        && (i.IsOfferGift || i.UnitPrice > 0 || i.TotalPrice != 0))
            .ToList();

        if (validItems.Count == 0)
        {
            ErrorMessage = "يجب إضافة عنصر واحد على الأقل بالكمية والسعر";
            return;
        }

        if (ShowClothingSizes && _productSizeService is not null)
        {
            foreach (var row in validItems.Where(r => r.ProductId is > 0))
            {
                if (row.ProductSizeId is not null) continue;
                if (!await _productSizeService.HasSizesAsync(row.ProductId!.Value)) continue;
                ErrorMessage = $"المنتج «{row.ItemName}» يتطلب اختيار القياس. افتح اختيار المنتجات أو أعد تحديد المنتج.";
                return;
            }
        }

        if (IsReturnMode && _featureFlags is not null && !_featureFlags.SalesReturns)
        {
            ErrorMessage = "فعّل «مرتجع مبيعات» من إعدادات الميزات أولاً";
            return;
        }

        // Stock validation for sales/damage (not returns — returns increase stock)
        if (SelectedWarehouse is not null && !IsReturnMode)
        {
            foreach (var item in validItems.Where(i => i.ProductId.HasValue))
            {
                var stocks = await _unitOfWork.WarehouseStocks.FindAsync(
                    s => s.WarehouseId == SelectedWarehouse.Id && s.ProductId == item.ProductId!.Value);
                var available = stocks.FirstOrDefault()?.Quantity ?? 0;
                var requiredQty = Math.Abs(InvoiceCustomFieldsHelper.ToStockQuantity(item));
                if (requiredQty > available)
                {
                    ErrorMessage = $"الكمية المطلوبة من '{item.ItemName}' ({requiredQty:N0}) تتجاوز الرصيد المتاح ({available:N0}) في المخزن '{SelectedWarehouse.Name}'";
                    return;
                }
            }

            if (_featureFlags?.ExpiryTracking == true && _productBatchService is not null)
            {
                foreach (var item in validItems.Where(i => i.ProductId.HasValue))
                {
                    var stockQty = Math.Abs(InvoiceCustomFieldsHelper.ToStockQuantity(item));
                    if (stockQty <= 0) continue;

                    try
                    {
                        if (item.BatchId is int batchId)
                        {
                            var selected = item.AvailableBatches.FirstOrDefault(b => b.Id == batchId);
                            if (selected is not null && selected.Quantity >= stockQty)
                                continue;
                        }

                        await _productBatchService.AllocateFefoAsync(
                            item.ProductId!.Value, SelectedWarehouse.Id, stockQty);
                    }
                    catch (InvalidOperationException ex)
                    {
                        ErrorMessage = $"«{item.ItemName}»: {ex.Message}";
                        return;
                    }
                }
            }
        }

        if (IsCreditPayment && SelectedCustomer is not null && !IsReturnMode && !IsDamageMode)
        {
            var creditCheck = await _customerCreditService.CheckCreditAsync(
                SelectedCustomer.Id, GrandTotal, isInstallment: false);
            if (!creditCheck.IsAllowed)
            {
                if (!BeautifulMessageDialog.ShowConfirm(
                        $"{creditCheck.Message}\n\nهل تريد المتابعة رغم تجاوز حد الائتمان؟"))
                    return;
            }
        }

        IsBusy = true;

        try
        {
            // Determine customer
            int? customerId = null;
            if (!IsDamageMode)
            {
                customerId = SelectedCustomer?.Id;

                if (customerId is null && !string.IsNullOrWhiteSpace(CustomerSearchText))
                {
                    var newCustomer = new Customer
                    {
                        Name = CustomerSearchText.Trim(),
                        CreatedBy = _currentUserService.Username,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Customers.AddAsync(newCustomer);
                    await _unitOfWork.SaveChangesAsync();
                    customerId = newCustomer.Id;
                }
            }

            var invoiceType = IsDamageMode
                ? InvoiceType.Damage
                : IsReturnMode
                    ? InvoiceType.SaleReturn
                    : IsInstallmentPayment ? InvoiceType.Installment : InvoiceType.Sale;

            var invoice = new Invoice
            {
                InvoiceNumber = InvoiceNumber,
                InvoiceType = invoiceType,
                CustomerId = customerId,
                DriverId = ShowDriverSelection && !IsDamageMode ? SelectedDriver?.Id : null,
                SalesRepresentativeId = ShowSalesRepSelection && !IsDamageMode ? SelectedSalesRepresentative?.Id : null,
                WarehouseId = SelectedWarehouse.Id,
                PaymentMethod = IsDamageMode ? PaymentMethod.Cash : SelectedPaymentMethod,
                CashBoxId = !IsDamageMode && IsCashPayment && SelectedCashBox is not null ? SelectedCashBox.Id : null,
                Date = InvoiceDate,
                CreditDueDate = !IsDamageMode && IsCreditPayment ? CreditDueDate : null,
                PaidAmount = !IsDamageMode && IsCreditPayment ? Math.Clamp(CreditPaidAmount, 0m, GrandTotal) : 0m,
                DiscountAmount = IsDamageMode
                    ? 0m
                    : (ShowProductDiscount ? InvoiceDiscountAmount : 0m)
                        + (ShowLoyaltyPanel ? Math.Max(0m, LoyaltyDiscountAmount) : 0m),
                LoyaltyRedeemDiscountAmount = !IsDamageMode && ShowLoyaltyPanel ? Math.Max(0m, LoyaltyDiscountAmount) : 0m,
                LoyaltyPointsRedeemed = !IsDamageMode && ShowLoyaltyPanel ? Math.Max(0, LoyaltyRedeemPoints) : 0,
                TransportFeeAmount = !IsDamageMode && ShowTransportFee ? Math.Max(0m, TransportFeeAmount) : 0m,
                RelatedInvoiceId = IsReturnMode ? _relatedInvoiceId : null,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
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

                if (!string.IsNullOrWhiteSpace(row.SizeName))
                {
                    row.CustomField1 = row.SizeName;
                    if (string.IsNullOrWhiteSpace(row.CustomField1Label))
                        row.CustomField1Label = ClothingSizeInvoiceHelper.SizeLabel;
                }

                var displayQty = IsReturnMode ? Math.Abs(row.Quantity) : row.Quantity;
                var stockQty = Math.Abs(InvoiceCustomFieldsHelper.ToStockQuantity(row));
                if (IsReturnMode)
                    stockQty = Math.Abs(stockQty);
                var factor = ProductDiscountHelper.NormalizeConversionFactor(row.UnitConversionFactor);
                var lineGross = displayQty * factor * row.UnitPrice;
                var lineDiscount = ShowProductDiscount ? row.DiscountAmount : 0m;
                if (lineDiscount > Math.Abs(lineGross))
                    lineDiscount = Math.Abs(lineGross);
                var lineTotal = ProductDiscountHelper.CalculateLineTotal(
                    displayQty, row.UnitPrice, lineDiscount, factor);
                invoiceItems.Add(new InvoiceItem
                {
                    ProductId = productId,
                    PricingTypeId = row.PricingTypeId,
                    ItemName = row.ItemName.Trim(),
                    Quantity = stockQty,
                    UnitPrice = row.IsOfferGift ? 0m : row.UnitPrice,
                    DiscountAmount = row.IsOfferGift ? 0m : lineDiscount,
                    TotalPrice = row.IsOfferGift ? 0m : lineTotal,
                    IsOfferGift = row.IsOfferGift,
                    OfferId = row.OfferId,
                    CustomFieldsJson = InvoiceCustomFieldsHelper.ToJson(row, ActiveCustomFieldLabels)
                });
            }

            // اقرأ العلم الحي — لا تعتمد على حالة قديمة إن تغيّرت الميزات أثناء فتح الشاشة
            if (_featureFlags?.SerialNumbers == true)
            {
                foreach (var row in validItems.Where(r => r.ProductId.HasValue && Math.Abs(r.Quantity) >= 1))
                {
                    if (string.IsNullOrWhiteSpace(row.SerialNumber) && !IsReturnMode)
                    {
                        ErrorMessage = $"أدخل الرقم التسلسلي للصنف «{row.ItemName}»";
                        return;
                    }
                }
            }

            Invoice saved;
            if (_editingInvoiceId is int editId)
            {
                saved = await _invoiceService.ReplaceInvoiceAsync(editId, invoice, invoiceItems);
                ClearEditingInvoiceId();
            }
            else
            {
                var applyLoyalty = !IsDamageMode && ShowLoyaltyPanel && customerId is not null;
                saved = await _invoiceService.CreateInvoiceAsync(
                    invoice,
                    invoiceItems,
                    skipStockUpdate: false,
                    loyaltyRedeemPoints: applyLoyalty ? Math.Max(0, LoyaltyRedeemPoints) : 0,
                    applyLoyalty: applyLoyalty);
            }

            try
            {
                await ApplyFeatureSideEffectsOnSaveAsync(validItems, invoiceItems);
            }
            catch (Exception sideEx)
            {
                BeautifulMessageDialog.ShowWarning($"حُفظت الفاتورة مع تحذير الميزات: {sideEx.Message}");
            }

            await TrySaveSalesRepCommissionAsync(saved.Id);

            _savedInvoice = saved;
            _savedItems = invoiceItems;
            IsSaved = true;
            InvoiceNumber = saved.InvoiceNumber;

            _draftService.ClearDraft(DraftKey);
            _recentActivity.Record(
                IsDamageMode ? "فاتورة تلف" : IsReturnMode ? "مرتجع مبيعات" : "فاتورة مبيعات",
                $"{saved.InvoiceNumber} — {saved.NetAmount:N0} د.ع",
                IsDamageMode ? "DamageInvoice" : IsReturnMode ? "SalesReturn" : "SaleInvoice",
                typeof(SalesInvoiceViewModel));

            BeautifulMessageDialog.ShowSuccess(
                $"تم حفظ {(IsDamageMode ? "فاتورة التلف" : IsReturnMode ? "مرتجع المبيعات" : "الفاتورة")} بنجاح\nرقم الفاتورة: {saved.InvoiceNumber}\nالمبلغ الكلي: {saved.NetAmount:N0} د.ع\n\nيمكنك الطباعة أو الإرسال عبر واتساب.");

            PrintInvoice();
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
    private void PrintInvoice()
    {
        if (_savedInvoice is null) return;
        var model = BuildSavedInvoicePrintModel();
        _exportService.PrintInvoice(model);
        if (ShowDriverSelection)
            _exportService.PrintInvoice(BuildWarehouseCopyPrintModel(model));
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

        var paidAmount = _savedInvoice.PaymentMethod == PaymentMethod.Cash
            ? GrandTotal
            : Math.Clamp(_savedInvoice.PaidAmount, 0m, GrandTotal);
        var remainingAmount = Math.Max(0m, GrandTotal - paidAmount);

        return new InvoicePrintModel
        {
            Title = "فاتورة مبيعات",
            InvoiceNumber = _savedInvoice.InvoiceNumber,
            Date = _savedInvoice.Date,
            CreditDueDate = _savedInvoice.CreditDueDate,
            PartyLabel = "العميل",
            PartyName = SelectedCustomer?.Name ?? CustomerSearchText,
            PartyPhone = SelectedCustomer?.Phone,
            PartyAddress = SelectedCustomer?.Address,
            DriverName = ShowDriverSelection ? SelectedDriver?.Name : null,
            SalesRepresentativeName = ShowSalesRepSelection ? SelectedSalesRepresentative?.Name : null,
            WarehouseName = SelectedWarehouse?.Name ?? string.Empty,
            PaymentMethod = _savedInvoice.PaymentMethod switch
            {
                PaymentMethod.Cash => "نقدي",
                PaymentMethod.Credit => "آجل",
                PaymentMethod.Installment => "أقساط",
                _ => "—"
            },
            Notes = _savedInvoice.Notes,
            Subtotal = Subtotal,
            RoundingAmount = RoundingAmount,
            TransportFeeAmount = ShowTransportFee ? TransportFeeAmount : 0m,
            DiscountAmount = _savedInvoice.DiscountAmount,
            PaidAmount = paidAmount,
            RemainingAmount = remainingAmount,
            GrandTotal = GrandTotal,
            Items = _savedItems.Select((item, i) =>
            {
                var usage = ShowPharmacyUsage
                    ? Items.FirstOrDefault(r => r.ProductId == item.ProductId)?.UsageInstructions
                    : null;
                var displayName = InvoiceCustomFieldsHelper.FormatItemDisplayName(
                    item.ItemName,
                    item.CustomFieldsJson);
                if (!string.IsNullOrWhiteSpace(usage))
                    displayName = $"{displayName}\nطريقة الاستخدام: {usage}";

                return new InvoicePrintItem
                {
                    Number = i + 1,
                    UsageInstructions = usage,
                    ItemName = displayName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                };
            }).ToList()
        };
    }

    private static InvoicePrintModel BuildWarehouseCopyPrintModel(InvoicePrintModel source) =>
        new()
        {
            Title = "نسخة مخزن",
            InvoiceNumber = source.InvoiceNumber,
            Date = source.Date,
            CreditDueDate = source.CreditDueDate,
            PartyLabel = source.PartyLabel,
            PartyName = source.PartyName,
            PartyPhone = source.PartyPhone,
            PartyAddress = source.PartyAddress,
            DriverName = source.DriverName,
            SalesRepresentativeName = source.SalesRepresentativeName,
            WarehouseName = source.WarehouseName,
            PaymentMethod = source.PaymentMethod,
            Notes = source.Notes,
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
    private void OpenCurrencyChange()
    {
        IraqiCurrencyChangeDialog.ShowCalculator(GrandTotal);
    }

    [RelayCommand]
    private async Task NewInvoice()
    {
        IsSaved = false;
        ClearEditingInvoiceId();
        _savedInvoice = null;
        _savedItems = [];
        ErrorMessage = string.Empty;
        Notes = string.Empty;
        TransportFeeAmount = 0m;
        CustomerSearchText = string.Empty;
        SelectedCustomer = null;
        SelectedDriver = null;
        SelectedSalesRepresentative = null;
        SelectedPaymentMethod = PaymentMethod.Cash;
        CreditDueDate = null;
        InvoiceDate = DateTime.Now;
        foreach (var item in Items.ToList())
            UnwireItemRow(item);
        Items.Clear();
        AddRow();

        RecalculateTotals();
        if (IsDamageMode)
        {
            InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Damage);
            PageTitle = "فاتورة تلف";
            OnPropertyChanged(nameof(ShowCustomerAndPayment));
            OnPropertyChanged(nameof(ShowCashBox));
        }
        else if (IsReturnMode)
        {
            InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.SaleReturn);
        }
        else
        {
            InvoiceNumber = await _invoiceService.GenerateInvoiceNumberAsync(InvoiceType.Sale);
            ApplyDefaultCustomerIfAny();
        }
        RefreshInvoiceWarnings();
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
    private void ShowSelectedPartyDetails()
    {
        if (SelectedCustomer is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر عميلاً أولاً لعرض تفاصيله");
            return;
        }

        PartyQuickDetailDialog.ShowCustomer(_partyQuickDetail, SelectedCustomer.Id);
    }

    [RelayCommand]
    private void ShowProductDetails(InvoiceItemRow? row)
    {
        if (row?.ProductId is not > 0)
        {
            BeautifulMessageDialog.ShowWarning("اختر منتجاً مسجلاً لعرض تفاصيله");
            return;
        }

        ProductQuickDetailDialog.Show(_productQuickDetail, row.ProductId.Value);
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
