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
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;

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

    public ObservableCollection<string> PartyNames { get; } = [];
    public ObservableCollection<CarTradePartyStatementRow> Rows { get; } = [];
    public ObservableCollection<CarTradeDebtSummaryRow> SellerDebts { get; } = [];
    public ObservableCollection<CarTradeDebtSummaryRow> BuyerDebts { get; } = [];

    public CarTradePartyStatementViewModel(
        ICarTradeService tradeService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast)
    {
        _tradeService = tradeService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _toast = toast;
        PageTitle = "كشف الحساب";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CarTradePermissionRegistry.CarTradePartyStatement);
        await LoadDebtSummariesAsync();
        await LoadPartyNamesAsync();
    }

    partial void OnPartySearchTextChanged(string value) => _ = LoadPartyNamesAsync();

    [RelayCommand]
    private async Task LoadDebtSummariesAsync()
    {
        IsBusy = true;
        try
        {
            var sellers = await _tradeService.GetSellerDebtsSummaryAsync();
            var buyers = await _tradeService.GetBuyerDebtsSummaryAsync();

            SellerDebts.Clear();
            foreach (var row in sellers)
                SellerDebts.Add(row);

            BuyerDebts.Clear();
            foreach (var row in buyers)
                BuyerDebts.Add(row);
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
    private async Task LoadPartyNamesAsync()
    {
        var names = await _tradeService.GetPartyNamesAsync(PartySearchText);
        PartyNames.Clear();
        foreach (var name in names)
            PartyNames.Add(name);
    }

    [RelayCommand]
    private async Task LoadStatementAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedPartyName))
        {
            _toast.ShowWarning("يرجى اختيار الطرف");
            return;
        }

        IsBusy = true;
        try
        {
            var data = await _tradeService.GetPartyStatementAsync(new CarTradePartyStatementFilter
            {
                PartyName = SelectedPartyName,
                DateFrom = DateFrom,
                DateTo = DateTo
            });

            PartyName = data.PartyName;
            PartyPhone = string.IsNullOrWhiteSpace(data.PartyPhone) ? "—" : data.PartyPhone;
            TotalDebit = data.TotalDebit.ToString("N0");
            TotalCredit = data.TotalCredit.ToString("N0");
            Balance = data.Balance.ToString("N0");

            Rows.Clear();
            foreach (var row in data.Rows)
                Rows.Add(row);
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
}
