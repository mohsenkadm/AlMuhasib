using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCollectionViewModel : ViewModelBase
{
    private readonly IGoldSaleService _saleService;
    private readonly IGoldCashService _cashService;
    private readonly IGoldPricingService _pricingService;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;

    public ObservableCollection<GoldInvoiceListItem> OpenInvoices { get; } = [];
    public ObservableCollection<GoldCashBox> CashBoxes { get; } = [];

    public IReadOnlyList<GoldCurrencyOption> Currencies { get; } =
    [
        new(GoldCurrency.IQD, "دينار عراقي"),
        new(GoldCurrency.USD, "دولار أمريكي")
    ];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private GoldInvoiceListItem? _selectedInvoice;
    [ObservableProperty] private DateTime _paymentDate = DateTime.Today;
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private GoldCurrency _paymentCurrency = GoldCurrency.IQD;
    [ObservableProperty] private decimal _fxRate = 1m;
    [ObservableProperty] private GoldCashBox? _selectedCashBox;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public GoldCollectionViewModel(
        IGoldSaleService saleService,
        IGoldCashService cashService,
        IGoldPricingService pricingService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
    {
        _saleService = saleService;
        _cashService = cashService;
        _pricingService = pricingService;
        _toast = toast;
        _currentUserService = currentUserService;
        PageTitle = "تحصيل الآجل";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.Collection);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var (open, _) = await _saleService.GetPagedAsync(1, 200, SearchText, status: GoldInvoiceStatus.Open);
            var (partial, _) = await _saleService.GetPagedAsync(1, 200, SearchText, status: GoldInvoiceStatus.PartiallyPaid);
            OpenInvoices.Clear();
            foreach (var inv in open.Concat(partial).OrderByDescending(i => i.InvoiceDate))
                OpenInvoices.Add(inv);

            CashBoxes.Clear();
            foreach (var box in await _cashService.GetCashBoxesAsync())
                CashBoxes.Add(box);

            var fx = await _pricingService.GetLatestFxRateAsync();
            if (fx is not null && fx.UsdToIqd > 0)
                FxRate = fx.UsdToIqd;

            await SyncCashBoxAsync();
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

    partial void OnSelectedInvoiceChanged(GoldInvoiceListItem? value)
    {
        if (value is null) return;
        PaymentAmount = value.RemainingAmount;
        PaymentCurrency = value.PaymentCurrency;
        _ = SyncCashBoxAsync();
    }

    partial void OnPaymentCurrencyChanged(GoldCurrency value) => _ = SyncCashBoxAsync();

    private async Task SyncCashBoxAsync()
    {
        if (CashBoxes.Count == 0)
        {
            foreach (var box in await _cashService.GetCashBoxesAsync())
                CashBoxes.Add(box);
        }

        SelectedCashBox = CashBoxes.FirstOrDefault(b => b.Currency == PaymentCurrency && b.IsDefault)
            ?? CashBoxes.FirstOrDefault(b => b.Currency == PaymentCurrency)
            ?? CashBoxes.FirstOrDefault();
    }

    [RelayCommand]
    private async Task SearchAsync() => await LoadAsync();

    [RelayCommand]
    private async Task RecordPaymentAsync()
    {
        ErrorMessage = string.Empty;
        Message = string.Empty;

        if (!CanAdd && !CanEdit)
        {
            ErrorMessage = "ليس لديك صلاحية التحصيل";
            return;
        }

        if (SelectedInvoice is null)
        {
            ErrorMessage = "اختر فاتورة أولاً";
            _toast.ShowWarning(ErrorMessage);
            return;
        }

        if (PaymentAmount <= 0)
        {
            ErrorMessage = "أدخل مبلغ الدفعة";
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
            var request = new GoldPaymentRequest
            {
                InvoiceId = SelectedInvoice.Id,
                PaymentDate = PaymentDate.Date,
                Amount = PaymentAmount,
                Currency = PaymentCurrency,
                FxRate = FxRate,
                CashBoxId = SelectedCashBox?.Id,
                Notes = Notes
            };

            var invoice = await _saleService.RecordPaymentAsync(request);
            Message = $"تم تسجيل الدفعة على الفاتورة {invoice.InvoiceNumber} — المتبقي {invoice.RemainingAmount:N0}";
            _toast.ShowSuccess(Message);
            BeautifulMessageDialog.ShowSuccess(Message);
            Notes = string.Empty;
            SelectedInvoice = null;
            PaymentAmount = 0;
            await LoadAsync();
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
}
