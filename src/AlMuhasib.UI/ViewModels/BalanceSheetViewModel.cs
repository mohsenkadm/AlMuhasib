using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class BalanceSheetViewModel : ViewModelBase
{
    private readonly IReportService _reportService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;

    public BalanceSheetViewModel(IReportService reportService, IExportService exportService, ICurrentUserService currentUserService)
    {
        _reportService = reportService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        PageTitle = "موازنة يومية";
    }

    // ── Date Filter ─────────────────────────────────────

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    // ── EQUITY (RIGHT) ──────────────────────────────────

    [ObservableProperty] private decimal _capital;
    [ObservableProperty] private decimal _adjustments;
    [ObservableProperty] private decimal _accumulatedProfits;
    [ObservableProperty] private decimal _equityTotal;
    [ObservableProperty] private decimal _profitOpeningBalance;
    [ObservableProperty] private decimal _salesTotal;
    [ObservableProperty] private decimal _costOfSales;
    [ObservableProperty] private decimal _salesProfit;
    [ObservableProperty] private decimal _expensesTotal;

    // ── LIABILITIES ─────────────────────────────────────

    [ObservableProperty] private decimal _supplierPayables;
    [ObservableProperty] private decimal _investorDeposits;
    [ObservableProperty] private decimal _liabilitiesTotal;
    [ObservableProperty] private decimal _equityAndLiabilitiesTotal;

    // ── ASSETS (LEFT) ───────────────────────────────────

    [ObservableProperty] private decimal _cashBoxesTotal;
    [ObservableProperty] private decimal _banksTotal;
    [ObservableProperty] private decimal _customerDebts;
    [ObservableProperty] private decimal _inventoryValue;
    [ObservableProperty] private decimal _installmentReceivables;
    [ObservableProperty] private decimal _assetsTotal;

    public ObservableCollection<CashBoxLine> CashBoxLines { get; } = [];
    public ObservableCollection<BankLine> BankLines { get; } = [];

    // ── Balance Status ──────────────────────────────────

    [ObservableProperty] private bool _isBalanced;
    [ObservableProperty] private decimal _difference;
    [ObservableProperty] private string _balanceStatusText = string.Empty;

    // ── Commands ────────────────────────────────────────

    [RelayCommand]
    private async Task LoadBalanceSheetAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetBalanceSheetAsync(SelectedDate);

            // Equity
            Capital = result.Capital;
            Adjustments = result.Adjustments;
            AccumulatedProfits = result.AccumulatedProfits;
            EquityTotal = result.EquityTotal;
            ProfitOpeningBalance = result.ProfitOpeningBalance;
            SalesTotal = result.SalesTotal;
            CostOfSales = result.CostOfSales;
            SalesProfit = result.SalesProfit;
            ExpensesTotal = result.ExpensesTotal;

            // Liabilities
            SupplierPayables = result.SupplierPayables;
            InvestorDeposits = result.InvestorDeposits;
            LiabilitiesTotal = result.LiabilitiesTotal;
            EquityAndLiabilitiesTotal = result.EquityAndLiabilitiesTotal;

            // Assets
            CashBoxesTotal = result.CashBoxesTotal;
            CashBoxLines.Clear();
            foreach (var c in result.CashBoxes)
                CashBoxLines.Add(new CashBoxLine { Name = c.Name, Balance = c.Balance });

            BanksTotal = result.BanksTotal;
            BankLines.Clear();
            foreach (var b in result.Banks)
                BankLines.Add(new BankLine { Name = b.Name, Balance = b.Balance });

            CustomerDebts = result.CustomerDebts;
            InventoryValue = result.InventoryValue;
            InstallmentReceivables = result.InstallmentReceivables;
            AssetsTotal = result.AssetsTotal;

            // Status
            IsBalanced = result.IsBalanced;
            Difference = result.Difference;
            BalanceStatusText = result.IsBalanced
                ? "✅  متوازنة"
                : $"❌  غير متوازنة — فرق: {result.Difference:N0}";
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void PrintBalanceSheet()
    {
        var cols = new[] { "البند", "المبلغ" };
        var rows = BuildBalanceSheetRows();
        _exportService.PrintTable($"موازنة يومية - {SelectedDate:yyyy/MM/dd}", cols, (IList<object[]>)rows);
    }

    [RelayCommand]
    private void ExportBalanceSheet()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"موازنة_يومية_{SelectedDate:yyyyMMdd}.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "البند", "المبلغ" };
        var rows = BuildBalanceSheetRows();
        _exportService.ExportToExcel(dlg.FileName, "موازنة يومية", cols, (IList<object[]>)rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    private List<object[]> BuildBalanceSheetRows()
    {
        var rows = new List<object[]>
        {
            new object[] { "═══ حقوق الملكية ═══", "" },
            new object[] { "رأس المال", Capital },
            new object[] { "تسويات", Adjustments },
            new object[] { "  رصيد الأرباح الافتتاحي", ProfitOpeningBalance },
            new object[] { "  + إجمالي المبيعات", SalesTotal },
            new object[] { "  − تكلفة المبيعات", CostOfSales },
            new object[] { "  = أرباح المبيعات", SalesProfit },
            new object[] { "  − المصاريف", ExpensesTotal },
            new object[] { "الأرباح المتراكمة", AccumulatedProfits },
            new object[] { "إجمالي حقوق الملكية", EquityTotal },
            new object[] { "", "" },
            new object[] { "═══ الالتزامات ═══", "" },
            new object[] { "المطلوبات للموردين", SupplierPayables },
            new object[] { "ودائع مستثمرين (جديدة)", InvestorDeposits },
            new object[] { "إجمالي الالتزامات", LiabilitiesTotal },
            new object[] { "", "" },
            new object[] { "إجمالي الملكية والالتزامات", EquityAndLiabilitiesTotal },
            new object[] { "", "" },
            new object[] { "═══ الموجودات ═══", "" },
            new object[] { "الصناديق (القاصات)", CashBoxesTotal },
        };
        foreach (var c in CashBoxLines)
            rows.Add(new object[] { $"  - {c.Name}", c.Balance });
        rows.Add(new object[] { "المصارف", BanksTotal });
        foreach (var b in BankLines)
            rows.Add(new object[] { $"  - {b.Name}", b.Balance });
        rows.Add(new object[] { "المدينون (ديون العملاء)", CustomerDebts });
        rows.Add(new object[] { "قيمة مواد المخزون", InventoryValue });
        rows.Add(new object[] { "المبالغ المطلوبة (أقساط)", InstallmentReceivables });
        rows.Add(new object[] { "إجمالي الموجودات", AssetsTotal });
        rows.Add(new object[] { "", "" });
        rows.Add(new object[] { BalanceStatusText, "" });
        return rows;
    }

    // ── Initialization ──────────────────────────────────

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "BalanceSheet");

        await LoadBalanceSheetAsync();
    }
}

// ── Display models ──────────────────────────────────────

public class CashBoxLine
{
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

public class BankLine
{
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
