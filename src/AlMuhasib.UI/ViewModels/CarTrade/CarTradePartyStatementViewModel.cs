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
        PageTitle = "كشف حساب طرف";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CarTradePermissionRegistry.CarTradePartyStatement);
        await LoadPartyNamesAsync();
    }

    partial void OnPartySearchTextChanged(string value) => _ = LoadPartyNamesAsync();

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
        if (!CanExport || Rows.Count == 0)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"CarTradePartyStatement_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog() != true)
            return;

        var headers = new[] { "التاريخ", "رقم العملية", "النوع", "السيارة", "الإجمالي", "المدفوع", "المتبقي", "الدور" };
        var data = Rows.Select(r => new object?[]
        {
            r.TransactionDate.ToString("yyyy/MM/dd"), r.TransactionNumber, r.TradeType, r.CarName,
            r.TotalAmount, r.AmountPaid, r.RemainingAmount, r.PartyRole
        }).ToList();

        _exportService.ExportToExcel(dialog.FileName, "كشف حساب طرف", headers, data);
        _toast.ShowSuccess("تم تصدير الملف بنجاح");
    }

    [RelayCommand]
    private void PrintStatement()
    {
        if (!CanPrint || Rows.Count == 0)
            return;

        var cols = new[] { "التاريخ", "رقم العملية", "النوع", "السيارة", "الإجمالي", "المدفوع", "المتبقي", "الدور" };
        var tableRows = Rows.Select(r => new object[]
        {
            r.TransactionDate.ToString("yyyy/MM/dd"), r.TransactionNumber, r.TradeType, r.CarName,
            r.TotalAmount, r.AmountPaid, r.RemainingAmount, r.PartyRole
        }).ToList();

        var summary = new List<string>
        {
            $"الطرف: {PartyName}",
            $"الهاتف: {PartyPhone}",
            $"إجمالي المدين: {TotalDebit}",
            $"إجمالي الدائن: {TotalCredit}",
            $"الرصيد النهائي: {Balance}"
        };

        if (DateFrom.HasValue || DateTo.HasValue)
        {
            summary.Insert(0,
                $"الفترة: {DateFrom?.ToString("yyyy/MM/dd") ?? "—"} إلى {DateTo?.ToString("yyyy/MM/dd") ?? "—"}");
        }

        _exportService.PrintTable($"كشف حساب — {PartyName}", cols, tableRows, summary);
    }
}
