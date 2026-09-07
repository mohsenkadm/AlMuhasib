using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

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
    private void ShowAmountDetails(string? key)
    {
        if (_lastResult is null || string.IsNullOrWhiteSpace(key))
            return;

        var r = _lastResult;
        var periodNet = r.GrossProfit - r.TotalExpenses;
        AmountBreakdownModel model = key switch
        {
            "sales" => new AmountBreakdownModel
            {
                Title = "تفاصيل إجمالي المبيعات",
                Subtitle = "مجموع صافي فواتير المبيعات والأقساط ضمن الفترة",
                Formula = "المبيعات = Σ صافي فواتير البيع والأقساط (بدون أرصدة افتتاحية)",
                ResultLabel = "إجمالي المبيعات",
                ResultAmount = r.TotalSales,
                Lines =
                [
                    new AmountBreakdownLine
                    {
                        Operator = "Σ",
                        Label = "فواتير المبيعات والأقساط",
                        Amount = r.TotalSales,
                        Description = "يُستبعد رصيد افتتاحي العملاء وخطط الأقساط الافتتاحية"
                    }
                ],
                Note = r.TotalSales < 0
                    ? "المبلغ سالب بسبب مرتجعات مبيعات أكبر من المبيعات في الفترة."
                    : null
            },
            "cogs" => new AmountBreakdownModel
            {
                Title = "تفاصيل تكلفة المبيعات",
                Subtitle = "كلفة البضاعة المباعة (متوسط التكلفة × الكمية)",
                Formula = "التكلفة = Σ (كمية المباعة × متوسط تكلفة الوحدة)",
                ResultLabel = "تكلفة المبيعات",
                ResultAmount = r.TotalPurchases,
                Lines =
                [
                    new AmountBreakdownLine
                    {
                        Operator = "Σ",
                        Label = "كلفة البضاعة المباعة (COGS)",
                        Amount = r.TotalPurchases,
                        Description = "ليست مجموع فواتير المشتريات؛ تُحسب من متوسط كلفة المخزون"
                    }
                ]
            },
            "gross" => new AmountBreakdownModel
            {
                Title = "تفاصيل إجمالي الربح",
                Subtitle = "المبيعات ناقص تكلفة البضاعة المباعة",
                Formula = "إجمالي الربح = المبيعات − تكلفة المبيعات",
                ResultLabel = "إجمالي الربح",
                ResultAmount = r.GrossProfit,
                Lines =
                [
                    new AmountBreakdownLine { Operator = "+", Label = "إجمالي المبيعات", Amount = r.TotalSales },
                    new AmountBreakdownLine { Operator = "−", Label = "تكلفة المبيعات", Amount = r.TotalPurchases },
                    new AmountBreakdownLine { Operator = "=", Label = "إجمالي الربح", Amount = r.GrossProfit, IsResult = true }
                ],
                Note = r.GrossProfit < 0
                    ? "الرقم سالب لأن تكلفة المبيعات أكبر من المبيعات في هذه الفترة."
                    : null
            },
            "expenses" => new AmountBreakdownModel
            {
                Title = "تفاصيل إجمالي المصاريف",
                Subtitle = "مجموع المصروفات المسجّلة ضمن الفترة",
                Formula = "المصاريف = Σ مبالغ المصروفات",
                ResultLabel = "إجمالي المصاريف",
                ResultAmount = r.TotalExpenses,
                Lines =
                [
                    new AmountBreakdownLine
                    {
                        Operator = "Σ",
                        Label = "المصروفات",
                        Amount = r.TotalExpenses,
                        Description = "لا تشمل الرسوم البنكية ولا توزيعات الأرباح"
                    }
                ]
            },
            "net" => new AmountBreakdownModel
            {
                Title = "تفاصيل صافي الأرباح",
                Subtitle = "الصافي الظاهر في البطاقة، مع المكونات الكاملة للإيضاح",
                Formula = "الصافي الظاهر = إجمالي الربح − المصاريف",
                ResultLabel = "صافي الأرباح (البطاقة)",
                ResultAmount = periodNet,
                Lines =
                [
                    new AmountBreakdownLine { Operator = "+", Label = "إجمالي الربح", Amount = r.GrossProfit },
                    new AmountBreakdownLine { Operator = "−", Label = "إجمالي المصاريف", Amount = r.TotalExpenses },
                    new AmountBreakdownLine { Operator = "=", Label = "صافي الأرباح الظاهر", Amount = periodNet, IsResult = true },
                    new AmountBreakdownLine
                    {
                        Operator = "−",
                        Label = "رسوم بنكية (غير ظاهرة في البطاقة)",
                        Amount = r.TotalBankFees,
                        Description = "تُخصم في الصافي الكامل فقط"
                    },
                    new AmountBreakdownLine
                    {
                        Operator = "−",
                        Label = "توزيعات أرباح (غير ظاهرة في البطاقة)",
                        Amount = r.DistributedProfits
                    },
                    new AmountBreakdownLine
                    {
                        Operator = "+",
                        Label = "رصيد افتتاحي للأرباح",
                        Amount = r.ProfitOpeningBalance
                    },
                    new AmountBreakdownLine
                    {
                        Operator = "=",
                        Label = "الصافي الكامل",
                        Amount = r.NetProfit,
                        IsResult = true,
                        Description = "المبيعات − التكلفة − المصاريف − الرسوم − التوزيعات + الافتتاحي"
                    }
                ],
                Note = periodNet < 0 || r.NetProfit < 0
                    ? "الرقم السالب يعني أن الخصومات (تكلفة/مصاريف/رسوم/توزيعات) تجاوزت الإيرادات في الفترة."
                    : null
            },
            _ => new AmountBreakdownModel { Title = "تفاصيل المبلغ", ResultAmount = 0 }
        };

        AmountBreakdownDialog.Show(model);
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
