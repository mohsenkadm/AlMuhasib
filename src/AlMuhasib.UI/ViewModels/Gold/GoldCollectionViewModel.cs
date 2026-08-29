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
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCollectionViewModel : ViewModelBase
{
    private readonly IGoldSaleService _saleService;
    private readonly IGoldCashService _cashService;
    private readonly IGoldPricingService _pricingService;
    private readonly IExportService _exportService;
    private readonly IToastNotificationService _toast;
    private readonly ICurrentUserService _currentUserService;
    private List<GoldInvoiceListItem> _allInvoices = [];

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
        IExportService exportService,
        IToastNotificationService toast,
        ICurrentUserService currentUserService)
    {
        _saleService = saleService;
        _cashService = cashService;
        _pricingService = pricingService;
        _exportService = exportService;
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
            _allInvoices = open.Concat(partial).OrderByDescending(i => i.InvoiceDate).ToList();
            ApplyFilters();

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

    private void ApplyFilters()
    {
        var filtered = MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters)
            ? ColumnFilterEngine.Apply(_allInvoices, ColumnFilters)
            : _allInvoices.ToList();

        OpenInvoices.Clear();
        foreach (var inv in filtered)
            OpenInvoices.Add(inv);
    }

    protected override void OnColumnFiltersChanged() => ApplyFilters();

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

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            if (_allInvoices.Count == 0)
                await LoadAsync();

            var exportData = _allInvoices.Select(i => new
            {
                رقم_الفاتورة = i.InvoiceNumber,
                التاريخ = i.InvoiceDate.ToString("yyyy/MM/dd"),
                الزبون = i.CustomerName ?? "",
                المتبقي = i.RemainingAmount,
                العملة = i.PaymentCurrency.ToString()
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"تحصيل_الآجل_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "التحصيل");
                BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء التصدير: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintTable()
    {
        try
        {
            if (_allInvoices.Count == 0)
                await LoadAsync();

            var columns = new[] { "رقم الفاتورة", "التاريخ", "الزبون", "المتبقي", "العملة" };
            IList<object[]> rows = _allInvoices.Select(i => new object[]
            {
                i.InvoiceNumber,
                i.InvoiceDate.ToString("yyyy/MM/dd"),
                i.CustomerName ?? "",
                i.RemainingAmount.ToString("N0"),
                i.PaymentCurrency.ToString()
            }).ToList();
            _exportService.PrintTable("الفواتير المفتوحة للتحصيل", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task OpenInvoiceDetail(GoldInvoiceListItem? invoice)
    {
        invoice ??= SelectedInvoice;
        if (invoice is null)
            return;

        try
        {
            var full = await _saleService.GetByIdAsync(invoice.Id);
            if (full is null)
            {
                _toast.ShowError("لم يتم العثور على الفاتورة.");
                return;
            }

            GoldInvoiceDetailDialog.Show(full);
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }
}
