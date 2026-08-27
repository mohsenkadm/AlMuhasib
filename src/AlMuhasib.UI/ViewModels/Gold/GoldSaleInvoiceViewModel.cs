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

public partial class GoldSaleInvoiceViewModel : ViewModelBase
{
    private readonly IGoldSaleService _saleService;
    private readonly IGoldInventoryService _inventoryService;
    private readonly IGoldPricingService _pricingService;
    private readonly IGoldCustomerService _customerService;
    private readonly IGoldWarehouseService _warehouseService;
    private readonly IGoldCashService _cashService;
    private readonly IGoldScaleService _scaleService;
    private readonly IGoldSettingsService _settingsService;
    private readonly IGoldPrintService _printService;
    private readonly IWhatsAppShareService _whatsAppShare;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPartyQuickDetailService _partyQuickDetail;
    private int _quoteVersion;
    private GoldInvoice? _lastSavedInvoice;
    private bool _allowManualWeightEdit = true;
    private bool _autoSyncPaidAmount = true;
    private bool _suppressPaidAmountChanged;

    public ObservableCollection<GoldSaleLineDraft> Lines { get; } = [];
    public ObservableCollection<GoldCustomerListItem> Customers { get; } = [];
    public ObservableCollection<GoldWarehouse> Warehouses { get; } = [];
    public ObservableCollection<GoldCashBox> CashBoxes { get; } = [];
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
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private decimal _paidAmount;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private bool _weightFromScale;
    [ObservableProperty] private bool _isScaleConnected;
    [ObservableProperty] private string _scaleStatusText = "غير متصل";
    [ObservableProperty] private GoldSaleLineDraft? _selectedLine;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _canPrintInvoice;
    [ObservableProperty] private string _barcodeInput = string.Empty;
    [ObservableProperty] private bool _isItemPickerOpen;

    public GoldItemPickerViewModel ItemPicker { get; }

    [ObservableProperty] private decimal _totalGoldValue;
    [ObservableProperty] private decimal _totalMakingCharge;
    [ObservableProperty] private decimal _grandTotal;
    [ObservableProperty] private decimal _totalIqd;
    [ObservableProperty] private decimal _totalUsd;

    public GoldSaleInvoiceViewModel(
        IGoldSaleService saleService,
        IGoldInventoryService inventoryService,
        IGoldPricingService pricingService,
        IGoldCustomerService customerService,
        IGoldWarehouseService warehouseService,
        IGoldCashService cashService,
        IGoldScaleService scaleService,
        IGoldSettingsService settingsService,
        IGoldPrintService printService,
        IWhatsAppShareService whatsAppShare,
        IToastNotificationService toast,
        ICurrentUserService currentUserService,
        IPartyQuickDetailService partyQuickDetail)
    {
        _saleService = saleService;
        _inventoryService = inventoryService;
        _pricingService = pricingService;
        _customerService = customerService;
        _warehouseService = warehouseService;
        _cashService = cashService;
        _scaleService = scaleService;
        _settingsService = settingsService;
        _printService = printService;
        _whatsAppShare = whatsAppShare;
        _toast = toast;
        _currentUserService = currentUserService;
        _partyQuickDetail = partyQuickDetail;
        PageTitle = "فاتورة بيع ذهب";
        ItemPicker = new GoldItemPickerViewModel(inventoryService);
        ItemPicker.Confirmed += OnItemPickerConfirmed;
        ItemPicker.Cancelled += () => IsItemPickerOpen = false;
        Lines.CollectionChanged += OnLinesCollectionChanged;
        GoldFxRateRefreshHelper.Register(this, ApplyBroadcastFxRateAsync);
    }

    private async Task ApplyBroadcastFxRateAsync(decimal rate)
    {
        FxRate = rate;
        foreach (var line in Lines)
            await QuoteLineAsync(line);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.SaleInvoice);
        await LoadLookupsAsync();
        AddLine();
    }

    public override bool HasUnsavedChanges => Lines.Any(l => l.WeightGrams > 0);

    private async Task LoadLookupsAsync()
    {
        IsBusy = true;
        try
        {
            InvoiceNumber = await _saleService.GetNextInvoiceNumberAsync();

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
        foreach (var line in Lines)
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

        // نقدي + مدفوع صفر ≠ آجل: أعد ملء المدفوع بالإجمالي تلقائياً
        if (PaymentMethod == GoldPaymentMethod.Cash && value <= 0)
        {
            _autoSyncPaidAmount = true;
            SyncPaidAmountFromTotals();
            return;
        }

        _autoSyncPaidAmount = false;
    }

    partial void OnDiscountAmountChanged(decimal value) => RecalculateTotals();
    partial void OnFxRateChanged(decimal value)
    {
        _autoSyncPaidAmount = true;
        RecalculateTotals();
    }

    private void OnLinesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RecalculateTotals();

    [RelayCommand]
    private void AddLine()
    {
        var defaultKarat = Karats.FirstOrDefault()?.KaratValue ?? 21;
        var line = CreateLineDraft(defaultKarat);
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

    [RelayCommand]
    private async Task QuoteSelectedLineAsync()
    {
        if (SelectedLine is not null)
            await QuoteLineAsync(SelectedLine);
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

        var version = ++_quoteVersion;
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

            if (version != _quoteVersion && line.IsQuoting)
            {
                // Still apply latest for this line if weights match intent
            }

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

    private GoldSaleLineDraft CreateLineDraft(int defaultKarat) =>
        new(
            l => _ = QuoteLineAsync(l),
            l => _ = ApplyItemLookupAsync(l))
        {
            KaratValue = defaultKarat,
            IsWeightReadOnly = !_allowManualWeightEdit
        };

    private async Task ApplyItemLookupAsync(GoldSaleLineDraft line)
    {
        var lookup = line.ItemLookup?.Trim();
        if (string.IsNullOrEmpty(lookup))
        {
            line.ItemId = null;
            line.IsWeightReadOnly = !_allowManualWeightEdit;
            return;
        }

        try
        {
            GoldItem? item = null;
            if (int.TryParse(lookup, out var id))
                item = await _inventoryService.GetItemByIdAsync(id);
            item ??= await _inventoryService.GetItemByBarcodeAsync(lookup);

            if (item is null)
            {
                ErrorMessage = $"القطعة «{lookup}» غير موجودة في أصناف الذهب";
                line.ItemId = null;
                return;
            }

            if (item.Status != GoldItemStatus.InStock)
            {
                ErrorMessage = $"القطعة «{item.Name}» غير متاحة للبيع (الحالة: {GoldItemStatusDisplay.ToArabic(item.Status)})";
                line.ItemId = null;
                return;
            }

            await ApplyItemToLineAsync(line, item);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.ShowWarning(ex.Message);
        }
    }

    private async Task ApplyItemToLineAsync(GoldSaleLineDraft line, GoldItem item)
    {
        line.ItemId = item.Id;
        line.KaratValue = item.KaratValue;
        line.WeightGrams = item.WeightGrams;
        line.MakingCharge = item.SuggestedMakingCharge;
        line.Description = item.Name;
        line.IsWeightReadOnly = item.TrackAsPiece || !_allowManualWeightEdit;
        line.SetItemLookupSilently(
            string.IsNullOrWhiteSpace(item.Barcode) ? item.Id.ToString() : item.Barcode);
        ErrorMessage = string.Empty;
        Message = $"تم تحميل القطعة: {item.Name} — عيار {item.KaratValue}، {item.WeightGrams:N3} غ";
        await QuoteLineAsync(line);
    }

    [RelayCommand]
    private async Task ProcessBarcodeAsync()
    {
        var code = BarcodeInput?.Trim();
        if (string.IsNullOrEmpty(code))
            return;

        try
        {
            GoldItem? item = null;
            if (int.TryParse(code, out var id))
                item = await _inventoryService.GetItemByIdAsync(id);
            item ??= await _inventoryService.GetItemByBarcodeAsync(code);

            if (item is null)
            {
                ErrorMessage = $"القطعة «{code}» غير موجودة";
                _toast.ShowWarning(ErrorMessage);
                return;
            }

            if (item.Status != GoldItemStatus.InStock)
            {
                ErrorMessage = $"القطعة «{item.Name}» غير متاحة ({GoldItemStatusDisplay.ToArabic(item.Status)})";
                _toast.ShowWarning(ErrorMessage);
                return;
            }

            if (Lines.Any(l => l.ItemId == item.Id))
            {
                ErrorMessage = $"القطعة «{item.Name}» مضافة مسبقاً في الفاتورة";
                _toast.ShowWarning(ErrorMessage);
                BarcodeInput = string.Empty;
                return;
            }

            var emptyLine = Lines.FirstOrDefault(l => l.ItemId is null && l.WeightGrams <= 0);
            var line = emptyLine ?? CreateLineDraft(item.KaratValue);
            if (emptyLine is null)
                Lines.Add(line);

            SelectedLine = line;
            await ApplyItemToLineAsync(line, item);
            BarcodeInput = string.Empty;
            _toast.ShowSuccess(Message);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task OpenItemPickerAsync()
    {
        await ItemPicker.InitializeAsync();
        IsItemPickerOpen = true;
    }

    private async void OnItemPickerConfirmed(IReadOnlyList<GoldItem> items)
    {
        IsItemPickerOpen = false;
        foreach (var item in items)
        {
            if (Lines.Any(l => l.ItemId == item.Id))
                continue;

            var emptyLine = Lines.FirstOrDefault(l => l.ItemId is null && l.WeightGrams <= 0);
            var line = emptyLine ?? CreateLineDraft(item.KaratValue);
            if (emptyLine is null)
                Lines.Add(line);

            SelectedLine = line;
            await ApplyItemToLineAsync(line, item);
        }

        if (items.Count > 0)
            _toast.ShowSuccess($"تمت إضافة {items.Count} قطعة");
    }

    private void RecalculateTotals()
    {
        TotalGoldValue = Lines.Sum(l => l.GoldValue);
        TotalMakingCharge = Lines.Sum(l => l.MakingCharge);
        GrandTotal = Math.Max(0, TotalGoldValue + TotalMakingCharge - DiscountAmount);

        if (FxRate > 0)
        {
            if (PricingCurrency == GoldCurrency.USD)
            {
                TotalUsd = GrandTotal;
                TotalIqd = Math.Round(GrandTotal * FxRate, 0, MidpointRounding.AwayFromZero);
            }
            else
            {
                TotalIqd = GrandTotal;
                TotalUsd = Math.Round(GrandTotal / FxRate, 2, MidpointRounding.AwayFromZero);
            }
        }
        else
        {
            TotalIqd = PricingCurrency == GoldCurrency.IQD ? GrandTotal : 0;
            TotalUsd = PricingCurrency == GoldCurrency.USD ? GrandTotal : 0;
        }

        SyncPaidAmountFromTotals();
    }

    /// <summary>
    /// PaidAmount is always in PaymentCurrency. Keep it in sync when currencies/totals change
    /// unless the user typed a custom amount.
    /// </summary>
    private void SyncPaidAmountFromTotals()
    {
        if (PaymentMethod != GoldPaymentMethod.Cash)
            return;

        if (!_autoSyncPaidAmount)
            return;

        var expected = PaymentCurrency == PricingCurrency
            ? GrandTotal
            : PaymentCurrency == GoldCurrency.IQD ? TotalIqd : TotalUsd;

        _suppressPaidAmountChanged = true;
        PaidAmount = expected;
        _suppressPaidAmountChanged = false;
    }

    [RelayCommand]
    private async Task ReadScaleAsync()
    {
        var target = SelectedLine ?? Lines.LastOrDefault();
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
            foreach (var line in Lines)
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

        if (PaymentMethod == GoldPaymentMethod.Credit && SelectedCustomer is null)
        {
            ErrorMessage = "البيع الآجل يتطلب اختيار زبون";
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

        if (FxRate <= 0)
        {
            ErrorMessage = "أدخل سعر صرف صحيح";
            return;
        }

        IsBusy = true;
        try
        {
            RecalculateTotals();
            var request = new GoldSaleRequest
            {
                InvoiceDate = InvoiceDate.Date,
                PaymentMethod = PaymentMethod,
                CustomerId = SelectedCustomer?.Id,
                WarehouseId = SelectedWarehouse?.Id,
                PricingCurrency = PricingCurrency,
                PaymentCurrency = PaymentCurrency,
                FxRate = FxRate,
                DiscountAmount = DiscountAmount,
                PaidAmount = PaymentMethod == GoldPaymentMethod.Cash ? PaidAmount : PaidAmount,
                CashBoxId = SelectedCashBox?.Id,
                Notes = Notes,
                WeightFromScale = WeightFromScale || validLines.Any(l => l.WeightFromScale),
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

            var invoice = await _saleService.CreateSaleAsync(request);
            _lastSavedInvoice = invoice;
            CanPrintInvoice = CanPrint;
            Message = $"تم حفظ فاتورة البيع {invoice.InvoiceNumber}";
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

    private async Task ResetFormAsync()
    {
        InvoiceDate = DateTime.Today;
        PaymentMethod = GoldPaymentMethod.Cash;
        SelectedCustomer = null;
        SelectedWarehouse = Warehouses.FirstOrDefault(w => w.IsDefault) ?? Warehouses.FirstOrDefault();
        DiscountAmount = 0;
        _autoSyncPaidAmount = true;
        PaidAmount = 0;
        Notes = string.Empty;
        WeightFromScale = false;
        Lines.Clear();
        AddLine();
        try
        {
            InvoiceNumber = await _saleService.GetNextInvoiceNumberAsync();
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
