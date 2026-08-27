using System.Collections.ObjectModel;
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

public partial class GoldExchangeInvoiceViewModel : ViewModelBase
{
    private readonly IGoldExchangeService _exchangeService;
    private readonly IGoldPricingService _pricingService;
    private readonly IGoldCustomerService _customerService;
    private readonly IGoldCashService _cashService;
    private readonly IGoldWarehouseService _warehouseService;
    private readonly IGoldScaleService _scaleService;
    private readonly IGoldSettingsService _settingsService;
    private readonly IGoldPrintService _printService;
    private readonly IWhatsAppShareService _whatsAppShare;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPartyQuickDetailService _partyQuickDetail;
    private GoldInvoice? _lastSavedInvoice;
    private bool _allowManualWeightEdit = true;
    private bool _autoSyncPaidAmount = true;
    private bool _suppressPaidAmountChanged;

    public ObservableCollection<GoldSaleLineDraft> InLines { get; } = [];
    public ObservableCollection<GoldSaleLineDraft> OutLines { get; } = [];
    public ObservableCollection<GoldCustomerListItem> Customers { get; } = [];
    public ObservableCollection<GoldCashBox> CashBoxes { get; } = [];
    public ObservableCollection<GoldWarehouse> Warehouses { get; } = [];
    public ObservableCollection<GoldKarat> Karats { get; } = [];

    public IReadOnlyList<GoldPaymentMethodOption> PaymentMethods { get; } =
    [
        new(GoldPaymentMethod.Cash, "نقدي"),
        new(GoldPaymentMethod.Credit, "آجل")
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
    [ObservableProperty] private GoldCurrency _pricingCurrency = GoldCurrency.USD;
    [ObservableProperty] private GoldCurrency _paymentCurrency = GoldCurrency.IQD;
    [ObservableProperty] private decimal _fxRate = 1m;
    [ObservableProperty] private GoldCashBox? _selectedCashBox;
    [ObservableProperty] private decimal _exchangeCashDifference;
    [ObservableProperty] private decimal _paidAmount;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private bool _weightFromScale;
    [ObservableProperty] private bool _isScaleConnected;
    [ObservableProperty] private string _scaleStatusText = "غير متصل";
    [ObservableProperty] private GoldSaleLineDraft? _selectedInLine;
    [ObservableProperty] private GoldSaleLineDraft? _selectedOutLine;
    [ObservableProperty] private bool _isOutSectionActive;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private decimal _inTotalValue;
    [ObservableProperty] private decimal _outTotalValue;
    [ObservableProperty] private decimal _computedDifference;
    [ObservableProperty] private bool _canPrintInvoice;

    public GoldExchangeInvoiceViewModel(
        IGoldExchangeService exchangeService,
        IGoldPricingService pricingService,
        IGoldCustomerService customerService,
        IGoldCashService cashService,
        IGoldWarehouseService warehouseService,
        IGoldScaleService scaleService,
        IGoldSettingsService settingsService,
        IGoldPrintService printService,
        IWhatsAppShareService whatsAppShare,
        IToastNotificationService toast,
        ICurrentUserService currentUserService,
        IPartyQuickDetailService partyQuickDetail)
    {
        _exchangeService = exchangeService;
        _pricingService = pricingService;
        _customerService = customerService;
        _cashService = cashService;
        _warehouseService = warehouseService;
        _scaleService = scaleService;
        _settingsService = settingsService;
        _printService = printService;
        _whatsAppShare = whatsAppShare;
        _toast = toast;
        _currentUserService = currentUserService;
        _partyQuickDetail = partyQuickDetail;
        PageTitle = "تبديل ذهب";
        InLines.CollectionChanged += (_, _) => RecalculateTotals();
        OutLines.CollectionChanged += (_, _) => RecalculateTotals();
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
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.ExchangeInvoice);
        await LoadLookupsAsync();
        AddInLine();
        AddOutLine();
    }

    public override bool HasUnsavedChanges =>
        InLines.Any(l => l.WeightGrams > 0) || OutLines.Any(l => l.WeightGrams > 0);

    private async Task LoadLookupsAsync()
    {
        IsBusy = true;
        try
        {
            InvoiceNumber = await _exchangeService.GetNextInvoiceNumberAsync();

            Customers.Clear();
            var (customers, _) = await _customerService.GetPagedAsync(1, 500, activeOnly: true);
            foreach (var c in customers)
                Customers.Add(c);

            await _settingsService.EnsureDefaultsAsync();
            Karats.Clear();
            foreach (var k in await _pricingService.GetKaratsAsync())
                Karats.Add(k);

            Warehouses.Clear();
            foreach (var w in await _warehouseService.GetAllAsync(activeOnly: true))
                Warehouses.Add(w);
            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.IsDefault) ?? Warehouses.FirstOrDefault();

            var fx = await _pricingService.GetLatestFxRateAsync();
            if (fx is not null && fx.UsdToIqd > 0)
                FxRate = fx.UsdToIqd;

            await ReloadCashBoxesAsync();

            try
            {
                var settings = await _settingsService.GetSettingsAsync();
                _allowManualWeightEdit = settings.AllowManualWeightEdit;
            }
            catch
            {
                _allowManualWeightEdit = true;
            }

            RefreshScaleStatus();
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

    private void RefreshScaleStatus()
    {
        IsScaleConnected = _scaleService.IsConnected;
        ScaleStatusText = _scaleService.IsConnected ? "متصل" : "غير متصل";
    }

    private async Task ReloadCashBoxesAsync()
    {
        CashBoxes.Clear();
        foreach (var box in await _cashService.GetCashBoxesAsync())
            CashBoxes.Add(box);

        SelectedCashBox = CashBoxes.FirstOrDefault(b => b.Currency == PaymentCurrency && b.IsDefault)
            ?? CashBoxes.FirstOrDefault(b => b.Currency == PaymentCurrency)
            ?? CashBoxes.FirstOrDefault();
    }

    partial void OnPaymentCurrencyChanged(GoldCurrency value)
    {
        _autoSyncPaidAmount = true;
        _ = ReloadCashBoxesAsync();
        RecalculateTotals();
    }

    partial void OnPricingCurrencyChanged(GoldCurrency value)
    {
        _autoSyncPaidAmount = true;
        foreach (var line in InLines)
            _ = QuoteLineAsync(line);
        foreach (var line in OutLines)
            _ = QuoteLineAsync(line);
    }

    partial void OnPaymentMethodChanged(GoldPaymentMethod value)
    {
        _autoSyncPaidAmount = value == GoldPaymentMethod.Cash;
        RecalculateTotals();
    }

    partial void OnPaidAmountChanged(decimal value)
    {
        if (_suppressPaidAmountChanged)
            return;
        _autoSyncPaidAmount = false;
    }

    partial void OnFxRateChanged(decimal value)
    {
        _autoSyncPaidAmount = true;
        RecalculateTotals();
    }

    [RelayCommand]
    private void AddInLine()
    {
        var defaultKarat = Karats.FirstOrDefault()?.KaratValue ?? 21;
        var line = new GoldSaleLineDraft(l => _ = QuoteLineAsync(l))
        {
            KaratValue = defaultKarat,
            IsWeightReadOnly = !_allowManualWeightEdit
        };
        InLines.Add(line);
        SelectedInLine = line;
        IsOutSectionActive = false;
    }

    [RelayCommand]
    private void AddOutLine()
    {
        var defaultKarat = Karats.FirstOrDefault()?.KaratValue ?? 21;
        var line = new GoldSaleLineDraft(l => _ = QuoteLineAsync(l))
        {
            KaratValue = defaultKarat,
            IsWeightReadOnly = !_allowManualWeightEdit
        };
        OutLines.Add(line);
        SelectedOutLine = line;
        IsOutSectionActive = true;
    }

    [RelayCommand]
    private void RemoveInRow(GoldSaleLineDraft? line)
    {
        line ??= SelectedInLine;
        if (line is null || InLines.Count <= 1)
            return;
        InLines.Remove(line);
        SelectedInLine = InLines.LastOrDefault();
        RecalculateTotals();
    }

    [RelayCommand]
    private void RemoveOutRow(GoldSaleLineDraft? line)
    {
        line ??= SelectedOutLine;
        if (line is null || OutLines.Count <= 1)
            return;
        OutLines.Remove(line);
        SelectedOutLine = OutLines.LastOrDefault();
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
        InTotalValue = InLines.Sum(l => l.LineTotal);
        OutTotalValue = OutLines.Sum(l => l.LineTotal);
        ComputedDifference = OutTotalValue - InTotalValue;
        if (ExchangeCashDifference == 0 || Math.Abs(ExchangeCashDifference - ComputedDifference) < 0.01m)
            ExchangeCashDifference = ComputedDifference;

        if (PaymentMethod == GoldPaymentMethod.Cash && (_autoSyncPaidAmount || PaidAmount <= 0))
        {
            _suppressPaidAmountChanged = true;
            PaidAmount = Math.Abs(ExchangeCashDifference);
            _suppressPaidAmountChanged = false;
            _autoSyncPaidAmount = true;
        }
    }

    [RelayCommand]
    private async Task ReadScaleAsync()
    {
        var target = IsOutSectionActive
            ? (SelectedOutLine ?? OutLines.LastOrDefault())
            : (SelectedInLine ?? InLines.LastOrDefault());

        if (target is null)
        {
            Message = "أضف بنداً أولاً";
            return;
        }

        IsBusy = true;
        Message = string.Empty;
        try
        {
            var stable = await _scaleService.WaitForStableWeightAsync(timeout: TimeSpan.FromSeconds(8));
            if (!stable)
                throw new InvalidOperationException("لم يستقر الوزن على الميزان خلال المهلة المحددة");

            var grams = await _scaleService.ReadWeightGramsAsync();
            target.WeightGrams = grams;
            target.WeightFromScale = true;
            if (!_allowManualWeightEdit)
                target.IsWeightReadOnly = true;
            WeightFromScale = true;
            RefreshScaleStatus();
            Message = $"تم قراءة الوزن: {grams:N3} غرام";
            _toast.ShowSuccess(Message);
        }
        catch (Exception ex)
        {
            RefreshScaleStatus();
            Message = $"تعذر قراءة الميزان: {ex.Message}";
            ErrorMessage = Message;
            _toast.ShowError(Message);
            BeautifulMessageDialog.ShowError(Message, "الميزان");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshFxAsync()
    {
        try
        {
            var fx = await _pricingService.GetLatestFxRateAsync();
            if (fx is null || fx.UsdToIqd <= 0)
            {
                _toast.ShowWarning("لا يوجد سعر صرف مسجّل");
                return;
            }

            FxRate = fx.UsdToIqd;
            foreach (var line in InLines)
                await QuoteLineAsync(line);
            foreach (var line in OutLines)
                await QuoteLineAsync(line);
            _toast.ShowSuccess($"سعر الصرف: {FxRate:N0}");
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
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

        var validIn = InLines.Where(l => l.WeightGrams > 0 && l.MithqalPrice > 0).ToList();
        var validOut = OutLines.Where(l => l.WeightGrams > 0 && l.MithqalPrice > 0).ToList();
        if (validIn.Count == 0 && validOut.Count == 0)
        {
            ErrorMessage = "أضف بنداً وارداً أو صادراً بوزن وسعر مثقال";
            _toast.ShowWarning(ErrorMessage);
            return;
        }

        if (PaymentMethod == GoldPaymentMethod.Credit && SelectedCustomer is null && ExchangeCashDifference != 0)
        {
            ErrorMessage = "فرق المبادلة الآجل يتطلب اختيار زبون";
            _toast.ShowWarning(ErrorMessage);
            return;
        }

        if (FxRate <= 0)
        {
            ErrorMessage = "أدخل سعر صرف صحيح";
            return;
        }

        IsBusy = true;
        try
        {
            RecalculateTotals();
            var request = new GoldExchangeRequest
            {
                InvoiceDate = InvoiceDate.Date,
                PaymentMethod = PaymentMethod,
                CustomerId = SelectedCustomer?.Id,
                WarehouseId = SelectedWarehouse?.Id,
                PricingCurrency = PricingCurrency,
                PaymentCurrency = PaymentCurrency,
                FxRate = FxRate,
                ExchangeCashDifference = ExchangeCashDifference,
                PaidAmount = PaidAmount,
                CashBoxId = SelectedCashBox?.Id,
                Notes = Notes,
                WeightFromScale = WeightFromScale
                    || validIn.Any(l => l.WeightFromScale)
                    || validOut.Any(l => l.WeightFromScale),
                InLines = validIn.Select(ToLineRequest).ToList(),
                OutLines = validOut.Select(ToLineRequest).ToList()
            };

            var invoice = await _exchangeService.CreateExchangeAsync(request);
            _lastSavedInvoice = invoice;
            CanPrintInvoice = CanPrint;
            Message = $"تم حفظ فاتورة التبديل {invoice.InvoiceNumber}";
            _toast.ShowSuccess(Message);

            if (BeautifulMessageDialog.ShowConfirm(
                    $"{Message}\n\nهل تريد طباعة الفاتورة؟",
                    "طباعة"))
            {
                await PrintInvoiceAsync();
            }

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

    [RelayCommand(CanExecute = nameof(CanPrintInvoice))]
    private async Task PrintInvoiceAsync()
    {
        if (_lastSavedInvoice is null || !CanPrint)
            return;

        try
        {
            await _printService.PrintInvoiceAsync(_lastSavedInvoice);
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
            BeautifulMessageDialog.ShowError(ex.Message, "الطباعة");
        }
    }

    [RelayCommand(CanExecute = nameof(CanPrintInvoice))]
    private void SendInvoiceWhatsApp()
    {
        if (_lastSavedInvoice is null || !CanPrint)
            return;

        try
        {
            var model = _printService.BuildInvoicePrintModel(_lastSavedInvoice);
            _whatsAppShare.ShareInvoice(
                model,
                _lastSavedInvoice.Customer?.Phone ?? SelectedCustomer?.Phone,
                _lastSavedInvoice.Customer?.Name ?? SelectedCustomer?.Name ?? "زبون");
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
            BeautifulMessageDialog.ShowError(ex.Message, "واتساب");
        }
    }

    partial void OnCanPrintInvoiceChanged(bool value)
    {
        PrintInvoiceCommand.NotifyCanExecuteChanged();
        SendInvoiceWhatsAppCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task NewInvoiceAsync() => await ResetFormAsync();

    private static GoldSaleLineRequest ToLineRequest(GoldSaleLineDraft l) => new()
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
    };

    private async Task ResetFormAsync()
    {
        InvoiceDate = DateTime.Today;
        PaymentMethod = GoldPaymentMethod.Cash;
        SelectedCustomer = null;
        ExchangeCashDifference = 0;
        _autoSyncPaidAmount = true;
        PaidAmount = 0;
        Notes = string.Empty;
        WeightFromScale = false;
        InLines.Clear();
        OutLines.Clear();
        AddInLine();
        AddOutLine();
        try
        {
            InvoiceNumber = await _exchangeService.GetNextInvoiceNumberAsync();
            var fx = await _pricingService.GetLatestFxRateAsync();
            if (fx is not null && fx.UsdToIqd > 0)
                FxRate = fx.UsdToIqd;
            await ReloadCashBoxesAsync();
        }
        catch
        {
            // ignore refresh errors on reset
        }
        RecalculateTotals();
        ErrorMessage = string.Empty;
    }
}
