using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.CarTrade;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.CarTrade;

public partial class CarTradePartyStatementViewModel : ViewModelBase
{
    private readonly ICarTradeService _tradeService;
    private readonly ICarTradePrintService _printService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;

    private List<string> _allPartyNames = [];
    private List<CarTradePartyStatementRow> _allRows = [];
    private List<CarTradeDebtSummaryRow> _allSellerDebts = [];
    private List<CarTradeDebtSummaryRow> _allBuyerDebts = [];
    private System.Timers.Timer? _partySearchDebounce;

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private string _partySearchText = string.Empty;
    [ObservableProperty] private string? _selectedPartyName;
    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private string _partyName = "—";
    [ObservableProperty] private string _partyPhone = "—";
    [ObservableProperty] private string _totalDebit = "0";
    [ObservableProperty] private string _totalCredit = "0";
    [ObservableProperty] private string _balance = "0";

    [ObservableProperty] private bool _isPaymentDialogOpen;
    [ObservableProperty] private CarTradePartyStatementRow? _paymentRow;
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private DateTime _paymentDate = DateTime.Today;
    [ObservableProperty] private string _paymentNotes = string.Empty;
    [ObservableProperty] private string _paymentDialogTitle = string.Empty;
    [ObservableProperty] private string _paymentSummary = string.Empty;

    public ObservableCollection<string> PartyNames { get; } = [];
    public ObservableCollection<CarTradePartyStatementRow> Rows { get; } = [];
    public ObservableCollection<CarTradeDebtSummaryRow> SellerDebts { get; } = [];
    public ObservableCollection<CarTradeDebtSummaryRow> BuyerDebts { get; } = [];

    public CarTradePartyStatementViewModel(
        ICarTradeService tradeService,
        ICarTradePrintService printService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast)
    {
        _tradeService = tradeService;
        _printService = printService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _toast = toast;
        PageTitle = "كشف الحساب";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CarTradePermissionRegistry.CarTradePartyStatement);
        await LoadDebtSummariesAsync();
        await RefreshPartyNamesAsync();
    }

    partial void OnPartySearchTextChanged(string value)
    {
        _partySearchDebounce?.Stop();
        _partySearchDebounce?.Dispose();
        _partySearchDebounce = new System.Timers.Timer(250) { AutoReset = false };
        _partySearchDebounce.Elapsed += (_, _) =>
            App.Current.Dispatcher.Invoke(ApplyPartyNameFilter);
        _partySearchDebounce.Start();
    }

    protected override void OnColumnFiltersChanged() => ApplyAllGridFilters();

    [RelayCommand]
    private async Task LoadDebtSummariesAsync()
    {
        IsBusy = true;
        try
        {
            _allSellerDebts = (await _tradeService.GetSellerDebtsSummaryAsync()).ToList();
            _allBuyerDebts = (await _tradeService.GetBuyerDebtsSummaryAsync()).ToList();
            ApplyDebtFilters();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshPartyNamesAsync()
    {
        try
        {
            _allPartyNames = (await _tradeService.GetPartyNamesAsync(null)).ToList();
            ApplyPartyNameFilter();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    private void ApplyPartyNameFilter()
    {
        var term = PartySearchText?.Trim() ?? string.Empty;
        var filtered = string.IsNullOrWhiteSpace(term)
            ? _allPartyNames
            : _allPartyNames.Where(n => n.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();

        var previous = SelectedPartyName;
        PartyNames.Clear();
        foreach (var name in filtered)
            PartyNames.Add(name);

        if (!string.IsNullOrWhiteSpace(previous) && PartyNames.Contains(previous))
            SelectedPartyName = previous;
    }

    [RelayCommand]
    private async Task LoadStatementAsync()
    {
        var party = ResolveSelectedPartyName();
        if (string.IsNullOrWhiteSpace(party))
        {
            _toast.ShowWarning("يرجى اختيار الطرف أو كتابة اسمه");
            return;
        }

        SelectedPartyName = party;
        IsBusy = true;
        try
        {
            var data = await _tradeService.GetPartyStatementAsync(new CarTradePartyStatementFilter
            {
                PartyName = party,
                DateFrom = DateFrom,
                DateTo = DateTo
            });

            PartyName = data.PartyName;
            PartyPhone = string.IsNullOrWhiteSpace(data.PartyPhone) ? "—" : data.PartyPhone;
            TotalDebit = data.TotalDebit.ToString("N0");
            TotalCredit = data.TotalCredit.ToString("N0");
            Balance = data.Balance.ToString("N0");

            _allRows = data.Rows.ToList();
            ApplyRowFilters();
            SelectedTabIndex = 2;
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenPartyFromDebtAsync(CarTradeDebtSummaryRow? row)
    {
        if (row is null || string.IsNullOrWhiteSpace(row.PartyName))
            return;

        SelectedPartyName = row.PartyName;
        PartySearchText = row.PartyName;
        await LoadStatementAsync();
    }

    [RelayCommand]
    private void OpenSettleDialog(CarTradePartyStatementRow? row)
    {
        if (row is null || !row.CanSettle || !CanEdit)
        {
            if (row is not null && !row.CanSettle)
                _toast.ShowWarning("لا يوجد مبلغ متبقي لهذا القيد");
            return;
        }

        PaymentRow = row;
        PaymentAmount = row.RemainingAmount;
        PaymentDate = DateTime.Today;
        PaymentNotes = string.Empty;
        PaymentDialogTitle = row.IsSellerDebt ? "تسديد للبائع" : "تسديد من المشتري";
        PaymentSummary =
            $"عملية {row.TransactionNumber} — {row.CarName} — {row.DebtKind} — المتبقي: {row.RemainingAmount:N0}";
        IsPaymentDialogOpen = true;
    }

    [RelayCommand]
    private void ClosePaymentDialog()
    {
        IsPaymentDialogOpen = false;
        PaymentRow = null;
    }

    [RelayCommand]
    private async Task SubmitPaymentAsync()
    {
        if (PaymentRow is null)
            return;

        if (PaymentAmount <= 0)
        {
            _toast.ShowWarning("أدخل مبلغاً صحيحاً");
            return;
        }

        if (PaymentAmount > PaymentRow.RemainingAmount)
        {
            _toast.ShowWarning("المبلغ أكبر من المتبقي");
            return;
        }

        try
        {
            var row = PaymentRow;
            if (row.IsSellerDebt)
            {
                await _tradeService.RecordPurchasePaymentAsync(
                    row.TransactionId, PaymentAmount, PaymentDate, PaymentNotes);
            }
            else
            {
                await _tradeService.RecordSalePaymentAsync(
                    row.TransactionId, PaymentAmount, PaymentDate, PaymentNotes);
            }

            if (CanPrint)
            {
                var updated = await _tradeService.GetByIdAsync(row.TransactionId);
                var kind = row.IsSellerDebt
                    ? Core.Enums.CarTradePaymentKind.Purchase
                    : Core.Enums.CarTradePaymentKind.Sale;
                var payment = updated?.Payments
                    .Where(p => p.PaymentKind == kind)
                    .OrderByDescending(p => p.PaymentDate)
                    .ThenByDescending(p => p.Id)
                    .FirstOrDefault();

                if (updated is not null && payment is not null)
                    _printService.PrintPaymentReceipt(updated, payment);
            }

            IsPaymentDialogOpen = false;
            PaymentRow = null;
            _toast.ShowSuccess("تم تسجيل التسديد بنجاح");

            await LoadDebtSummariesAsync();
            await RefreshPartyNamesAsync();
            await LoadStatementAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (!CanExport)
            return;

        if (SelectedTabIndex == 0 && SellerDebts.Count == 0 ||
            SelectedTabIndex == 1 && BuyerDebts.Count == 0)
        {
            if (Rows.Count == 0)
                return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"CarTradeStatement_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog() != true)
            return;

        if (SelectedTabIndex == 2 && Rows.Count > 0)
        {
            var headers = new[] { "التاريخ", "رقم العملية", "النوع", "السيارة", "الإجمالي", "المدفوع", "المتبقي", "الدور", "نوع الدين" };
            var data = Rows.Select(r => new object?[]
            {
                r.TransactionDate.ToString("yyyy/MM/dd"), r.TransactionNumber, r.TradeType, r.CarName,
                r.TotalAmount, r.AmountPaid, r.RemainingAmount, r.PartyRole, r.DebtKind
            }).ToList();
            _exportService.ExportToExcel(dialog.FileName, "كشف طرف", headers, data);
        }
        else if (SelectedTabIndex == 0)
        {
            var headers = new[] { "البائع", "الهاتف", "عدد العمليات", "الإجمالي", "المدفوع", "المتبقي" };
            var data = SellerDebts.Select(r => new object?[]
            {
                r.PartyName, r.PartyPhone, r.TransactionCount, r.TotalAmount, r.AmountPaid, r.RemainingAmount
            }).ToList();
            _exportService.ExportToExcel(dialog.FileName, "ديون البائعين", headers, data);
        }
        else
        {
            var headers = new[] { "المشتري", "الهاتف", "عدد العمليات", "الإجمالي", "المدفوع", "المتبقي" };
            var data = BuyerDebts.Select(r => new object?[]
            {
                r.PartyName, r.PartyPhone, r.TransactionCount, r.TotalAmount, r.AmountPaid, r.RemainingAmount
            }).ToList();
            _exportService.ExportToExcel(dialog.FileName, "ديون المشترين", headers, data);
        }

        _toast.ShowSuccess("تم تصدير الملف بنجاح");
    }

    [RelayCommand]
    private void PrintStatement()
    {
        if (!CanPrint || SelectedTabIndex != 2 || Rows.Count == 0)
            return;

        var cols = new[] { "التاريخ", "رقم العملية", "النوع", "السيارة", "الإجمالي", "المدفوع", "المتبقي", "الدور", "نوع الدين" };
        var tableRows = Rows.Select(r => new object[]
        {
            r.TransactionDate.ToString("yyyy/MM/dd"), r.TransactionNumber, r.TradeType, r.CarName,
            r.TotalAmount, r.AmountPaid, r.RemainingAmount, r.PartyRole, r.DebtKind
        }).ToList();

        var summary = new List<string>
        {
            $"الطرف: {PartyName}",
            $"الهاتف: {PartyPhone}",
            $"إجمالي ديون البائع: {TotalDebit}",
            $"إجمالي ديون المشتري: {TotalCredit}",
            $"الرصيد النهائي: {Balance}"
        };

        if (DateFrom.HasValue || DateTo.HasValue)
        {
            summary.Insert(0,
                $"الفترة: {DateFrom?.ToString("yyyy/MM/dd") ?? "—"} إلى {DateTo?.ToString("yyyy/MM/dd") ?? "—"}");
        }

        _exportService.PrintTable($"كشف حساب — {PartyName}", cols, tableRows, summary);
    }

    private string? ResolveSelectedPartyName()
    {
        if (!string.IsNullOrWhiteSpace(SelectedPartyName))
            return SelectedPartyName.Trim();
        if (!string.IsNullOrWhiteSpace(PartySearchText))
            return PartySearchText.Trim();
        return null;
    }

    private void ApplyAllGridFilters()
    {
        ApplyDebtFilters();
        ApplyRowFilters();
    }

    private void ApplyDebtFilters()
    {
        SellerDebts.Clear();
        foreach (var row in ColumnFilterEngine.Apply(_allSellerDebts, ColumnFilters))
            SellerDebts.Add(row);

        BuyerDebts.Clear();
        foreach (var row in ColumnFilterEngine.Apply(_allBuyerDebts, ColumnFilters))
            BuyerDebts.Add(row);
    }

    private void ApplyRowFilters()
    {
        Rows.Clear();
        foreach (var row in ColumnFilterEngine.Apply(_allRows, ColumnFilters))
            Rows.Add(row);
    }
}
