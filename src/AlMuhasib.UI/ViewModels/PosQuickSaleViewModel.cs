using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Threading;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class PosQuickSaleViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInvoiceService _invoiceService;
    private readonly IProductService _productService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPreferencesService _userPreferences;
    private readonly IRecentActivityService _recentActivity;
    private readonly ISoundService _sound;
    private readonly IProductPriceService _productPriceService;
    private readonly IProductBatchService _productBatchService;
    private readonly IFeatureFlagService _featureFlags;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IPosFullscreenService _posFullscreen;
    private readonly InvoiceCostGuard _costGuard;
    private readonly DispatcherTimer _searchDebounce;

    private List<Product> _allProducts = [];
    private Dictionary<int, decimal> _lastSalePrices = [];
    private Dictionary<int, (decimal SaleIqd, decimal SaleUsd)> _catalogListPrices = [];
    private Dictionary<int, int> _defaultPricingTypeByProduct = [];
    private Dictionary<int, decimal> _warehouseQtyByProduct = [];
    private readonly bool _pricingEnabled;

    public ObservableCollection<PosProductTile> FilteredProducts { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<PosCartLine> CartLines { get; } = [];
    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private Warehouse? _selectedWarehouse;
    [ObservableProperty] private CashBox? _selectedCashBox;
    // SelectedPosCurrency / FilteredCashBoxes / ShowMultiCurrency live in TouchUi partial.
    [ObservableProperty] private decimal _subTotal;
    [ObservableProperty] private decimal _invoiceDiscountAmount;
    [ObservableProperty] private DiscountType _invoiceDiscountType = DiscountType.None;
    [ObservableProperty] private decimal _invoiceDiscountValue;
    [ObservableProperty] private decimal _grandTotal;
    [ObservableProperty] private decimal _paidAmount;
    [ObservableProperty] private decimal _changeAmount;
    [ObservableProperty] private decimal _creditRemainingAmount;
    [ObservableProperty] private bool _showChangeDue;
    [ObservableProperty] private bool _showCreditRemaining;
    [ObservableProperty] private bool _isPaymentDialogOpen;
    [ObservableProperty] private DateTime? _creditDueDate;
    [ObservableProperty] private int _cartLineCount;
    [ObservableProperty] private bool _showProductDiscount;
    [ObservableProperty] private string _currencyAmountSuffix = AccountingCurrencyHelper.IqdLabel;
    [ObservableProperty] private string _statusMessage = "امسح الباركود أو ابحث بالاسم ثم Enter للإضافة";
    [ObservableProperty] private string? _lastSavedInvoiceNumber;
    [ObservableProperty] private bool _printAfterSale = true;
    [ObservableProperty] private bool _isFullscreenActive;

    public IReadOnlyList<DiscountTypeOption> InvoiceDiscountTypeOptions { get; } =
    [
        new(DiscountType.None, "بدون خصم كلي"),
        new(DiscountType.Percentage, "نسبة مئوية (%)"),
        new(DiscountType.FixedAmount, "قيمة ثابتة")
    ];

    public string InvoiceDiscountValueHint => InvoiceDiscountType switch
    {
        DiscountType.Percentage => "نسبة الخصم (%)",
        DiscountType.FixedAmount => $"مبلغ الخصم ({CurrencyAmountSuffix})",
        _ => "قيمة"
    };

    public string PaidAmountHint => $"المبلغ المدفوع ({CurrencyAmountSuffix})";

    [ObservableProperty] private DiscountTypeOption? _selectedInvoiceDiscountOption;

    private bool _printAfterConfirm;

    public PosQuickSaleViewModel(
        IUnitOfWork unitOfWork,
        IInvoiceService invoiceService,
        IProductService productService,
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences,
        IRecentActivityService recentActivity,
        ISoundService sound,
        IProductPriceService productPriceService,
        IProductBatchService productBatchService,
        IFeatureFlagService featureFlags,
        IProductSerialService productSerialService,
        IProductSizeService productSizeService,
        IProductColorService productColorService,
        ILoyaltyService loyaltyService,
        IProductOfferService productOfferService,
        IPosFullscreenService posFullscreen,
        IExchangeRateService exchangeRateService)
    {
        _unitOfWork = unitOfWork;
        _invoiceService = invoiceService;
        _productService = productService;
        _sound = sound;
        _currentUserService = currentUserService;
        _userPreferences = userPreferences;
        _recentActivity = recentActivity;
        _productPriceService = productPriceService;
        _productBatchService = productBatchService;
        _featureFlags = featureFlags;
        _exchangeRateService = exchangeRateService;
        _pricingEnabled = userPreferences.Current.FeatureFlags.ProductPricingEnabled;
        _costGuard = new InvoiceCostGuard(unitOfWork, productPriceService, _pricingEnabled);
        PageTitle = "بيع سريع (POS)";
        SelectedInvoiceDiscountOption = InvoiceDiscountTypeOptions[0];
        ConfigurePosFeatureServices(productSerialService, productSizeService, productColorService);
        ConfigureLoyaltyService(loyaltyService);
        ConfigureProductOfferService(productOfferService);
        _posFullscreen = posFullscreen;

        CartLines.CollectionChanged += OnCartChanged;

        _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _searchDebounce.Tick += (_, _) =>
        {
            _searchDebounce.Stop();
            RefreshFilteredProducts();
        };
    }

    partial void OnSelectedInvoiceDiscountOptionChanged(DiscountTypeOption? value)
    {
        if (value is not null)
            InvoiceDiscountType = value.Type;
        RecalcCartTotals();
    }

    partial void OnInvoiceDiscountTypeChanged(DiscountType value)
    {
        OnPropertyChanged(nameof(InvoiceDiscountValueHint));
        RecalcCartTotals();
    }
    partial void OnInvoiceDiscountValueChanged(decimal value) => RecalcCartTotals();

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "SaleInvoice");
        if (!CanAdd)
        {
            StatusMessage = "ليس لديك صلاحية إنشاء فواتير مبيعات";
            return;
        }

        IsBusy = true;
        try
        {
            var products = (await _productService.GetVisibleInCurrentBranchAsync()).OrderBy(p => p.Name).ToList();
            _allProducts = products;

            Categories.Clear();
            foreach (var category in (await _unitOfWork.Categories.GetAllAsync()).OrderBy(c => c.Name))
                Categories.Add(category);
            SelectedCategory = Categories.FirstOrDefault();

            await LoadSuggestedPricesAsync();
            if (_pricingEnabled)
                await LoadCatalogPricesAsync();

            Warehouses.Clear();
            foreach (var w in await _unitOfWork.Warehouses.GetAllAsync())
                Warehouses.Add(w);

            RefreshMultiCurrencyVisibility();
            SelectedPosCurrency = AccountingCurrency.IQD;
            SeedCashBoxes(await _unitOfWork.CashBoxes.GetAllAsync());

            var prefs = _userPreferences.Current;
            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == prefs.DefaultPosWarehouseId)
                                ?? Warehouses.FirstOrDefault();

            var preferredCash = _allCashBoxes.FirstOrDefault(c => c.Id == prefs.DefaultPosCashBoxId)
                                ?? _allCashBoxes.FirstOrDefault(c => c.Currency == AccountingCurrency.IQD)
                                ?? _allCashBoxes.FirstOrDefault();
            RefreshFilteredCashBoxes(preferKeepSelection: false);
            if (preferredCash is not null &&
                (!ShowMultiCurrency || preferredCash.Currency == AccountingCurrency.IQD))
                SelectedCashBox = FilteredCashBoxes.FirstOrDefault(c => c.Id == preferredCash.Id)
                                  ?? FilteredCashBoxes.FirstOrDefault();

            await LoadPosCustomersAsync();
            await LoadHeldInvoicesAsync();
            await LoadBulkPricingTypesAsync();
            await LoadRecentInvoicesAsync();
            await LoadWarehouseStockAsync();

            RefreshFilteredProducts();
            RefreshCurrencyAmountSuffix();
            await RefreshFxAndEquivalentAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        // أثناء البحث النصي نتجاهل فلتر الصنف؛ عند اختيار صنف نفرّغ البحث لنرى منتجات الصنف.
        if (value is not null && !string.IsNullOrWhiteSpace(SearchText))
            SearchText = string.Empty;
        else
            RefreshFilteredProducts();
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    partial void OnSelectedWarehouseChanged(Warehouse? value)
    {
        PersistPosDefaults();
        _ = ReloadWarehouseStockAndProductsAsync();
    }

    partial void OnSelectedCashBoxChanged(CashBox? value)
    {
        PersistPosDefaults();
        // POS stays IQD; do not switch document currency from cash-box currency.
        SelectedPosCurrency = AccountingCurrency.IQD;
        RefreshCurrencyAmountSuffix();
        RefreshFilteredProducts();
        RequoteCartForCashBoxCurrency();
        _ = RefreshFxAndEquivalentAsync();
    }

    partial void OnPaidAmountChanged(decimal value) => RecalcChange();

    private AccountingCurrency PosDocumentCurrency => AccountingCurrency.IQD;

    private void RefreshCurrencyAmountSuffix()
    {
        CurrencyAmountSuffix = AccountingCurrencyHelper.GetLabel(PosDocumentCurrency);
        OnPropertyChanged(nameof(InvoiceDiscountValueHint));
        OnPropertyChanged(nameof(PaidAmountHint));
    }

    private decimal ResolveSuggestedPrice(int productId)
    {
        if (_pricingEnabled && _catalogListPrices.TryGetValue(productId, out var catalog))
        {
            var list = ProductListPriceHelper.ResolveListPrice(
                catalog.SaleIqd, catalog.SaleUsd, PosDocumentCurrency);
            if (list > 0 || PosDocumentCurrency == AccountingCurrency.USD)
                return list;
        }

        if (PosDocumentCurrency == AccountingCurrency.USD)
            return 0m;

        return _lastSalePrices.GetValueOrDefault(productId);
    }

    private void RequoteCartForCashBoxCurrency()
    {
        if (!ShowMultiCurrency)
            return;

        foreach (var line in CartLines.Where(l => !l.IsOfferGift && l.ProductId > 0))
            line.UnitPrice = ResolveSuggestedPrice(line.ProductId);

        RecalcCartTotals();
    }

    [RelayCommand]
    private async Task AddProductFromSearch()
    {
        var term = SearchText.Trim();
        if (string.IsNullOrEmpty(term))
            return;

        // أوقف debounce وحدّث القائمة فوراً حتى لا تُضاف مادة قديمة بالخطأ
        _searchDebounce.Stop();
        RefreshFilteredProducts();

        var barcodeMatch = _allProducts.FirstOrDefault(p =>
            !string.IsNullOrEmpty(p.Barcode) &&
            p.Barcode.Equals(term, StringComparison.OrdinalIgnoreCase));

        if (barcodeMatch is not null)
        {
            if (!barcodeMatch.ShowInCashier)
            {
                NotifyPosValidationError($"المنتج غير متاح في الكاشير — الباركود: {term}");
                SearchText = string.Empty;
                return;
            }

            if (!EnsureStockAllowsAdd(barcodeMatch, additionalQty: 1m))
            {
                SearchText = string.Empty;
                return;
            }

            _sound.Play(SoundEffect.Scan);
            await AddOrIncrementProductAsync(barcodeMatch);
            SearchText = string.Empty;
            return;
        }

        // مسح باركود (أرقام/رموز) بدون تطابق تام → خطأ، لا تأخذ أول نتيجة بحث جزئي
        if (LooksLikeBarcodeScan(term))
        {
            NotifyPosValidationError($"لم يُعثر على منتج بهذا الباركود: {term}");
            SearchText = string.Empty;
            return;
        }

        var first = FilteredProducts.FirstOrDefault();
        if (first is not null)
        {
            if (!EnsureStockAllowsAdd(first.Product, additionalQty: 1m))
            {
                SearchText = string.Empty;
                return;
            }

            await AddOrIncrementProductAsync(first.Product);
            SearchText = string.Empty;
            return;
        }

        NotifyPosValidationError($"لم يُعثر على منتج بهذا الاسم: {term}");
        SearchText = string.Empty;
    }

    private static bool LooksLikeBarcodeScan(string term)
    {
        if (term.Length < 4)
            return false;

        // قارئات الباركود تُرسل أرقاماً أو رموزاً بدون مسافات
        if (term.Contains(' '))
            return false;

        var alnum = 0;
        var digits = 0;
        foreach (var ch in term)
        {
            if (char.IsLetterOrDigit(ch))
                alnum++;
            if (char.IsDigit(ch))
                digits++;
        }

        return alnum == term.Length && digits >= term.Length * 0.7;
    }

    private void NotifyPosValidationError(string message)
    {
        StatusMessage = message;
        // حوار واضح + صوت خطأ فوري (التوست قد لا يظهر أو يُفوت أثناء المسح)
        BeautifulMessageDialog.ShowErrorDialog(message);
    }

    private void NotifyPosValidationWarning(string message)
    {
        StatusMessage = message;
        BeautifulMessageDialog.ShowWarning(message);
    }

    /// <summary>يرجع false إن تجاوزت الكمية رصيد المخزن.</summary>
    private bool EnsureStockAllowsAdd(Product product, decimal additionalQty)
    {
        if (SelectedWarehouse is null)
            return true;

        var available = _warehouseQtyByProduct.GetValueOrDefault(product.Id);
        var inCart = CartLines.Where(l => l.ProductId == product.Id).Sum(l => l.Quantity);
        if (inCart + additionalQty <= available)
            return true;

        NotifyPosValidationError(
            available <= 0
                ? $"«{product.Name}» غير متوفر في المخزن"
                : $"«{product.Name}» — الكمية المطلوبة تتجاوز الرصيد ({available:N0})");
        return false;
    }

    [RelayCommand]
    private async Task AddProduct(PosProductTile? tile)
    {
        if (tile?.Product is null) return;
        if (!EnsureStockAllowsAdd(tile.Product, additionalQty: 1m))
            return;
        await AddOrIncrementProductAsync(tile.Product);
    }

    [RelayCommand]
    private void RemoveLine(PosCartLine? line)
    {
        if (line is null || line.IsOfferGift) return;
        CartLines.Remove(line);
        _ = RefreshOfferGiftsAsync();
    }

    [RelayCommand]
    private void IncreaseLine(PosCartLine? line)
    {
        if (line is null || line.IsOfferGift) return;
        var product = _allProducts.FirstOrDefault(p => p.Id == line.ProductId);
        if (product is not null && !EnsureStockAllowsAdd(product, additionalQty: 1m))
            return;
        line.Quantity += 1;
        _ = RefreshOfferGiftsAsync();
    }

    [RelayCommand]
    private void DecreaseLine(PosCartLine? line)
    {
        if (line is null || line.IsOfferGift) return;
        if (line.Quantity <= 1)
            CartLines.Remove(line);
        else
            line.Quantity -= 1;
        _ = RefreshOfferGiftsAsync();
    }

    [RelayCommand]
    private void ClearCart()
    {
        if (CartLines.Count == 0) return;
        if (!BeautifulMessageDialog.ShowConfirm("مسح كل بنود السلة؟")) return;
        CartLines.Clear();
        PaidAmount = 0;
        ClearActiveHeldInvoice();
        StatusMessage = "تم مسح السلة";
    }

    [RelayCommand]
    private void OpenPaymentDialog()
    {
        if (IsInstallmentMode)
        {
            _ = CompleteInstallmentSaleCoreAsync();
            return;
        }

        if (!CanOpenPaymentDialog()) return;
        _printAfterConfirm = PrintAfterSale;
        PaidAmount = GrandTotal;
        RecalcChange();
        IsPaymentDialogOpen = true;
    }

    /// <summary>
    /// إتمام البيع نقداً بالمبلغ الكامل وطباعة مباشرة دون واجهة الدفع أو حوار الطباعة (F12).
    /// </summary>
    [RelayCommand]
    private async Task OpenPaymentDialogAndPrintAsync()
    {
        if (IsInstallmentMode)
        {
            await CompleteInstallmentSaleCoreAsync();
            return;
        }

        if (!CanOpenPaymentDialog()) return;
        PaidAmount = GrandTotal;
        CreditDueDate = DateTime.Today.AddMonths(1);
        RecalcChange();
        await CompleteSaleCoreAsync(printReceipt: true, silentPrint: true);
    }

    [RelayCommand]
    private void ClosePaymentDialog()
    {
        IsPaymentDialogOpen = false;
    }

    [RelayCommand]
    private void OpenCurrencyChange()
    {
        if (PosDocumentCurrency == AccountingCurrency.USD)
        {
            BeautifulMessageDialog.ShowInfo("حاسبة فئات الدينار متاحة لفواتير الدينار فقط.");
            return;
        }

        var applied = IraqiCurrencyChangeDialog.Show(
            invoiceTotal: GrandTotal,
            allowApplyPaid: true);

        if (applied is null)
            return;

        PaidAmount = applied.Value;
        RecalcChange();

        // If payment dialog already open, just update paid amount; otherwise open it.
        if (IsPaymentDialogOpen)
            return;

        if (CanOpenPaymentDialog())
        {
            _printAfterConfirm = PrintAfterSale;
            IsPaymentDialogOpen = true;
        }
    }

    [RelayCommand]
    private async Task ConfirmPaymentAsync()
    {
        if (!IsPaymentDialogOpen) return;

        if (PaidAmount < 0)
        {
            BeautifulMessageDialog.ShowWarning("المبلغ المدفوع غير صالح");
            return;
        }

        var isCredit = PaidAmount < GrandTotal;
        if (isCredit)
        {
            if (SelectedPosCustomer is null && _userPreferences.Current.DefaultSalesCustomerId is null)
            {
                BeautifulMessageDialog.ShowWarning("اختر عميلاً للبيع الآجل");
                return;
            }
        }

        IsPaymentDialogOpen = false;
        await CompleteSaleCoreAsync(printReceipt: _printAfterConfirm, silentPrint: false);
    }

    private bool CanOpenPaymentDialog()
    {
        if (IsInstallmentMode)
            return true;

        if (!CanAdd)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية إنشاء فواتير مبيعات");
            return false;
        }

        if (SelectedWarehouse is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر المخزن");
            return false;
        }

        if (SelectedCashBox is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر القاصة");
            return false;
        }

        if (CartLines.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("السلة فارغة");
            return false;
        }

        var invalid = CartLines.Where(l => !l.IsOfferGift && l.UnitPrice <= 0).ToList();
        if (invalid.Count > 0)
        {
            BeautifulMessageDialog.ShowWarning("أدخل سعراً لكل البنود");
            return false;
        }

        return true;
    }

    [RelayCommand]
    private async Task CompleteSaleAsync() => await OpenPaymentDialogAndCompleteAsync(printReceipt: PrintAfterSale);

    [RelayCommand]
    private async Task CompleteSaleAndPrintAsync() => await OpenPaymentDialogAndCompleteAsync(printReceipt: true);

    private async Task OpenPaymentDialogAndCompleteAsync(bool printReceipt)
    {
        if (IsInstallmentMode)
        {
            await CompleteInstallmentSaleCoreAsync();
            return;
        }

        if (!CanOpenPaymentDialog()) return;
        _printAfterConfirm = printReceipt;
        PaidAmount = GrandTotal;
        CreditDueDate = DateTime.Today.AddMonths(1);
        RecalcChange();
        IsPaymentDialogOpen = true;
    }

    private async Task CompleteSaleCoreAsync(bool printReceipt, bool silentPrint = false)
    {
        if (IsInstallmentMode)
        {
            await CompleteInstallmentSaleCoreAsync();
            return;
        }

        if (!CanAdd) return;

        if (SelectedWarehouse is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر المخزن");
            return;
        }

        if (SelectedCashBox is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر القاصة");
            return;
        }

        if (CartLines.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("السلة فارغة");
            return;
        }

        var invalid = CartLines.Where(l => !l.IsOfferGift && l.UnitPrice <= 0).ToList();
        if (invalid.Count > 0)
        {
            BeautifulMessageDialog.ShowWarning("أدخل سعراً لكل البنود");
            return;
        }

        foreach (var line in CartLines)
        {
            var stocks = await _unitOfWork.WarehouseStocks.FindAsync(
                s => s.WarehouseId == SelectedWarehouse.Id && s.ProductId == line.ProductId);
            var available = stocks.FirstOrDefault()?.Quantity ?? 0;
            var needed = line.Quantity;
            if (needed > available)
            {
                BeautifulMessageDialog.ShowWarning(
                    $"الكمية من «{line.ProductName}» ({needed:N0}) تتجاوز الرصيد ({available:N0})");
                return;
            }
        }

        if (ShowExpiryTracking)
        {
            foreach (var line in CartLines)
            {
                if (line.BatchId is int batchId)
                {
                    var batch = line.AvailableBatches.FirstOrDefault(b => b.Id == batchId);
                    if (batch is not null && batch.Quantity < line.Quantity)
                    {
                        BeautifulMessageDialog.ShowWarning(
                            $"«{line.ProductName}»: كمية الدفعة غير كافية");
                        return;
                    }
                    continue;
                }

                try
                {
                    await _productBatchService.AllocateFefoAsync(
                        line.ProductId, SelectedWarehouse.Id, line.Quantity);
                }
                catch (InvalidOperationException ex)
                {
                    BeautifulMessageDialog.ShowWarning($"«{line.ProductName}»: {ex.Message}");
                    return;
                }
            }
        }

        if (ShowSerialNumbers)
        {
            foreach (var line in CartLines.Where(l => string.IsNullOrWhiteSpace(l.SerialNumber) == false))
            {
                // ok — serial optional unless product has available serials and none selected
            }

            var missingSerial = CartLines.FirstOrDefault(l =>
                l.AvailableSerials.Count > 0 && string.IsNullOrWhiteSpace(l.SerialNumber));
            if (missingSerial is not null)
            {
                BeautifulMessageDialog.ShowWarning($"اختر سيريال لـ «{missingSerial.ProductName}»");
                return;
            }
        }

        var isCredit = PaidAmount < GrandTotal;
        int? customerId = SelectedPosCustomer?.Id ?? _userPreferences.Current.DefaultSalesCustomerId;
        if (isCredit && customerId is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر عميلاً للبيع الآجل");
            return;
        }

        if (isCredit && CreditDueDate is null)
        {
            BeautifulMessageDialog.ShowWarning("حدد تاريخ استحقاق التسديد للآجل");
            return;
        }

        if (isCredit && CreditDueDate.HasValue && CreditDueDate.Value.Date <= DateTime.Today)
        {
            BeautifulMessageDialog.ShowWarning("تاريخ الاستحقاق يجب أن يكون بعد اليوم");
            return;
        }

        var belowCostCurrency = PosDocumentCurrency;
        var belowCost = await _costGuard.FindBelowCostLinesAsync(
            BuildCartProfitRows(), ShowProductDiscount, belowCostCurrency);
        if (belowCost.Count > 0)
        {
            var msg = InvoiceCostGuard.FormatBelowCostMessage(belowCost, belowCostCurrency);
            if (!InvoiceValidationDialog.ShowWarningConfirm($"{msg}\n\nهل تريد المتابعة بالبيع؟"))
                return;
        }

        IsBusy = true;
        try
        {
            var cartSnapshot = CartLines.ToList();
            var totalSnapshot = GrandTotal;
            var paidSnapshot = Math.Min(PaidAmount, GrandTotal);
            var currency = PosDocumentCurrency;
            var fxRate = await ResolveFxRateAsync(currency);
            if (currency == AccountingCurrency.USD && fxRate <= 0)
            {
                BeautifulMessageDialog.ShowWarning("سعر الصرف مطلوب لبيع بالدولار. سجّل سعر الصرف اليومي أولاً.");
                return;
            }

            var invoice = new Invoice
            {
                InvoiceType = InvoiceType.Sale,
                CustomerId = customerId,
                WarehouseId = SelectedWarehouse.Id,
                PaymentMethod = isCredit ? PaymentMethod.Credit : PaymentMethod.Cash,
                CashBoxId = SelectedCashBox.Id,
                Currency = currency,
                FxRate = fxRate,
                Date = DateTime.Now,
                DiscountAmount = (ShowProductDiscount ? InvoiceDiscountAmount : 0m)
                    + (ShowLoyaltyPanel ? Math.Max(0m, LoyaltyDiscountAmount) : 0m),
                LoyaltyRedeemDiscountAmount = ShowLoyaltyPanel ? Math.Max(0m, LoyaltyDiscountAmount) : 0m,
                LoyaltyPointsRedeemed = ShowLoyaltyPanel ? Math.Max(0, LoyaltyRedeemPoints) : 0,
                PaidAmount = isCredit ? paidSnapshot : GrandTotal,
                CreditDueDate = isCredit ? CreditDueDate : null,
                Notes = isCredit ? "بيع سريع POS — آجل" : "بيع سريع POS"
            };

            var items = cartSnapshot.Select(line => new InvoiceItem
            {
                ProductId = line.ProductId,
                PricingTypeId = line.PricingTypeId,
                ItemName = line.ProductName,
                Quantity = line.Quantity,
                UnitPrice = line.IsOfferGift ? 0m : line.UnitPrice,
                DiscountAmount = line.IsOfferGift ? 0m : (ShowProductDiscount ? line.DiscountAmount : 0m),
                TotalPrice = line.IsOfferGift ? 0m : line.LineTotal,
                IsOfferGift = line.IsOfferGift,
                OfferId = line.OfferId,
                CustomFieldsJson = line.ToCustomFieldsJson()
            }).ToList();

            var applyLoyalty = ShowLoyaltyPanel && customerId is not null;
            var saved = await _invoiceService.CreateInvoiceAsync(
                invoice,
                items,
                skipStockUpdate: false,
                loyaltyRedeemPoints: applyLoyalty ? Math.Max(0, LoyaltyRedeemPoints) : 0,
                applyLoyalty: applyLoyalty);
            LastSavedInvoiceNumber = saved.InvoiceNumber;

            await ApplyPosFeatureSideEffectsOnSaveAsync(cartSnapshot, items);
            await DiscardActiveHeldInvoiceAsync();

            _recentActivity.Record(
                "بيع سريع",
                $"{saved.InvoiceNumber} — {AccountingCurrencyHelper.Format(saved.NetAmount, currency)}",
                "SaleInvoice",
                typeof(PosQuickSaleViewModel));

            CartLines.Clear();
            PaidAmount = 0;
            LoyaltyRedeemPoints = 0;
            LoyaltyDiscountAmount = 0m;
            StatusMessage = isCredit
                ? $"تم البيع الآجل — {saved.InvoiceNumber} — مدفوع {AccountingCurrencyHelper.Format(paidSnapshot, currency)} — متبقي {AccountingCurrencyHelper.Format(saved.RemainingAmount, currency)}"
                : $"تم البيع — {saved.InvoiceNumber} — {AccountingCurrencyHelper.Format(saved.NetAmount, currency)}";
            if (applyLoyalty && saved.LoyaltyPointsEarned > 0)
                StatusMessage += $" — +{saved.LoyaltyPointsEarned} نقطة ولاء";
            _sound.Play(SoundEffect.Success);
            _ = RefreshLoyaltyQuoteAsync();
            _ = LoadRecentInvoicesAsync();
            _ = ReloadWarehouseStockAndProductsAsync();

            if (printReceipt)
            {
                try
                {
                    PrintReceiptForInvoice(
                        saved,
                        cartSnapshot,
                        totalSnapshot,
                        pharmacyUsage: ShowPharmacy,
                        silent: silentPrint);
                }
                catch (Exception printEx)
                {
                    BeautifulMessageDialog.ShowWarning($"تم البيع لكن فشلت الطباعة:\n{printEx.Message}");
                }
            }
            else
            {
                var msg = isCredit
                    ? $"تم حفظ الفاتورة الآجلة\nرقم: {saved.InvoiceNumber}\nالمدفوع: {paidSnapshot:N0} د.ع\nالمتبقي: {saved.RemainingAmount:N0} د.ع"
                    : $"تم حفظ الفاتورة\nرقم: {saved.InvoiceNumber}\nالمبلغ: {saved.NetAmount:N0} د.ع";
                BeautifulMessageDialog.ShowSuccess(msg);
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذّر إتمام البيع:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadWarehouseStockAsync()
    {
        _warehouseQtyByProduct = [];
        if (SelectedWarehouse is null) return;

        var stocks = await _unitOfWork.WarehouseStocks.FindAsync(
            s => s.WarehouseId == SelectedWarehouse.Id);
        _warehouseQtyByProduct = stocks
            .GroupBy(s => s.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.Quantity));
    }

    private async Task ReloadWarehouseStockAndProductsAsync()
    {
        await LoadWarehouseStockAsync();
        RefreshFilteredProducts();
    }

    private void RefreshFilteredProducts()
    {
        var term = SearchText.Trim();
        IEnumerable<Product> query = _allProducts.Where(p => p.ShowInCashier);

        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(p => ProductSearchHelper.Matches(p, term));
        else if (SelectedCategory is not null)
            query = query.Where(p => p.CategoryId == SelectedCategory.Id);

        FilteredProducts.Clear();
        foreach (var p in query.Take(80))
        {
            FilteredProducts.Add(new PosProductTile
            {
                Product = p,
                Price = ResolveSuggestedPrice(p.Id),
                AvailableQuantity = _warehouseQtyByProduct.GetValueOrDefault(p.Id)
            });
        }
    }

    private async Task LoadSuggestedPricesAsync()
    {
        _lastSalePrices = new Dictionary<int, decimal>();
        var saleItems = (await _unitOfWork.InvoiceItems.FindAsync(i => i.ProductId != null)).ToList();
        foreach (var product in _allProducts)
        {
            var lastSale = saleItems
                .Where(i => i.ProductId == product.Id && i.UnitPrice > 0)
                .OrderByDescending(i => i.Id)
                .FirstOrDefault();
            _lastSalePrices[product.Id] = lastSale?.UnitPrice ?? 0;
        }
    }

    private async Task LoadCatalogPricesAsync()
    {
        _defaultPricingTypeByProduct.Clear();
        _catalogListPrices.Clear();
        var prices = await _productPriceService.GetByProductIdsAsync(_allProducts.Select(p => p.Id));
        foreach (var group in prices.GroupBy(p => p.ProductId))
        {
            var preferred = group.FirstOrDefault(p => p.PricingType?.IsDefault == true) ?? group.First();
            _catalogListPrices[group.Key] = (preferred.SalePrice, preferred.SalePriceUsd);
            _defaultPricingTypeByProduct[group.Key] = preferred.PricingTypeId;
        }
    }

    private void OnCartChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (PosCartLine line in e.NewItems)
            {
                line.PropertyChanged += (_, _) =>
                {
                    RecalcCartTotals();
                    _ = RefreshLoyaltyQuoteAsync();
                    if (!line.IsOfferGift && !_isApplyingOffers)
                        _ = RefreshOfferGiftsAsync();
                };
            }
        }

        RecalcCartTotals();
        _ = RefreshLoyaltyQuoteAsync();
        if (!_isApplyingOffers)
            _ = RefreshOfferGiftsAsync();
    }

    private void RecalcCartTotals()
    {
        var currency = PosDocumentCurrency;
        SubTotal = AccountingCurrencyHelper.NormalizeAmount(CartLines.Sum(l => l.LineTotal), currency);
        if (ShowProductDiscount)
            InvoiceDiscountAmount = AccountingCurrencyHelper.NormalizeAmount(
                ProductDiscountHelper.CalculateInvoiceDiscount(
                    InvoiceDiscountType, InvoiceDiscountValue, SubTotal),
                currency);
        else
            InvoiceDiscountAmount = 0m;

        var loyaltyDiscount = ShowLoyaltyPanel ? Math.Max(0m, LoyaltyDiscountAmount) : 0m;
        GrandTotal = AccountingCurrencyHelper.NormalizeAmount(
            Math.Max(0m, SubTotal - InvoiceDiscountAmount - loyaltyDiscount), currency);
        CartLineCount = CartLines.Count;
        RecalcChange();
        RefreshIqdEquivalentOnly();
    }

    private void RecalcChange()
    {
        ChangeAmount = PaidAmount > GrandTotal ? PaidAmount - GrandTotal : 0;
        CreditRemainingAmount = PaidAmount < GrandTotal ? GrandTotal - PaidAmount : 0;
        ShowChangeDue = ChangeAmount > 0;
        ShowCreditRemaining = CreditRemainingAmount > 0;
        if (ShowCreditRemaining && CreditDueDate is null)
            CreditDueDate = DateTime.Today.AddMonths(1);
    }

    private List<InvoiceItemRow> BuildCartProfitRows() =>
        CartLines
            .Where(l => l.ProductId > 0 && !string.IsNullOrWhiteSpace(l.ProductName) && l.Quantity != 0)
            .Select(l => new InvoiceItemRow
            {
                ProductId = l.ProductId,
                ItemName = l.ProductName,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountAmount = ShowProductDiscount ? l.DiscountAmount : 0m,
                PricingTypeId = l.PricingTypeId,
                IsOfferGift = l.IsOfferGift
            })
            .ToList();

    [RelayCommand]
    private async Task CheckProfitAsync()
    {
        var rows = BuildCartProfitRows().Where(r => !r.IsOfferGift).ToList();
        if (rows.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("أضف مواد إلى السلة أولاً ثم افحص الربح.");
            return;
        }

        try
        {
            var vm = new InvoiceProfitCheckViewModel(
                _unitOfWork,
                _productPriceService,
                _pricingEnabled,
                ShowProductDiscount,
                PosDocumentCurrency);
            await vm.LoadAsync(rows, InvoiceDiscountType, InvoiceDiscountValue);

            var owner = Application.Current.MainWindow;
            var result = InvoiceProfitCheckDialog.Show(owner, vm);
            if (result == true && ShowProductDiscount)
            {
                InvoiceDiscountType = vm.DiscountType;
                InvoiceDiscountValue = vm.DiscountValue;
                SelectedInvoiceDiscountOption = InvoiceDiscountTypeOptions.FirstOrDefault(o => o.Type == vm.DiscountType)
                                               ?? InvoiceDiscountTypeOptions[0];
                RecalcCartTotals();
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر فحص ربح الفاتورة:\n{ex.Message}");
        }
    }

    private void PersistPosDefaults()
    {
        if (SelectedWarehouse is null && SelectedCashBox is null) return;
        _userPreferences.Update(p =>
        {
            if (SelectedWarehouse is not null)
                p.DefaultPosWarehouseId = SelectedWarehouse.Id;
            if (SelectedCashBox is not null)
                p.DefaultPosCashBoxId = SelectedCashBox.Id;
        });
    }

    private async Task<decimal> ResolveFxRateAsync(AccountingCurrency currency)
    {
        if (currency != AccountingCurrency.USD)
            return 1m;
        try
        {
            return await _exchangeRateService.GetUsdToIqdForDateOrLatestAsync(DateTime.Today);
        }
        catch
        {
            return 0m;
        }
    }

    public string FullscreenToggleToolTip =>
        IsFullscreenActive ? "العودة داخل النظام" : "فتح الكاشير بملء الشاشة";

    partial void OnIsFullscreenActiveChanged(bool value) =>
        OnPropertyChanged(nameof(FullscreenToggleToolTip));

    [RelayCommand]
    private void ToggleFullscreen()
    {
        if (_posFullscreen.IsOpen)
            _posFullscreen.Close();
        else
            _posFullscreen.Open(this);
    }

    internal void RequestExitFullscreen()
    {
        if (_posFullscreen.IsOpen)
            _posFullscreen.Close();
    }
}
