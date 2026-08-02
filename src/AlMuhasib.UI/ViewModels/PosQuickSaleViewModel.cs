using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
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
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPreferencesService _userPreferences;
    private readonly IRecentActivityService _recentActivity;
    private readonly IFavoriteProductsService _favoriteProducts;
    private readonly ISoundService _sound;
    private readonly IProductPriceService _productPriceService;
    private readonly IProductBatchService _productBatchService;
    private readonly IFeatureFlagService _featureFlags;
    private readonly DispatcherTimer _searchDebounce;

    private List<Product> _allProducts = [];
    private Dictionary<int, decimal> _suggestedPrices = [];
    private Dictionary<int, int> _defaultPricingTypeByProduct = [];
    private readonly bool _pricingEnabled;

    public ObservableCollection<PosProductTile> FilteredProducts { get; } = [];
    public ObservableCollection<PosProductTile> FavoriteProducts { get; } = [];
    public ObservableCollection<PosCartLine> CartLines { get; } = [];
    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private Warehouse? _selectedWarehouse;
    [ObservableProperty] private CashBox? _selectedCashBox;
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
    [ObservableProperty] private int _cartLineCount;
    [ObservableProperty] private bool _showProductDiscount;
    [ObservableProperty] private string _statusMessage = "امسح الباركود أو ابحث بالاسم ثم Enter للإضافة";
    [ObservableProperty] private string? _lastSavedInvoiceNumber;
    [ObservableProperty] private bool _printAfterSale = true;

    public IReadOnlyList<DiscountTypeOption> InvoiceDiscountTypeOptions { get; } =
    [
        new(DiscountType.None, "بدون خصم كلي"),
        new(DiscountType.Percentage, "نسبة مئوية (%)"),
        new(DiscountType.FixedAmount, "قيمة ثابتة (د.ع)")
    ];

    [ObservableProperty] private DiscountTypeOption? _selectedInvoiceDiscountOption;

    private bool _printAfterConfirm;

    public PosQuickSaleViewModel(
        IUnitOfWork unitOfWork,
        IInvoiceService invoiceService,
        ICurrentUserService currentUserService,
        IUserPreferencesService userPreferences,
        IRecentActivityService recentActivity,
        IFavoriteProductsService favoriteProducts,
        ISoundService sound,
        IProductPriceService productPriceService,
        IProductBatchService productBatchService,
        IFeatureFlagService featureFlags,
        IProductSerialService productSerialService,
        IProductSizeService productSizeService,
        IProductColorService productColorService)
    {
        _unitOfWork = unitOfWork;
        _invoiceService = invoiceService;
        _sound = sound;
        _currentUserService = currentUserService;
        _userPreferences = userPreferences;
        _recentActivity = recentActivity;
        _favoriteProducts = favoriteProducts;
        _productPriceService = productPriceService;
        _productBatchService = productBatchService;
        _featureFlags = featureFlags;
        _pricingEnabled = userPreferences.Current.FeatureFlags.ProductPricingEnabled;
        PageTitle = "بيع سريع (POS)";
        SelectedInvoiceDiscountOption = InvoiceDiscountTypeOptions[0];
        ConfigurePosFeatureServices(productSerialService, productSizeService, productColorService);

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

    partial void OnInvoiceDiscountTypeChanged(DiscountType value) => RecalcCartTotals();
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
            var products = (await _unitOfWork.Products.GetAllAsync()).OrderBy(p => p.Name).ToList();
            _allProducts = products;

            await LoadSuggestedPricesAsync();
            if (_pricingEnabled)
                await LoadCatalogPricesAsync();

            Warehouses.Clear();
            foreach (var w in await _unitOfWork.Warehouses.GetAllAsync())
                Warehouses.Add(w);

            CashBoxes.Clear();
            foreach (var c in await _unitOfWork.CashBoxes.GetAllAsync())
                CashBoxes.Add(c);

            var prefs = _userPreferences.Current;
            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == prefs.DefaultPosWarehouseId)
                                ?? Warehouses.FirstOrDefault();
            SelectedCashBox = CashBoxes.FirstOrDefault(c => c.Id == prefs.DefaultPosCashBoxId)
                              ?? CashBoxes.FirstOrDefault();

            await LoadPosCustomersAsync();
            await LoadHeldInvoicesAsync();

            RefreshFilteredProducts();
            RefreshFavoriteProducts();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ToggleFavorite(PosProductTile? tile)
    {
        if (tile?.Product is null) return;
        _favoriteProducts.ToggleFavorite(tile.Product.Id);
        RefreshFavoriteProducts();
        RefreshFilteredProducts();
        StatusMessage = _favoriteProducts.IsFavorite(tile.Product.Id)
            ? $"أُضيف «{tile.Product.Name}» للمفضلة"
            : $"أُزيل «{tile.Product.Name}» من المفضلة";
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    partial void OnSelectedWarehouseChanged(Warehouse? value) => PersistPosDefaults();
    partial void OnSelectedCashBoxChanged(CashBox? value) => PersistPosDefaults();
    partial void OnPaidAmountChanged(decimal value) => RecalcChange();

    [RelayCommand]
    private async Task AddProductFromSearch()
    {
        var term = SearchText.Trim();
        if (string.IsNullOrEmpty(term))
            return;

        var barcodeMatch = _allProducts.FirstOrDefault(p =>
            !string.IsNullOrEmpty(p.Barcode) &&
            p.Barcode.Equals(term, StringComparison.OrdinalIgnoreCase));

        if (barcodeMatch is not null)
        {
            _sound.Play(SoundEffect.Scan);
            await AddOrIncrementProductAsync(barcodeMatch);
            SearchText = string.Empty;
            return;
        }

        var first = FilteredProducts.FirstOrDefault();
        if (first is not null)
        {
            await AddOrIncrementProductAsync(first.Product);
            SearchText = string.Empty;
        }
        else
            StatusMessage = "لم يُعثر على منتج";
    }

    [RelayCommand]
    private async Task AddProduct(PosProductTile? tile)
    {
        if (tile?.Product is null) return;
        await AddOrIncrementProductAsync(tile.Product);
    }

    [RelayCommand]
    private void RemoveLine(PosCartLine? line)
    {
        if (line is null) return;
        CartLines.Remove(line);
    }

    [RelayCommand]
    private void IncreaseLine(PosCartLine? line)
    {
        if (line is null) return;
        line.Quantity += 1;
    }

    [RelayCommand]
    private void DecreaseLine(PosCartLine? line)
    {
        if (line is null) return;
        if (line.Quantity <= 1)
            CartLines.Remove(line);
        else
            line.Quantity -= 1;
    }

    [RelayCommand]
    private void ClearCart()
    {
        if (CartLines.Count == 0) return;
        if (!BeautifulMessageDialog.ShowConfirm("مسح كل بنود السلة؟")) return;
        CartLines.Clear();
        PaidAmount = 0;
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

    [RelayCommand]
    private void OpenPaymentDialogAndPrint()
    {
        if (IsInstallmentMode)
        {
            _ = CompleteInstallmentSaleCoreAsync();
            return;
        }

        if (!CanOpenPaymentDialog()) return;
        _printAfterConfirm = true;
        PaidAmount = GrandTotal;
        RecalcChange();
        IsPaymentDialogOpen = true;
    }

    [RelayCommand]
    private void ClosePaymentDialog()
    {
        IsPaymentDialogOpen = false;
    }

    [RelayCommand]
    private void OpenCurrencyChange()
    {
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
        await CompleteSaleCoreAsync(printReceipt: _printAfterConfirm);
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

        var invalid = CartLines.Where(l => l.UnitPrice <= 0).ToList();
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
        RecalcChange();
        IsPaymentDialogOpen = true;
    }

    private async Task CompleteSaleCoreAsync(bool printReceipt)
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

        var invalid = CartLines.Where(l => l.UnitPrice <= 0).ToList();
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

        IsBusy = true;
        try
        {
            var cartSnapshot = CartLines.ToList();
            var totalSnapshot = GrandTotal;
            var paidSnapshot = Math.Min(PaidAmount, GrandTotal);

            var invoice = new Invoice
            {
                InvoiceType = InvoiceType.Sale,
                CustomerId = customerId,
                WarehouseId = SelectedWarehouse.Id,
                PaymentMethod = isCredit ? PaymentMethod.Credit : PaymentMethod.Cash,
                CashBoxId = SelectedCashBox.Id,
                Date = DateTime.Now,
                DiscountAmount = ShowProductDiscount ? InvoiceDiscountAmount : 0m,
                PaidAmount = isCredit ? paidSnapshot : GrandTotal,
                CreditDueDate = isCredit ? DateTime.Today.AddMonths(1) : null,
                Notes = isCredit ? "بيع سريع POS — آجل" : "بيع سريع POS"
            };

            var items = cartSnapshot.Select(line => new InvoiceItem
            {
                ProductId = line.ProductId,
                PricingTypeId = line.PricingTypeId,
                ItemName = line.ProductName,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                DiscountAmount = ShowProductDiscount ? line.DiscountAmount : 0m,
                TotalPrice = line.LineTotal,
                CustomFieldsJson = line.ToCustomFieldsJson()
            }).ToList();

            var saved = await _invoiceService.CreateInvoiceAsync(invoice, items);
            LastSavedInvoiceNumber = saved.InvoiceNumber;

            await ApplyPosFeatureSideEffectsOnSaveAsync(cartSnapshot, items);

            _recentActivity.Record(
                "بيع سريع",
                $"{saved.InvoiceNumber} — {saved.NetAmount:N0} د.ع",
                "SaleInvoice",
                typeof(PosQuickSaleViewModel));

            CartLines.Clear();
            PaidAmount = 0;
            StatusMessage = isCredit
                ? $"تم البيع الآجل — {saved.InvoiceNumber} — مدفوع {paidSnapshot:N0} — متبقي {saved.RemainingAmount:N0} د.ع"
                : $"تم البيع — {saved.InvoiceNumber} — {saved.NetAmount:N0} د.ع";
            _sound.Play(SoundEffect.Success);

            if (printReceipt)
            {
                try
                {
                    PrintReceiptForInvoice(saved, cartSnapshot, totalSnapshot, pharmacyUsage: ShowPharmacy);
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

    private void RefreshFilteredProducts()
    {
        var term = SearchText.Trim();
        IEnumerable<Product> query = _allProducts;

        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(p => ProductSearchHelper.Matches(p, term));

        FilteredProducts.Clear();
        foreach (var p in query.Take(48))
        {
            FilteredProducts.Add(new PosProductTile
            {
                Product = p,
                Price = _suggestedPrices.GetValueOrDefault(p.Id),
                IsFavorite = _favoriteProducts.IsFavorite(p.Id)
            });
        }
    }

    private void RefreshFavoriteProducts()
    {
        FavoriteProducts.Clear();
        foreach (var id in _favoriteProducts.GetFavoriteProductIds())
        {
            var product = _allProducts.FirstOrDefault(p => p.Id == id);
            if (product is not null)
            {
                FavoriteProducts.Add(new PosProductTile
                {
                    Product = product,
                    Price = _suggestedPrices.GetValueOrDefault(product.Id),
                    IsFavorite = true
                });
            }
        }
    }

    private async Task LoadSuggestedPricesAsync()
    {
        _suggestedPrices = new Dictionary<int, decimal>();
        var saleItems = (await _unitOfWork.InvoiceItems.FindAsync(i => i.ProductId != null)).ToList();
        foreach (var product in _allProducts)
        {
            var lastSale = saleItems
                .Where(i => i.ProductId == product.Id && i.UnitPrice > 0)
                .OrderByDescending(i => i.Id)
                .FirstOrDefault();
            _suggestedPrices[product.Id] = lastSale?.UnitPrice ?? 0;
        }
    }

    private async Task LoadCatalogPricesAsync()
    {
        _defaultPricingTypeByProduct.Clear();
        var prices = await _productPriceService.GetByProductIdsAsync(_allProducts.Select(p => p.Id));
        foreach (var group in prices.GroupBy(p => p.ProductId))
        {
            var preferred = group.FirstOrDefault(p => p.PricingType?.IsDefault == true) ?? group.First();
            _suggestedPrices[group.Key] = preferred.SalePrice;
            _defaultPricingTypeByProduct[group.Key] = preferred.PricingTypeId;
        }
    }

    private void OnCartChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (PosCartLine line in e.NewItems)
                line.PropertyChanged += (_, _) => RecalcCartTotals();
        }

        RecalcCartTotals();
    }

    private void RecalcCartTotals()
    {
        SubTotal = CartLines.Sum(l => l.LineTotal);
        if (ShowProductDiscount)
            InvoiceDiscountAmount = ProductDiscountHelper.CalculateInvoiceDiscount(
                InvoiceDiscountType, InvoiceDiscountValue, SubTotal);
        else
            InvoiceDiscountAmount = 0m;

        GrandTotal = Math.Max(0m, SubTotal - InvoiceDiscountAmount);
        CartLineCount = CartLines.Count;
        RecalcChange();
    }

    private void RecalcChange()
    {
        ChangeAmount = PaidAmount > GrandTotal ? PaidAmount - GrandTotal : 0;
        CreditRemainingAmount = PaidAmount < GrandTotal ? GrandTotal - PaidAmount : 0;
        ShowChangeDue = ChangeAmount > 0;
        ShowCreditRemaining = CreditRemainingAmount > 0;
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
}
