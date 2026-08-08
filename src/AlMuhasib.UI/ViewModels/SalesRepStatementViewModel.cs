using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.SalesRep;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace AlMuhasib.UI.ViewModels;

public partial class SalesRepStatementViewModel : ReportViewModelBase
{
    private readonly ISalesRepService _salesRepService;

    public ObservableCollection<SalesRepresentative> Representatives { get; } = [];
    public ObservableCollection<SalesRepStatementLine> InvoiceRows { get; } = [];
    public ObservableCollection<SalesRepCommissionRow> CommissionRows { get; } = [];

    private List<SalesRepStatementLine> _allInvoices = [];
    private List<SalesRepCommissionRow> _allCommissions = [];

    [ObservableProperty] private SalesRepresentative? _selectedSalesRep;
    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private bool _showInvoicesTab = true;

    [ObservableProperty] private string _totalSales = "0";
    [ObservableProperty] private string _totalCollections = "0";
    [ObservableProperty] private string _remainingReceivables = "0";
    [ObservableProperty] private string _totalCommissions = "0";
    [ObservableProperty] private string _paidCommissions = "0";
    [ObservableProperty] private string _unpaidCommissions = "0";
    [ObservableProperty] private string _pendingHandover = "0";
    [ObservableProperty] private string _invoiceCount = "0";
    [ObservableProperty] private string _customerCount = "0";

    [ObservableProperty] private ISeries[] _chartSeries = [];
    [ObservableProperty] private Axis[] _chartXAxes = [];
    [ObservableProperty] private Axis[] _chartYAxes = [];

    [ObservableProperty] private string _searchText = string.Empty;

    public SalesRepStatementViewModel(
        ISalesRepService salesRepService,
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        _salesRepService = salesRepService;
        PageTitle = "كشف حساب المندوب";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "SalesRepStatement");
        Representatives.Clear();
        foreach (var r in (await _unitOfWork.SalesRepresentatives.GetAllAsync()).OrderBy(x => x.Name))
            Representatives.Add(r);
        SelectedSalesRep = Representatives.FirstOrDefault();
        if (SelectedSalesRep is not null)
            await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (SelectedSalesRep is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر مندوباً أولاً");
            return;
        }

        try
        {
            IsBusy = true;
            var statement = await _salesRepService.GetStatementAsync(SelectedSalesRep.Id, DateFrom, DateTo);

            TotalSales = FormatCurrency(statement.TotalSales);
            TotalCollections = FormatCurrency(statement.TotalCollections);
            RemainingReceivables = FormatCurrency(statement.RemainingReceivables);
            TotalCommissions = FormatCurrency(statement.TotalCommissions);
            PaidCommissions = FormatCurrency(statement.PaidCommissions);
            UnpaidCommissions = FormatCurrency(statement.UnpaidCommissions);
            PendingHandover = FormatCurrency(statement.PendingHandover);
            InvoiceCount = statement.InvoiceCount.ToString("N0");
            CustomerCount = statement.CustomerCount.ToString("N0");

            ChartSeries = [ChartThemeConfig.Column([statement.TotalSales, statement.TotalCommissions], "المبالغ", 0)];
            ChartXAxes = [ChartThemeConfig.CreateXAxis(["المبيعات", "العمولات"])];
            ChartYAxes = [ChartThemeConfig.CreateYAxis()];

            _allInvoices = statement.RecentInvoices.ToList();
            _allCommissions = statement.Commissions.ToList();
            CurrentPage = 1;
            ApplyCurrentTab();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        ShowInvoicesTab = value == 0;
        CurrentPage = 1;
        ApplyCurrentTab();
    }

    partial void OnSearchTextChanged(string value)
    {
        CurrentPage = 1;
        ApplyCurrentTab();
    }

    protected override void OnPageChanged() => ApplyCurrentTab();

    private void ApplyCurrentTab()
    {
        if (SelectedTabIndex == 0)
        {
            var filtered = FilterInvoices(_allInvoices);
            UpdatePaginationWithFilters(filtered, InvoiceRows);
        }
        else
        {
            var filtered = FilterCommissions(_allCommissions);
            UpdatePaginationWithFilters(filtered, CommissionRows);
        }
    }

    private List<SalesRepStatementLine> FilterInvoices(List<SalesRepStatementLine> source)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return source;
        var term = SearchText.Trim();
        return source.Where(r =>
            r.InvoiceNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
            || r.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private List<SalesRepCommissionRow> FilterCommissions(List<SalesRepCommissionRow> source)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return source;
        var term = SearchText.Trim();
        return source.Where(r =>
            r.InvoiceNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
            || r.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || SalesRepCommissionTypeLabels.Get(r.CommissionType).Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        try
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel|*.xlsx",
                FileName = $"كشف_مندوب_{SelectedSalesRep?.Name}_{DateTime.Now:yyyyMMdd}.xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            if (SelectedTabIndex == 0)
            {
                var cols = new[] { "الفاتورة", "التاريخ", "العميل", "الصافي", "المدفوع", "المتبقي", "العمولة" };
                var rows = _allInvoices.Select(r => new object[]
                {
                    r.InvoiceNumber, r.Date.ToString("yyyy/MM/dd"), r.CustomerName,
                    r.NetAmount, r.PaidAmount, r.RemainingAmount, r.CommissionAmount
                }).ToList();
                _exportService.ExportToExcel(dlg.FileName, "الفواتير", cols, rows);
            }
            else
            {
                var cols = new[] { "الفاتورة", "التاريخ", "العميل", "النوع", "الأساس", "العمولة", "المدفوع", "المتبقي", "الحالة" };
                var rows = _allCommissions.Select(r => new object[]
                {
                    r.InvoiceNumber, r.InvoiceDate.ToString("yyyy/MM/dd"), r.CustomerName,
                    SalesRepCommissionTypeLabels.Get(r.CommissionType),
                    r.BaseAmount, r.CommissionAmount, r.PaidAmount, r.UnpaidAmount, r.Status.ToString()
                }).ToList();
                _exportService.ExportToExcel(dlg.FileName, "العمولات", cols, rows);
            }

            BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void Print()
    {
        try
        {
            if (SelectedTabIndex == 0)
            {
                var cols = new[] { "الفاتورة", "التاريخ", "العميل", "الصافي", "المدفوع", "المتبقي", "العمولة" };
                IList<object[]> rows = _allInvoices.Select(r => new object[]
                {
                    r.InvoiceNumber, r.Date.ToString("yyyy/MM/dd"), r.CustomerName,
                    r.NetAmount.ToString("N0"), r.PaidAmount.ToString("N0"),
                    r.RemainingAmount.ToString("N0"), r.CommissionAmount.ToString("N0")
                }).ToList();
                _exportService.PrintTable($"كشف حساب المندوب — فواتير — {SelectedSalesRep?.Name}", cols, rows);
            }
            else
            {
                var cols = new[] { "الفاتورة", "التاريخ", "العميل", "النوع", "العمولة", "المتبقي" };
                IList<object[]> rows = _allCommissions.Select(r => new object[]
                {
                    r.InvoiceNumber, r.InvoiceDate.ToString("yyyy/MM/dd"), r.CustomerName,
                    SalesRepCommissionTypeLabels.Get(r.CommissionType),
                    r.CommissionAmount.ToString("N0"), r.UnpaidAmount.ToString("N0")
                }).ToList();
                _exportService.PrintTable($"كشف حساب المندوب — عمولات — {SelectedSalesRep?.Name}", cols, rows);
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }
}
