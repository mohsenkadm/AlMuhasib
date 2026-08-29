using System.Collections.ObjectModel;
using System.Collections.Specialized;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldSaleReturnViewModel : ViewModelBase
{
    private readonly IGoldSaleService _saleService;
    private readonly IGoldPricingService _pricingService;
    private readonly IGoldCustomerService _customerService;
    private readonly IGoldWarehouseService _warehouseService;
    private readonly IGoldCashService _cashService;
    private readonly IGoldSettingsService _settingsService;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPartyQuickDetailService _partyQuickDetail;

    public ObservableCollection<GoldSaleLineDraft> Lines { get; } = [];
    public ObservableCollection<GoldCustomerListItem> Customers { get; } = [];
    public ObservableCollection<GoldWarehouse> Warehouses { get; } = [];
    public ObservableCollection<GoldCashBox> CashBoxes { get; } = [];
    public ObservableCollection<GoldKarat> Karats { get; } = [];
    public ObservableCollection<GoldInvoiceListItem> OriginalSales { get; } = [];

    public IReadOnlyList<GoldPaymentMethodOption> PaymentMethods { get; } =
    [
        new(GoldPaymentMethod.Cash, "نقدي (استرداد)"),
        new(GoldPaymentMethod.Credit, "آجل (تخفيض ذمة)")
    ];

    public IReadOnlyList<GoldCurrencyOption> Currencies { get; } =
    [
        new(GoldCurrency.IQD, "دينار عراقي"),
        new(GoldCurrency.USD, "دولار أمريكي")
    ];

    [ObservableProperty] private string _invoiceNumber = "—";
    [ObservableProperty] private DateTime _invoiceDate = DateTime.Today;
    [ObservableProperty] private GoldPaymentMethod _paymentMethod = GoldPaymentMethod.Cash;
    [ObservableProperty] private GoldCustomerListItem? _selectedCustomer;
    [ObservableProperty] private GoldWarehouse? _selectedWarehouse;
    [ObservableProperty] private GoldInvoiceListItem? _selectedOriginalSale;
    [ObservableProperty] private GoldCurrency _pricingCurrency = GoldCurrency.IQD;
    [ObservableProperty] private GoldCurrency _paymentCurrency = GoldCurrency.IQD;
    [ObservableProperty] private decimal _fxRate = 1m;
    [ObservableProperty] private GoldCashBox? _selectedCashBox;
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private decimal _paidAmount;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private GoldSaleLineDraft? _selectedLine;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private decimal _totalGoldValue;
    [ObservableProperty] private decimal _totalMakingCharge;
    [ObservableProperty] private decimal _grandTotal;

    public GoldSaleReturnViewModel(
        IGoldSaleService saleService,
        IGoldPricingService pricingService,
        IGoldCustomerService customerService,
        IGoldWarehouseService warehouseService,
        IGoldCashService cashService,
        IGoldSettingsService settingsService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService,
        IPartyQuickDetailService partyQuickDetail)
    {
        _saleService = saleService;
        _pricingService = pricingService;
        _customerService = customerService;
        _warehouseService = warehouseService;
        _cashService = cashService;
        _settingsService = settingsService;
        _toast = toast;
        _currentUserService = currentUserService;
        _partyQuickDetail = partyQuickDetail;
        PageTitle = "مرتجع بيع ذهب";
        Lines.CollectionChanged += OnLinesCollectionChanged;
        GoldFxRateRefreshHelper.Register(this, ApplyBroadcastFxRateAsync);
    }

    private Task ApplyBroadcastFxRateAsync(decimal rate)
    {
        FxRate = rate;
        RecalculateTotals();
        return Task.CompletedTask;
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.SaleReturn);
        await LoadLookupsAsync();
        AddLine();
    }

    public async Task PrepareFromSaleIdAsync(int saleId)
    {
        var sale = await _saleService.GetByIdAsync(saleId);
        if (sale is null)
        {
            _toast.ShowWarning("لم يتم العثور على فاتورة البيع");
            return;
        }

        if (OriginalSales.All(s => s.Id != saleId))
        {
            OriginalSales.Insert(0, new GoldInvoiceListItem
            {
                Id = sale.Id,
                InvoiceNumber = sale.InvoiceNumber,
                InvoiceDate = sale.InvoiceDate,
                CustomerName = sale.Customer?.Name,
                PricingCurrency = sale.PricingCurrency,
                PaymentCurrency = sale.PaymentCurrency,
                Status = sale.Status
            });
        }

        SelectedOriginalSale = OriginalSales.FirstOrDefault(s => s.Id == saleId);
    }

    public override bool HasUnsavedChanges => Lines.Any(l => l.WeightGrams > 0);

    private async Task LoadLookupsAsync()
    {
        IsBusy = true;
        try
        {
            InvoiceNumber = await _saleService.GetNextSaleReturnNumberAsync();

            Customers.Clear();
            var (customers, _) = await _customerService.GetPagedAsync(1, 500, activeOnly: true);
            foreach (var c in customers)
                Customers.Add(c);

            Warehouses.Clear();
            foreach (var w in await _warehouseService.GetAllAsync(activeOnly: true))
                Warehouses.Add(w);
            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.IsDefault) ?? Warehouses.FirstOrDefault();

            await _settingsService.EnsureDefaultsAsync();
            Karats.Clear();
            foreach (var k in await _pricingService.GetKaratsAsync())
                Karats.Add(k);

            OriginalSales.Clear();
            var (sales, _) = await _saleService.GetPagedAsync(1, 100);
            foreach (var s in sales.Where(x => x.Status != GoldInvoiceStatus.Cancelled))
                OriginalSales.Add(s);

            var fx = await _pricingService.GetLatestFxRateAsync();
            if (fx is not null && fx.UsdToIqd > 0)
                FxRate = fx.UsdToIqd;

            await ReloadCashBoxesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadCashBoxesAsync()
    {
        CashBoxes.Clear();
        foreach (var box in await _cashService.GetCashBoxesAsync(activeOnly: true))
            CashBoxes.Add(box);
        SelectedCashBox = CashBoxes.FirstOrDefault(c => c.Currency == PaymentCurrency && c.IsDefault)
            ?? CashBoxes.FirstOrDefault(c => c.Currency == PaymentCurrency)
            ?? CashBoxes.FirstOrDefault();
    }

    partial void OnPaymentCurrencyChanged(GoldCurrency value) => _ = ReloadCashBoxesAsync();

    partial void OnSelectedOriginalSaleChanged(GoldInvoiceListItem? value)
    {
        if (value is null) return;
        PricingCurrency = value.PricingCurrency;
        PaymentCurrency = value.PaymentCurrency;
        SelectedCustomer = Customers.FirstOrDefault(c => c.Name == value.CustomerName);
        Notes = string.IsNullOrWhiteSpace(Notes)
            ? $"مرتجع لفاتورة {value.InvoiceNumber}"
            : Notes;
    }

    private void OnLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RecalculateTotals();

    [RelayCommand]
    private void AddLine()
    {
        var defaultKarat = Karats.FirstOrDefault()?.KaratValue ?? 21;
        var line = new GoldSaleLineDraft(l => _ = QuoteLineAsync(l)) { KaratValue = defaultKarat };
        Lines.Add(line);
        SelectedLine = line;
    }

    [RelayCommand]
    private void RemoveRow(GoldSaleLineDraft? line)
    {
        line ??= SelectedLine;
        if (line is null || Lines.Count <= 1)
            return;
        Lines.Remove(line);
        SelectedLine = Lines.LastOrDefault();
        RecalculateTotals();
    }

    private async Task QuoteLineAsync(GoldSaleLineDraft line)
    {
        if (line.WeightGrams <= 0 || line.KaratValue <= 0)
        {
            line.GoldValue = 0;
            line.LineTotal = 0;
            RecalculateTotals();
            return;
        }

        line.IsQuoting = true;
        try
        {
            var quote = await _pricingService.QuoteAsync(
                line.KaratValue,
                line.WeightGrams,
                line.MakingCharge,
                PricingCurrency,
                line.MithqalPrice > 0 ? line.MithqalPrice : null,
                FxRate > 0 ? FxRate : null,
                line.MakingChargeMode,
                line.MakingChargeRate);
            line.ApplyQuote(quote);
            if (quote.FxRate is > 0)
                FxRate = quote.FxRate.Value;
            RecalculateTotals();
        }
        catch (Exception ex)
        {
            Message = ex.Message;
            _toast.ShowWarning(ex.Message);
        }
        finally
        {
            line.IsQuoting = false;
        }
    }

    private void RecalculateTotals()
    {
        TotalGoldValue = Lines.Sum(l => l.GoldValue);
        TotalMakingCharge = Lines.Sum(l => l.MakingCharge);
        GrandTotal = Math.Max(0, TotalGoldValue + TotalMakingCharge - DiscountAmount);
        if (PaymentMethod == GoldPaymentMethod.Cash && PaidAmount <= 0)
            PaidAmount = GrandTotal;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        Message = string.Empty;

        if (!CanAdd)
        {
            ErrorMessage = "ليس لديك صلاحية الحفظ";
            return;
        }

        if (PaymentMethod == GoldPaymentMethod.Credit && SelectedCustomer is null)
        {
            ErrorMessage = "مرتجع الآجل يتطلب اختيار زبون";
            _toast.ShowWarning(ErrorMessage);
            return;
        }

        var validLines = Lines.Where(l => l.WeightGrams > 0 && l.MithqalPrice > 0).ToList();
        if (validLines.Count == 0)
        {
            ErrorMessage = "أضف بنداً واحداً على الأقل بوزن وسعر مثقال";
            _toast.ShowWarning(ErrorMessage);
            return;
        }

        IsBusy = true;
        try
        {
            RecalculateTotals();
            var request = new GoldSaleReturnRequest
            {
                InvoiceDate = InvoiceDate.Date,
                PaymentMethod = PaymentMethod,
                CustomerId = SelectedCustomer?.Id,
                WarehouseId = SelectedWarehouse?.Id,
                RelatedInvoiceId = SelectedOriginalSale?.Id,
                PricingCurrency = PricingCurrency,
                PaymentCurrency = PaymentCurrency,
                FxRate = FxRate,
                DiscountAmount = DiscountAmount,
                PaidAmount = PaidAmount,
                CashBoxId = SelectedCashBox?.Id,
                Notes = Notes,
                Lines = validLines.Select(l => new GoldSaleLineRequest
                {
                    ItemId = l.ItemId,
                    KaratValue = l.KaratValue,
                    WeightGrams = l.WeightGrams,
                    MithqalPrice = l.MithqalPrice,
                    MakingCharge = l.MakingCharge,
                    MakingChargeMode = l.MakingChargeMode,
                    MakingChargeRate = l.MakingChargeRate,
                    Description = l.Description,
                    WeightFromScale = l.WeightFromScale
                }).ToList()
            };

            var invoice = await _saleService.CreateSaleReturnAsync(request);
            Message = $"تم حفظ مرتجع البيع {invoice.InvoiceNumber}";
            _toast.ShowSuccess(Message);
            BeautifulMessageDialog.ShowSuccess(Message);
            await ResetFormAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.ShowError(ex.Message);
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NewInvoiceAsync() => await ResetFormAsync();

    private async Task ResetFormAsync()
    {
        InvoiceDate = DateTime.Today;
        PaymentMethod = GoldPaymentMethod.Cash;
        SelectedCustomer = null;
        SelectedOriginalSale = null;
        SelectedWarehouse = Warehouses.FirstOrDefault(w => w.IsDefault) ?? Warehouses.FirstOrDefault();
        DiscountAmount = 0;
        PaidAmount = 0;
        Notes = string.Empty;
        Lines.Clear();
        AddLine();
        try
        {
            InvoiceNumber = await _saleService.GetNextSaleReturnNumberAsync();
            var fx = await _pricingService.GetLatestFxRateAsync();
            if (fx is not null && fx.UsdToIqd > 0)
                FxRate = fx.UsdToIqd;
            await ReloadCashBoxesAsync();
        }
        catch
        {
            // ignore
        }
        RecalculateTotals();
        ErrorMessage = string.Empty;
    }
}
