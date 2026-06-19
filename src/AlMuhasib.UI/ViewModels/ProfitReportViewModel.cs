using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Charts;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class ProfitReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalSales = "0";
    [ObservableProperty] private string _costOfSales = "0";
    [ObservableProperty] private string _grossProfit = "0";
    [ObservableProperty] private string _totalExpenses = "0";
    [ObservableProperty] private string _netProfit = "0";
    [ObservableProperty] private bool _isDetailsVisible;
    [ObservableProperty] private string _detailProductCount = "0";
    [ObservableProperty] private string _detailTotalQuantity = "0";
    [ObservableProperty] private string _detailTopProduct = "—";
    [ObservableProperty] private string _detailInvoiceCount = "0";
    [ObservableProperty] private string _detailInvoiceRevenue = "0";
    [ObservableProperty] private string _detailInvoiceProfit = "0";

    [ObservableProperty] private ISeries[] _periodSeries = [];
    [ObservableProperty] private Axis[] _periodXAxes = [];
    [ObservableProperty] private Axis[] _periodYAxes = [];

    private ProfitReportResult? _lastResult;
    private List<ProductProfitMarginRow> _detailRows = [];
    private List<ProfitInvoiceDetailRow> _invoiceRows = [];

    public ObservableCollection<ProductProfitMarginRow> DetailRows { get; } = [];
    public ObservableCollection<ProfitInvoiceDetailRow> InvoiceRows { get; } = [];

    public ProfitReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "تقرير الأرباح";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetProfitReportAsync(DateFrom, DateTo);
            _lastResult = result;

            var periodNet = result.GrossProfit - result.TotalExpenses;

            TotalSales = FormatCurrency(result.TotalSales);
            CostOfSales = FormatCurrency(result.TotalPurchases);
            GrossProfit = FormatCurrency(result.GrossProfit);
            TotalExpenses = FormatCurrency(result.TotalExpenses);
            NetProfit = FormatCurrency(periodNet);

            PeriodSeries =
            [
                ChartThemeConfig.Column([result.TotalSales], "إجمالي المبيعات", 0),
                ChartThemeConfig.Column([result.TotalPurchases], "تكلفة المبيعات", 3),
                ChartThemeConfig.Column([result.GrossProfit], "إجمالي الربح", 2),
                ChartThemeConfig.Column([result.TotalExpenses], "المصاريف", 1),
                ChartThemeConfig.Column([periodNet], "صافي الأرباح", 4)
            ];
            PeriodXAxes = [ChartThemeConfig.CreateXAxis(["الفترة المحددة"], 0)];
            PeriodYAxes = [ChartThemeConfig.CreateYAxis()];

            if (IsDetailsVisible)
                await LoadDetailsAsync();
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ToggleDetailsAsync()
    {
        IsDetailsVisible = !IsDetailsVisible;
        if (IsDetailsVisible)
            await LoadDetailsAsync();
    }

    private async Task LoadDetailsAsync()
    {
        var productTask = _reportService.GetProductProfitMarginReportAsync(DateFrom, DateTo, null);
        var invoiceTask = _reportService.GetProfitInvoiceDetailsAsync(DateFrom, DateTo);
        await Task.WhenAll(productTask, invoiceTask);

        var details = await productTask;
        _detailRows = details.Rows.OrderByDescending(r => r.GrossProfit).ToList();
        DetailRows.Clear();
        foreach (var row in _detailRows)
            DetailRows.Add(row);

        DetailProductCount = _detailRows.Count.ToString();
        DetailTotalQuantity = _detailRows.Sum(r => r.QuantitySold).ToString("N0");
        DetailTopProduct = _detailRows.FirstOrDefault()?.ProductName ?? "—";

        _invoiceRows = (await invoiceTask).OrderByDescending(r => r.Date).ToList();
        InvoiceRows.Clear();
        foreach (var row in _invoiceRows)
            InvoiceRows.Add(row);

        DetailInvoiceCount = _invoiceRows.Count.ToString();
        DetailInvoiceRevenue = FormatCurrency(_invoiceRows.Sum(r => r.Revenue));
        DetailInvoiceProfit = FormatCurrency(_invoiceRows.Sum(r => r.GrossProfit));
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "تقرير_الأرباح.xlsx" };
        if (dlg.ShowDialog() != true) return;

        var summaryCols = new[] { "البند", "المبلغ" };
        var periodNet = (_lastResult?.GrossProfit ?? 0) - (_lastResult?.TotalExpenses ?? 0);
        var summaryRows = BuildSummaryRows(periodNet);

        _exportService.ExportToExcel(dlg.FileName, "الأرباح", summaryCols, summaryRows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "البند", "المبلغ" };
        var periodNet = (_lastResult?.GrossProfit ?? 0) - (_lastResult?.TotalExpenses ?? 0);
        var rows = BuildSummaryRows(periodNet);
        _exportService.PrintTable("تقرير الأرباح", cols, rows);
    }

    private List<object[]> BuildSummaryRows(decimal periodNet)
    {
        var rows = new List<object[]>
        {
            new object[] { "من تاريخ", DateFrom?.ToString("yyyy/MM/dd") ?? "" },
            new object[] { "إلى تاريخ", DateTo?.ToString("yyyy/MM/dd") ?? "" },
            new object[] { "إجمالي المبيعات", _lastResult?.TotalSales ?? 0 },
            new object[] { "تكلفة المبيعات", _lastResult?.TotalPurchases ?? 0 },
            new object[] { "إجمالي الربح", _lastResult?.GrossProfit ?? 0 },
            new object[] { "إجمالي المصاريف", _lastResult?.TotalExpenses ?? 0 },
            new object[] { "صافي الأرباح", periodNet }
        };

        if (_invoiceRows.Count > 0)
        {
            rows.Add(new object[] { "", "" });
            rows.Add(new object[] { "═══ تفاصيل الفواتير ═══", "" });
            foreach (var r in _invoiceRows)
                rows.Add(new object[] { $"{r.InvoiceNumber} - {r.CustomerName}", $"مبيعات: {r.Revenue:N0} | تكلفة: {r.Cost:N0} | ربح: {r.GrossProfit:N0}" });
        }

        if (_detailRows.Count > 0)
        {
            rows.Add(new object[] { "", "" });
            rows.Add(new object[] { "═══ تفاصيل المنتجات ═══", "" });
            foreach (var r in _detailRows)
                rows.Add(new object[] { r.ProductName, r.GrossProfit });
        }

        return rows;
    }
}
