using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace AlMuhasib.UI.ViewModels;

public sealed class DailyOperationsDetailSection
{
    public int Index { get; init; }
    public string Title { get; init; } = string.Empty;
}

public partial class DailyOperationsReportViewModel : ReportViewModelBase
{
    private DailyOperationsReportResult? _lastResult;

    [ObservableProperty] private string _cashSales = "0";
    [ObservableProperty] private string _expenses = "0";
    [ObservableProperty] private string _salesReturns = "0";
    [ObservableProperty] private string _supplierPayments = "0";
    [ObservableProperty] private string _receipts = "0";
    [ObservableProperty] private string _netAmount = "0";

    [ObservableProperty] private string _cashSalesCount = "0";
    [ObservableProperty] private string _expenseCount = "0";
    [ObservableProperty] private string _salesReturnCount = "0";
    [ObservableProperty] private string _supplierPaymentCount = "0";
    [ObservableProperty] private string _receiptCount = "0";

    [ObservableProperty] private ISeries[] _summarySeries = [];
    [ObservableProperty] private ISeries[] _paymentMethodSeries = [];
    [ObservableProperty] private ISeries[] _userSeries = [];
    [ObservableProperty] private Axis[] _userXAxes = [];
    [ObservableProperty] private Axis[] _userYAxes = [];
    [ObservableProperty] private ISeries[] _categorySeries = [];
    [ObservableProperty] private Axis[] _categoryXAxes = [];
    [ObservableProperty] private Axis[] _categoryYAxes = [];

    [ObservableProperty] private bool _showBranchSection;
    [ObservableProperty] private int _selectedDetailIndex;
    [ObservableProperty] private string _detailTableTitle = "مبيعات حسب المستخدم";

    private List<DailyOperationsBranchRow> _allBranchRows = [];
    private List<DailyOperationsUserRow> _allUserRows = [];
    private List<DailyOperationsPaymentMethodRow> _allPaymentRows = [];
    private List<DailyOperationsCategoryRow> _allCategoryRows = [];
    private List<DailyOperationsProductRow> _allProductRows = [];

    public ObservableCollection<DailyOperationsBranchRow> BranchRows { get; } = [];
    public ObservableCollection<DailyOperationsUserRow> UserRows { get; } = [];
    public ObservableCollection<DailyOperationsPaymentMethodRow> PaymentMethodRows { get; } = [];
    public ObservableCollection<DailyOperationsCategoryRow> CategoryRows { get; } = [];
    public ObservableCollection<DailyOperationsProductRow> ProductRows { get; } = [];
    public ObservableCollection<DailyOperationsDetailSection> DetailSections { get; } = [];

    public bool ShowUsersGrid => SelectedDetailIndex == 0;
    public bool ShowPaymentGrid => SelectedDetailIndex == 1;
    public bool ShowCategoryGrid => SelectedDetailIndex == 2;
    public bool ShowProductGrid => SelectedDetailIndex == 3;
    public bool ShowBranchGrid => SelectedDetailIndex == 4 && ShowBranchSection;

    public DailyOperationsReportViewModel(
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "التقرير اليومي";
        DateFrom = DateTime.Today;
        DateTo = DateTime.Today;
        RebuildDetailSections();
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        await LoadDataAsync();
    }

    partial void OnSelectedDetailIndexChanged(int value)
    {
        DetailTableTitle = DetailSections.FirstOrDefault(s => s.Index == value)?.Title ?? "التفاصيل";
        OnPropertyChanged(nameof(ShowUsersGrid));
        OnPropertyChanged(nameof(ShowPaymentGrid));
        OnPropertyChanged(nameof(ShowCategoryGrid));
        OnPropertyChanged(nameof(ShowProductGrid));
        OnPropertyChanged(nameof(ShowBranchGrid));
        CurrentPage = 1;
        RefreshActivePagination();
    }

    partial void OnShowBranchSectionChanged(bool value)
    {
        RebuildDetailSections();
        if (!value && SelectedDetailIndex == 4)
            SelectedDetailIndex = 0;
        OnPropertyChanged(nameof(ShowBranchGrid));
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetDailyOperationsReportAsync(DateFrom, DateTo, CurrencyScope);
            _lastResult = result;

            CashSales = FormatCurrency(result.CashSales);
            Expenses = FormatCurrency(result.Expenses);
            SalesReturns = FormatCurrency(result.SalesReturns);
            SupplierPayments = FormatCurrency(result.SupplierPayments);
            Receipts = FormatCurrency(result.Receipts);
            NetAmount = FormatCurrency(result.NetAmount);

            CashSalesCount = result.CashSalesInvoiceCount.ToString("N0");
            ExpenseCount = result.ExpenseCount.ToString("N0");
            SalesReturnCount = result.SalesReturnInvoiceCount.ToString("N0");
            SupplierPaymentCount = result.SupplierPaymentCount.ToString("N0");
            ReceiptCount = result.ReceiptCount.ToString("N0");

            SummarySeries = result.SummaryChart.Count > 0
                ? ChartThemeConfig.PieFromNameAmount(result.SummaryChart)
                : [];
            PaymentMethodSeries = result.PaymentMethodChart.Count > 0
                ? ChartThemeConfig.PieFromNameAmount(result.PaymentMethodChart)
                : [];

            if (result.UserChart.Count > 0)
            {
                UserSeries = [ChartThemeConfig.Column(result.UserChart.Select(d => d.Amount).ToArray(), "المبيعات", 0)];
                UserXAxes = [ChartThemeConfig.CreateXAxis(result.UserChart.Select(d => d.Name).ToArray(), -35)];
                UserYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            else
            {
                UserSeries = [];
                UserXAxes = [];
                UserYAxes = [];
            }

            if (result.CategoryChart.Count > 0)
            {
                CategorySeries = [ChartThemeConfig.Column(result.CategoryChart.Select(d => d.Amount).ToArray(), "حسب الصنف", 1)];
                CategoryXAxes = [ChartThemeConfig.CreateXAxis(result.CategoryChart.Select(d => d.Name).ToArray(), -35)];
                CategoryYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            else
            {
                CategorySeries = [];
                CategoryXAxes = [];
                CategoryYAxes = [];
            }

            _allBranchRows = result.BranchRows;
            _allUserRows = result.UserRows;
            _allPaymentRows = result.PaymentMethodRows;
            _allCategoryRows = result.CategoryRows;
            _allProductRows = result.ProductRows;
            ShowBranchSection = _allBranchRows.Count > 1;

            CurrentPage = 1;
            RefreshActivePagination();
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

    protected override void OnPageChanged() => RefreshActivePagination();

    private void RebuildDetailSections()
    {
        DetailSections.Clear();
        DetailSections.Add(new DailyOperationsDetailSection { Index = 0, Title = "مبيعات حسب المستخدم" });
        DetailSections.Add(new DailyOperationsDetailSection { Index = 1, Title = "مبيعات حسب طريقة الدفع" });
        DetailSections.Add(new DailyOperationsDetailSection { Index = 2, Title = "مبيعات حسب الصنف" });
        DetailSections.Add(new DailyOperationsDetailSection { Index = 3, Title = "مبيعات حسب المادة" });
        if (ShowBranchSection)
            DetailSections.Add(new DailyOperationsDetailSection { Index = 4, Title = "ملخص حسب الفرع" });
        DetailTableTitle = DetailSections.FirstOrDefault(s => s.Index == SelectedDetailIndex)?.Title
                           ?? DetailSections.FirstOrDefault()?.Title
                           ?? "التفاصيل";
    }

    private void RefreshActivePagination()
    {
        switch (SelectedDetailIndex)
        {
            case 1:
                UpdatePaginationWithFilters(_allPaymentRows, PaymentMethodRows);
                break;
            case 2:
                UpdatePaginationWithFilters(_allCategoryRows, CategoryRows);
                break;
            case 3:
                UpdatePaginationWithFilters(_allProductRows, ProductRows);
                break;
            case 4 when ShowBranchSection:
                UpdatePaginationWithFilters(_allBranchRows, BranchRows);
                break;
            default:
                UpdatePaginationWithFilters(_allUserRows, UserRows);
                break;
        }
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (_lastResult is null)
        {
            BeautifulMessageDialog.ShowWarning("لا توجد بيانات للتصدير. اضغط بحث أولاً.");
            return;
        }

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = $"التقرير_اليومي_{DateFrom:yyyyMMdd}_{DateTo:yyyyMMdd}.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var summaryRows = new List<object[]>
        {
            new object[] { "مبيعات نقدية", _lastResult.CashSales, _lastResult.CashSalesInvoiceCount },
            new object[] { "المصاريف", _lastResult.Expenses, _lastResult.ExpenseCount },
            new object[] { "مرجوعات المبيعات", _lastResult.SalesReturns, _lastResult.SalesReturnInvoiceCount },
            new object[] { "مدفوعات الموردين", _lastResult.SupplierPayments, _lastResult.SupplierPaymentCount },
            new object[] { "وصولات القبض", _lastResult.Receipts, _lastResult.ReceiptCount },
            new object[] { "الصافي", _lastResult.NetAmount, "—" },
        };

        var sheets = new List<(string SheetName, string[] Columns, IList<object[]> Rows)>
        {
            ("الملخص",
                new[] { "البند", "المبلغ", "العدد" },
                summaryRows),
            ("المستخدمون",
                new[] { "المستخدم", "عدد الفواتير", "المبلغ", "النسبة %" },
                _allUserRows.Select(r => new object[] { r.UserName, r.InvoiceCount, r.Amount, r.SharePercent }).ToList()),
            ("طرق الدفع",
                new[] { "طريقة الدفع", "عدد الفواتير", "المبلغ", "النسبة %" },
                _allPaymentRows.Select(r => new object[] { r.PaymentMethod, r.InvoiceCount, r.Amount, r.SharePercent }).ToList()),
            ("الأصناف",
                new[] { "الصنف", "عدد البنود", "الكمية", "المبلغ", "النسبة %" },
                _allCategoryRows.Select(r => new object[] { r.CategoryName, r.LineCount, r.Quantity, r.Amount, r.SharePercent }).ToList()),
            ("المواد",
                new[] { "الترتيب", "المادة", "الصنف", "عدد البنود", "الكمية", "المبلغ", "النسبة %" },
                _allProductRows.Select(r => new object[]
                {
                    r.Rank, r.ProductName, r.CategoryName, r.LineCount, r.Quantity, r.Amount, r.SharePercent
                }).ToList()),
        };

        if (_allBranchRows.Count > 0)
        {
            sheets.Add(("الفروع",
                ["الفرع", "مبيعات نقدية", "مصاريف", "مرتجعات", "مدفوعات موردين", "وصولات قبض", "الصافي"],
                _allBranchRows.Select(r => new object[]
                {
                    r.BranchName, r.CashSales, r.Expenses, r.SalesReturns, r.SupplierPayments, r.Receipts, r.NetAmount
                }).ToList()));
        }

        _exportService.ExportToExcel(dlg.FileName, sheets);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        if (_lastResult is null)
        {
            BeautifulMessageDialog.ShowWarning("لا توجد بيانات للطباعة. اضغط بحث أولاً.");
            return;
        }

        var summaryLines = new List<string>
        {
            $"من {_lastResult.DateFrom:yyyy/MM/dd} إلى {_lastResult.DateTo:yyyy/MM/dd}",
            $"مبيعات نقدية: {_lastResult.CashSales:N0}",
            $"المصاريف: {_lastResult.Expenses:N0}",
            $"مرجوعات المبيعات: {_lastResult.SalesReturns:N0}",
            $"مدفوعات الموردين: {_lastResult.SupplierPayments:N0}",
            $"وصولات القبض: {_lastResult.Receipts:N0}",
            $"الصافي: {_lastResult.NetAmount:N0}",
        };

        switch (SelectedDetailIndex)
        {
            case 1:
                _exportService.PrintTable(
                    "التقرير اليومي — طرق الدفع",
                    ["طريقة الدفع", "عدد الفواتير", "المبلغ", "النسبة %"],
                    _allPaymentRows.Select(r => new object[] { r.PaymentMethod, r.InvoiceCount, r.Amount, r.SharePercent }).ToList(),
                    summaryLines);
                break;
            case 2:
                _exportService.PrintTable(
                    "التقرير اليومي — الأصناف",
                    ["الصنف", "عدد البنود", "الكمية", "المبلغ", "النسبة %"],
                    _allCategoryRows.Select(r => new object[] { r.CategoryName, r.LineCount, r.Quantity, r.Amount, r.SharePercent }).ToList(),
                    summaryLines);
                break;
            case 3:
                _exportService.PrintTable(
                    "التقرير اليومي — المواد",
                    ["الترتيب", "المادة", "الصنف", "الكمية", "المبلغ", "النسبة %"],
                    _allProductRows.Select(r => new object[]
                    {
                        r.Rank, r.ProductName, r.CategoryName, r.Quantity, r.Amount, r.SharePercent
                    }).ToList(),
                    summaryLines);
                break;
            case 4 when ShowBranchSection:
                _exportService.PrintTable(
                    "التقرير اليومي — الفروع",
                    ["الفرع", "مبيعات نقدية", "مصاريف", "مرتجعات", "مدفوعات", "قبض", "الصافي"],
                    _allBranchRows.Select(r => new object[]
                    {
                        r.BranchName, r.CashSales, r.Expenses, r.SalesReturns, r.SupplierPayments, r.Receipts, r.NetAmount
                    }).ToList(),
                    summaryLines);
                break;
            default:
                _exportService.PrintTable(
                    "التقرير اليومي — المستخدمون",
                    ["المستخدم", "عدد الفواتير", "المبلغ", "النسبة %"],
                    _allUserRows.Select(r => new object[] { r.UserName, r.InvoiceCount, r.Amount, r.SharePercent }).ToList(),
                    summaryLines);
                break;
        }
    }
}
