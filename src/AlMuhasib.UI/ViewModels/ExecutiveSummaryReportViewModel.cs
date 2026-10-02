using System.Collections.ObjectModel;
using System.Windows.Media;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.ViewModels;

public sealed class ExecutiveSummaryCardItem
{
    public string MetricKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Hint { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal AmountUsd { get; init; }
    public string? Suffix { get; init; }
    public string Value { get; init; } = string.Empty;
    public string? SecondaryValue { get; init; }
    public bool ShowSecondaryValue { get; init; }
    public PackIconKind Icon { get; init; } = PackIconKind.ChartLine;
    public Brush AccentBrush { get; init; } = Brushes.SteelBlue;
    public Brush AccentLightBrush { get; init; } = Brushes.AliceBlue;
    public Brush ValueBrush { get; init; } = Brushes.Black;
}

public sealed class ExecutiveSummarySectionItem
{
    public string Title { get; init; } = string.Empty;
    public Brush AccentBrush { get; init; } = Brushes.Teal;
    public ObservableCollection<ExecutiveSummaryCardItem> Cards { get; init; } = [];
}

public partial class ExecutiveSummaryReportViewModel : ReportViewModelBase
{
    private readonly IFeatureFlagService _featureFlags;
    private ExecutiveBusinessSummaryResult? _lastResult;

    public ObservableCollection<ExecutiveSummarySectionItem> Sections { get; } = [];

    [ObservableProperty] private string _periodHint = string.Empty;
    [ObservableProperty] private int _cardCount;
    [ObservableProperty] private bool _showMultiCurrency;
    [ObservableProperty] private int _cardsColumns = 4;
    [ObservableProperty] private double _cardsMinHeight = 128;

    public ExecutiveSummaryReportViewModel(
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IFeatureFlagService featureFlags)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        _featureFlags = featureFlags;
        PageTitle = "الملخص التنفيذي للأعمال";
        ShowMultiCurrency = _featureFlags.MultiCurrency;
        CardsColumns = ShowMultiCurrency ? 3 : 4;
        CardsMinHeight = ShowMultiCurrency ? 168 : 128;
        _featureFlags.FlagsChanged += (_, _) =>
            FeatureUiRefresh.Invoke(() =>
            {
                ShowMultiCurrency = _featureFlags.MultiCurrency;
                CardsColumns = ShowMultiCurrency ? 3 : 4;
                CardsMinHeight = ShowMultiCurrency ? 168 : 128;
                if (_lastResult is not null)
                    BuildSections(_lastResult);
            });
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
            var result = await _reportService.GetExecutiveBusinessSummaryAsync(DateFrom, DateTo);
            _lastResult = result;
            BuildSections(result);
            PeriodHint =
                $"الأرصدة حتى {(result.DateTo ?? DateTime.Today):yyyy/MM/dd} · نشاط الفترة من {(result.DateFrom ?? DateTime.Today.AddMonths(-1)):yyyy/MM/dd} إلى {(result.DateTo ?? DateTime.Today):yyyy/MM/dd}";
            CardCount = Sections.Sum(s => s.Cards.Count);
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

    [RelayCommand]
    private void ShowCardDetails(string? key)
    {
        if (_lastResult is null || string.IsNullOrWhiteSpace(key)) return;
        var model = BuildBreakdown(key, _lastResult);
        if (model is null) return;
        model = AttachUsdResult(key, model, _lastResult);
        AmountBreakdownDialog.Show(model);
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        if (_lastResult is null || Sections.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("لا توجد بيانات للتصدير. اضغط بحث أولاً.");
            return;
        }

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = "الملخص_التنفيذي.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var (cols, rows) = BuildExportRows();
        _exportService.ExportToExcel(dlg.FileName, "الملخص التنفيذي", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        if (Sections.Count == 0)
        {
            BeautifulMessageDialog.ShowWarning("لا توجد بيانات للطباعة. اضغط بحث أولاً.");
            return;
        }

        var (cols, rows) = BuildExportRows();
        _exportService.PrintTable("الملخص التنفيذي للأعمال", cols, rows);
    }

    private (string[] Cols, List<object[]> Rows) BuildExportRows()
    {
        if (ShowMultiCurrency)
        {
            var cols = new[] { "القسم", "المؤشر", "دينار عراقي (د.ع)", "دولار ($)" };
            var rows = Sections
                .SelectMany(s => s.Cards.Select(c => new object[]
                {
                    s.Title,
                    c.Title,
                    c.Value,
                    c.ShowSecondaryValue
                        ? AccountingCurrencyHelper.Format(c.AmountUsd, AccountingCurrency.USD)
                        : "—"
                }))
                .ToList();
            return (cols, rows);
        }

        return (
            new[] { "القسم", "المؤشر", "القيمة" },
            Sections.SelectMany(s => s.Cards.Select(c => new object[] { s.Title, c.Title, c.Value })).ToList());
    }

    private void BuildSections(ExecutiveBusinessSummaryResult r)
    {
        Sections.Clear();

        Sections.Add(MakeSection("المركز المالي", "#00695C",
        [
            MoneyCard("customerCredit", "رصيد الآجل للعملاء",
                "متبقي فواتير الآجل بعد خصم سندات القبض/الدين غير المطبّقة",
                r.CustomerReceivables, r.CustomerReceivablesUsd, PackIconKind.AccountCash, "#AD1457", "#FCE4EC"),
            MoneyCard("installmentAr", "ذمم الأقساط",
                "مجموع المتبقي من أقساط غير مسددة بالكامل",
                r.InstallmentReceivables, r.InstallmentReceivablesUsd, PackIconKind.CalendarClock, "#EF6C00", "#FFF3E0"),
            MoneyCard("supplierCredit", "رصيد الآجل للموردين",
                "متبقي مشتريات الآجل بعد خصم سندات الصرف غير المطبّقة",
                r.SupplierPayables, r.SupplierPayablesUsd, PackIconKind.TruckDelivery, "#00695C", "#E0F2F1"),
            MoneyCard("inventoryCost", "قيمة المخزون (تكلفة)",
                "كمية المخزن × متوسط تكلفة الشراء لكل منتج",
                r.InventoryCostValue, 0, PackIconKind.Warehouse, "#1565C0", "#E3F2FD"),
            QtyCard("inventoryQty", "كمية المخزون",
                "إجمالي الكميات المتوفرة في كل المخازن",
                r.InventoryQuantity, PackIconKind.PackageVariant, "#283593", "#E8EAF6"),
            MoneyCard("cashBoxes", "أرصدة الصناديق",
                "مجموع أرصدة القاصات الحالية",
                r.CashBoxesBalance, r.CashBoxesBalanceUsd, PackIconKind.CashRegister, "#00838F", "#E0F7FA"),
            MoneyCard("banks", "أرصدة المصارف",
                "مجموع أرصدة الحسابات المصرفية",
                r.BankBalance, r.BankBalanceUsd, PackIconKind.Bank, "#1565C0", "#E3F2FD"),
            MoneyCard("nwc", "رأس المال العامل",
                "نقد + مصارف + ذمم عملاء + أقساط − ذمم موردين",
                r.NetWorkingCapital, r.NetWorkingCapitalUsd, PackIconKind.SwapHorizontal, "#2E7D32", "#E8F5E9"),
            MoneyCard("totalAssets", "إجمالي الأصول",
                "من الميزانية العمومية حتى تاريخ النهاية",
                r.TotalAssets, 0, PackIconKind.ChartBox, "#2E7D32", "#E8F5E9"),
            MoneyCard("totalLiabilities", "إجمالي الالتزامات",
                "ذمم الموردين + ودائع المستثمرين",
                r.TotalLiabilities, 0, PackIconKind.ScaleBalance, "#C62828", "#FFEBEE"),
            MoneyCard("totalEquity", "حقوق الملكية",
                "رأس المال + التعديلات + الأرباح المتراكمة",
                r.TotalEquity, 0, PackIconKind.AccountBalance, "#1565C0", "#E3F2FD"),
            MoneyCard("accumulatedProfits", "الأرباح المتراكمة",
                "أرباح متراكمة من بداية النشاط حتى تاريخ النهاية",
                r.AccumulatedProfits, 0, PackIconKind.ChartTimelineVariant, "#6A1B9A", "#F3E5F5"),
        ]));

        Sections.Add(MakeSection("المبيعات", "#2E7D32",
        [
            MoneyCard("totalSales", "صافي المبيعات",
                "بيع/أقساط − مرتجعات المبيعات خلال الفترة",
                r.TotalSales, r.TotalSalesUsd, PackIconKind.PointOfSale, "#2E7D32", "#E8F5E9"),
            MoneyCard("cashSales", "مبيعات نقدية (صافي)",
                "مبيعات نقدية − مرتجعات نقدية",
                r.CashSales, r.CashSalesUsd, PackIconKind.Cash, "#00838F", "#E0F7FA"),
            MoneyCard("creditSales", "مبيعات آجل (صافي)",
                "مبيعات آجل − مرتجعات آجل",
                r.CreditSales, r.CreditSalesUsd, PackIconKind.CreditCardOutline, "#AD1457", "#FCE4EC"),
            MoneyCard("installmentSales", "مبيعات أقساط",
                "فواتير بيع بطريقة الدفع أقساط",
                r.InstallmentSales, r.InstallmentSalesUsd, PackIconKind.CalendarMonth, "#EF6C00", "#FFF3E0"),
            CountCard("salesCount", "عدد فواتير البيع",
                "يشمل فواتير البيع/الأقساط والمرتجعات في الفترة",
                r.SalesInvoiceCount, PackIconKind.FileDocumentOutline, "#1565C0", "#E3F2FD"),
            MoneyCard("avgSale", "متوسط قيمة الفاتورة",
                "صافي المبيعات ÷ عدد الفواتير",
                r.AverageSaleInvoice, r.AverageSaleInvoiceUsd, PackIconKind.ChartBar, "#283593", "#E8EAF6"),
        ]));

        Sections.Add(MakeSection("المشتريات والتكاليف", "#EF6C00",
        [
            MoneyCard("totalPurchases", "صافي المشتريات",
                "مشتريات − مرتجعات المشتريات خلال الفترة",
                r.TotalPurchases, r.TotalPurchasesUsd, PackIconKind.CartArrowDown, "#EF6C00", "#FFF3E0"),
            MoneyCard("cashPurchases", "مشتريات نقدية (صافي)",
                "مشتريات نقدية − مرتجعات نقدية",
                r.CashPurchases, r.CashPurchasesUsd, PackIconKind.CashMinus, "#00838F", "#E0F7FA"),
            MoneyCard("creditPurchases", "مشتريات آجل (صافي)",
                "مشتريات آجل − مرتجعات آجل",
                r.CreditPurchases, r.CreditPurchasesUsd, PackIconKind.TruckCheck, "#00695C", "#E0F2F1"),
            CountCard("purchaseCount", "عدد فواتير الشراء",
                "يشمل فواتير الشراء والمرتجعات في الفترة",
                r.PurchaseInvoiceCount, PackIconKind.FileDocumentMultiple, "#1565C0", "#E3F2FD"),
            MoneyCard("cogs", "تكلفة البضاعة المباعة",
                "تكلفة الكميات المباعة (متوسط التكلفة × الكمية)",
                r.CostOfGoodsSold, 0, PackIconKind.PackageDown, "#C62828", "#FFEBEE"),
        ]));

        Sections.Add(MakeSection("الأرباح", "#1565C0",
        [
            MoneyCard("grossProfit", "مجمل الربح",
                "المبيعات − تكلفة البضاعة المباعة",
                r.GrossProfit, 0, PackIconKind.TrendingUp, "#2E7D32", "#E8F5E9"),
            PercentCard("grossMargin", "هامش المجمل %",
                "(مجمل الربح ÷ المبيعات) × 100",
                r.GrossMarginPercent, PackIconKind.Percent, "#00838F", "#E0F7FA"),
            MoneyCard("expenses", "المصروفات",
                "مجموع المصاريف المسجّلة في الفترة",
                r.TotalExpenses, r.TotalExpensesUsd, PackIconKind.CashMinus, "#C62828", "#FFEBEE"),
            MoneyCard("bankFees", "الرسوم البنكية",
                "رسوم سندات القبض البنكي في الفترة",
                r.TotalBankFees, 0, PackIconKind.BankTransfer, "#6A1B9A", "#F3E5F5"),
            MoneyCard("operatingProfit", "الربح التشغيلي",
                "مجمل الربح − المصاريف − الرسوم البنكية",
                r.OperatingProfit, r.OperatingProfitUsd, PackIconKind.ChartAreaspline, "#1565C0", "#E3F2FD"),
            MoneyCard("distributions", "توزيعات الأرباح",
                "مبالغ توزيع الأرباح على المستثمرين",
                r.DistributedProfits, 0, PackIconKind.AccountCash, "#EF6C00", "#FFF3E0"),
            MoneyCard("netProfit", "صافي الربح",
                "التشغيلي − التوزيعات + رصيد افتتاحي للأرباح",
                r.NetProfit, r.NetProfitUsd, PackIconKind.Finance, "#2E7D32", "#E8F5E9"),
            PercentCard("netMargin", "هامش الصافي %",
                "(صافي الربح ÷ المبيعات) × 100",
                r.NetMarginPercent, PackIconKind.ChartDonut, "#283593", "#E8EAF6"),
        ]));

        Sections.Add(MakeSection("العملاء", "#AD1457",
        [
            CountCard("activeCustomers", "عملاء نشطون (الفترة)",
                "عملاء لديهم فواتير بيع ضمن الفترة",
                r.ActiveCustomersCount, PackIconKind.AccountMultiple, "#AD1457", "#FCE4EC"),
            MoneyCard("customerCollections", "المحصّل من العملاء",
                "سندات قبض/دين + أقساط محصّلة في الفترة",
                r.CustomerCollections, r.CustomerCollectionsUsd, PackIconKind.CashPlus, "#2E7D32", "#E8F5E9"),
            CountCard("customersWithBalance", "عملاء برصيد",
                "عملاء رصيدهم المستحق أكبر من صفر",
                r.CustomersWithBalanceCount, PackIconKind.AccountAlert, "#EF6C00", "#FFF3E0"),
            MoneyCard("highestCustomer", "أعلى رصيد عميل",
                "أكبر رصيد مستحق لعميل واحد",
                r.HighestCustomerBalance, r.HighestCustomerBalanceUsd, PackIconKind.AccountStar, "#C62828", "#FFEBEE"),
        ]));

        Sections.Add(MakeSection("الموردين", "#00695C",
        [
            CountCard("activeSuppliers", "موردون نشطون (الفترة)",
                "موردون لديهم فواتير شراء ضمن الفترة",
                r.ActiveSuppliersCount, PackIconKind.Truck, "#00695C", "#E0F2F1"),
            MoneyCard("supplierPayments", "المدفوع للموردين",
                "سندات صرف + مشتريات نقدية في الفترة",
                r.SupplierPayments, r.SupplierPaymentsUsd, PackIconKind.CashRefund, "#EF6C00", "#FFF3E0"),
            CountCard("suppliersWithBalance", "موردون برصيد",
                "موردون رصيدهم الآجل أكبر من صفر",
                r.SuppliersWithBalanceCount, PackIconKind.TruckAlert, "#C62828", "#FFEBEE"),
            MoneyCard("highestSupplier", "أعلى رصيد مورد",
                "أكبر رصيد آجل لمورد واحد",
                r.HighestSupplierBalance, r.HighestSupplierBalanceUsd, PackIconKind.TruckCheckOutline, "#1565C0", "#E3F2FD"),
        ]));

        Sections.Add(MakeSection("المخزون", "#283593",
        [
            CountCard("stockedProducts", "منتجات ذات رصيد",
                "عدد المنتجات التي كميتها أكبر من صفر",
                r.StockedProductCount, PackIconKind.Barcode, "#283593", "#E8EAF6"),
            MoneyCard("inventorySale", "قيمة المخزون (بيع)",
                "كمية المخزن × سعر البيع",
                r.InventorySaleValue, 0, PackIconKind.Tag, "#1565C0", "#E3F2FD"),
            MoneyCard("inventoryPotential", "ربح محتمل بالمخزن",
                "قيمة البيع − قيمة التكلفة للمخزون الحالي",
                r.InventoryPotentialProfit, 0, PackIconKind.ChartLineVariant, "#2E7D32", "#E8F5E9"),
            CountCard("belowMin", "تحت الحد الأدنى",
                "أصناف كميتها أقل من الحد الأدنى للمخزن",
                r.BelowMinimumStockCount, PackIconKind.AlertCircle, "#C62828", "#FFEBEE"),
        ]));

        Sections.Add(MakeSection("الأقساط والنقد", "#C62828",
        [
            CountCard("overdueCount", "أقساط متأخرة (عدد)",
                "أقساط مستحقة وغير مسددة حتى تاريخ النهاية",
                r.OverdueInstallmentsCount, PackIconKind.AlarmLight, "#C62828", "#FFEBEE"),
            MoneyCard("overdueAmount", "أقساط متأخرة (مبلغ)",
                "مجموع المتبقي للأقساط المتأخرة",
                r.OverdueInstallmentsAmount, r.OverdueInstallmentsAmountUsd, PackIconKind.CashRemove, "#AD1457", "#FCE4EC"),
            MoneyCard("collectedInstallments", "أقساط محصّلة",
                "مبالغ الأقساط المسددة خلال الفترة",
                r.CollectedInstallments, r.CollectedInstallmentsUsd, PackIconKind.CashCheck, "#2E7D32", "#E8F5E9"),
            MoneyCard("receipts", "سندات قبض",
                "مجموع سندات القبض والدين في الفترة",
                r.ReceiptVouchersAmount, r.ReceiptVouchersAmountUsd, PackIconKind.Receipt, "#00838F", "#E0F7FA"),
            MoneyCard("payments", "سندات صرف",
                "مجموع سندات الصرف في الفترة",
                r.PaymentVouchersAmount, r.PaymentVouchersAmountUsd, PackIconKind.NoteMinus, "#EF6C00", "#FFF3E0"),
            MoneyCard("transfersAmount", "تحويلات (مبلغ)",
                "إجمالي مبالغ التحويلات بين الحسابات",
                r.TransfersAmount, r.TransfersAmountUsd, PackIconKind.SwapHorizontal, "#1565C0", "#E3F2FD"),
            CountCard("transfersCount", "عدد التحويلات",
                "عدد عمليات التحويل في الفترة",
                r.TransfersCount, PackIconKind.Transfer, "#283593", "#E8EAF6"),
        ]));
    }

    private AmountBreakdownModel? BuildBreakdown(string key, ExecutiveBusinessSummaryResult r)
    {
        var period = PeriodSubtitle(r);
        var model = key switch
        {
            "customerCredit" => new AmountBreakdownModel
            {
                Title = "تفاصيل رصيد الآجل للعملاء",
                Subtitle = period,
                Formula = "الرصيد = متبقي فواتير الآجل − سندات دين غير مطبّقة − سندات قبض غير مطبّقة (لكل عملة على حدة)",
                ResultLabel = "رصيد الآجل للعملاء",
                ResultAmount = r.CustomerReceivables,
                Lines =
                [
                    Line("+", "متبقي فواتير الآجل", r.CustomerCreditInvoiceRemaining, r.CustomerCreditInvoiceRemainingUsd, "فواتير بيع/أقساط آجلة غير مسددة"),
                    Line("−", "سندات دين غير مطبّقة", r.CustomerUnappliedDebt, r.CustomerUnappliedDebtUsd),
                    Line("−", "سندات قبض غير مطبّقة", r.CustomerUnappliedReceipts, r.CustomerUnappliedReceiptsUsd, "غير مرتبطة بفاتورة أو قسط"),
                    Line("=", "رصيد الآجل للعملاء", r.CustomerReceivables, r.CustomerReceivablesUsd, isResult: true)
                ]
            },
            "supplierCredit" => new AmountBreakdownModel
            {
                Title = "تفاصيل رصيد الآجل للموردين",
                Subtitle = period,
                Formula = "الرصيد = متبقي مشتريات الآجل − سندات صرف غير مطبّقة (لكل عملة على حدة)",
                ResultLabel = "رصيد الآجل للموردين",
                ResultAmount = r.SupplierPayables,
                Lines =
                [
                    Line("+", "متبقي مشتريات الآجل", r.SupplierCreditInvoiceRemaining, r.SupplierCreditInvoiceRemainingUsd),
                    Line("−", "سندات صرف غير مطبّقة", r.SupplierUnappliedPayments, r.SupplierUnappliedPaymentsUsd, "غير مرتبطة بفاتورة شراء"),
                    Line("=", "رصيد الآجل للموردين", r.SupplierPayables, r.SupplierPayablesUsd, isResult: true)
                ]
            },
            "installmentAr" => Single("ذمم الأقساط", "مجموع RemainingAmount للأقساط غير المسددة", r.InstallmentReceivables, r.InstallmentReceivablesUsd, period),
            "inventoryCost" => new AmountBreakdownModel
            {
                Title = "تفاصيل قيمة المخزون بالتكلفة",
                Subtitle = period,
                Formula = "القيمة = Σ (الكمية × متوسط تكلفة الشراء)",
                ResultLabel = "قيمة المخزون (تكلفة)",
                ResultAmount = r.InventoryCostValue,
                Lines =
                [
                    Line("Σ", "قيمة المخزون بالتكلفة", r.InventoryCostValue, isResult: true)
                ],
                Note = $"إجمالي الكمية: {r.InventoryQuantity:N0}"
            },
            "inventoryQty" => Single("كمية المخزون", "مجموع كميات WarehouseStocks > 0", r.InventoryQuantity, 0, period, isMoney: false),
            "cashBoxes" => Single("أرصدة الصناديق", "مجموع أرصدة القاصات حسب العملة", r.CashBoxesBalance, r.CashBoxesBalanceUsd, period),
            "banks" => Single("أرصدة المصارف", "مجموع أرصدة الحسابات المصرفية حسب العملة", r.BankBalance, r.BankBalanceUsd, period),
            "nwc" => new AmountBreakdownModel
            {
                Title = "تفاصيل رأس المال العامل",
                Subtitle = period,
                Formula = "صناديق + مصارف + آجل عملاء + أقساط − آجل موردين (دينار ودولار منفصلان)",
                ResultLabel = "رأس المال العامل",
                ResultAmount = r.NetWorkingCapital,
                Lines =
                [
                    Line("+", "الصناديق", r.CashBoxesBalance, r.CashBoxesBalanceUsd),
                    Line("+", "المصارف", r.BankBalance, r.BankBalanceUsd),
                    Line("+", "آجل العملاء", r.CustomerReceivables, r.CustomerReceivablesUsd),
                    Line("+", "ذمم الأقساط", r.InstallmentReceivables, r.InstallmentReceivablesUsd),
                    Line("−", "آجل الموردين", r.SupplierPayables, r.SupplierPayablesUsd),
                    Line("=", "رأس المال العامل", r.NetWorkingCapital, r.NetWorkingCapitalUsd, isResult: true)
                ]
            },
            "totalAssets" => Single("إجمالي الأصول", "من الميزانية العمومية", r.TotalAssets, 0, period),
            "totalLiabilities" => Single("إجمالي الالتزامات", "من الميزانية العمومية", r.TotalLiabilities, 0, period),
            "totalEquity" => Single("حقوق الملكية", "من الميزانية العمومية", r.TotalEquity, 0, period),
            "accumulatedProfits" => Single("الأرباح المتراكمة", "من الميزانية العمومية حتى تاريخ النهاية", r.AccumulatedProfits, 0, period),
            "totalSales" => new AmountBreakdownModel
            {
                Title = "تفاصيل صافي المبيعات",
                Subtitle = period,
                Formula = "نقدي + آجل + أقساط − مرتجعات المبيعات (لكل عملة)",
                ResultLabel = "صافي المبيعات",
                ResultAmount = r.TotalSales,
                Lines =
                [
                    Line("+", "مبيعات نقدية (صافي)", r.CashSales, r.CashSalesUsd),
                    Line("+", "مبيعات آجل (صافي)", r.CreditSales, r.CreditSalesUsd),
                    Line("+", "مبيعات أقساط", r.InstallmentSales, r.InstallmentSalesUsd),
                    Line("=", "صافي المبيعات", r.TotalSales, r.TotalSalesUsd, isResult: true)
                ]
            },
            "cashSales" => Single("مبيعات نقدية (صافي)", "مبيعات نقدية − مرتجعات نقدية", r.CashSales, r.CashSalesUsd, period),
            "creditSales" => Single("مبيعات آجل (صافي)", "مبيعات آجل − مرتجعات آجل", r.CreditSales, r.CreditSalesUsd, period),
            "installmentSales" => Single("مبيعات أقساط", "فواتير بيع أقساط في الفترة", r.InstallmentSales, r.InstallmentSalesUsd, period),
            "salesCount" => Single("عدد فواتير البيع", "يشمل البيع/الأقساط والمرتجعات", r.SalesInvoiceCount, 0, period, isMoney: false),
            "avgSale" => new AmountBreakdownModel
            {
                Title = "تفاصيل متوسط قيمة الفاتورة",
                Subtitle = period,
                Formula = "المتوسط = صافي المبيعات ÷ عدد الفواتير (لكل عملة)",
                ResultLabel = "متوسط قيمة الفاتورة",
                ResultAmount = r.AverageSaleInvoice,
                Lines =
                [
                    Line("+", "صافي المبيعات", r.TotalSales, r.TotalSalesUsd),
                    Line("÷", "عدد الفواتير", r.SalesInvoiceCount, isMoney: false),
                    Line("=", "المتوسط", r.AverageSaleInvoice, r.AverageSaleInvoiceUsd, isResult: true)
                ]
            },
            "totalPurchases" => new AmountBreakdownModel
            {
                Title = "تفاصيل صافي المشتريات",
                Subtitle = period,
                Formula = "نقدي + آجل − مرتجعات المشتريات (لكل عملة)",
                ResultLabel = "صافي المشتريات",
                ResultAmount = r.TotalPurchases,
                Lines =
                [
                    Line("+", "مشتريات نقدية (صافي)", r.CashPurchases, r.CashPurchasesUsd),
                    Line("+", "مشتريات آجل (صافي)", r.CreditPurchases, r.CreditPurchasesUsd),
                    Line("=", "صافي المشتريات", r.TotalPurchases, r.TotalPurchasesUsd, isResult: true)
                ]
            },
            "cashPurchases" => Single("مشتريات نقدية (صافي)", "مشتريات نقدية − مرتجعات نقدية", r.CashPurchases, r.CashPurchasesUsd, period),
            "creditPurchases" => Single("مشتريات آجل (صافي)", "مشتريات آجل − مرتجعات آجل", r.CreditPurchases, r.CreditPurchasesUsd, period),
            "purchaseCount" => Single("عدد فواتير الشراء", "يشمل الشراء والمرتجعات", r.PurchaseInvoiceCount, 0, period, isMoney: false),
            "cogs" => Single("تكلفة البضاعة المباعة", "متوسط التكلفة × الكميات المباعة", r.CostOfGoodsSold, 0, period),
            "grossProfit" => new AmountBreakdownModel
            {
                Title = "تفاصيل مجمل الربح",
                Subtitle = period,
                Formula = "مجمل الربح = المبيعات − تكلفة البضاعة",
                ResultLabel = "مجمل الربح",
                ResultAmount = r.GrossProfit,
                Lines =
                [
                    Line("+", "المبيعات", r.TotalSales),
                    Line("−", "تكلفة البضاعة المباعة", r.CostOfGoodsSold),
                    Line("=", "مجمل الربح", r.GrossProfit, isResult: true)
                ],
                Note = ShowMultiCurrency
                    ? $"إفصاح دولار منفصل: مبيعات $ {r.TotalSalesUsd:N2} (التكلفة تُحسب بالدينار عادةً)."
                    : null
            },
            "grossMargin" => new AmountBreakdownModel
            {
                Title = "تفاصيل هامش المجمل",
                Subtitle = period,
                Formula = "الهامش % = (مجمل الربح ÷ المبيعات) × 100",
                ResultLabel = "هامش المجمل %",
                ResultAmount = r.GrossMarginPercent,
                Lines =
                [
                    Line("+", "مجمل الربح", r.GrossProfit),
                    Line("÷", "المبيعات", r.TotalSales),
                    Line("=", "الهامش %", r.GrossMarginPercent, isResult: true)
                ],
                Note = "القيمة النهائية معروضة كنسبة مئوية."
            },
            "expenses" => Single("المصروفات", "مجموع مصاريف الفترة حسب العملة", r.TotalExpenses, r.TotalExpensesUsd, period),
            "bankFees" => Single("الرسوم البنكية", "BankFees من سندات القبض البنكي", r.TotalBankFees, 0, period),
            "operatingProfit" => new AmountBreakdownModel
            {
                Title = "تفاصيل الربح التشغيلي",
                Subtitle = period,
                Formula = "تشغيلي = مجمل − مصاريف − رسوم بنكية · دولار: مبيعات $ − مصروفات $",
                ResultLabel = "الربح التشغيلي",
                ResultAmount = r.OperatingProfit,
                Lines =
                [
                    Line("+", "مجمل الربح", r.GrossProfit),
                    Line("−", "المصروفات", r.TotalExpenses, r.TotalExpensesUsd),
                    Line("−", "الرسوم البنكية", r.TotalBankFees),
                    Line("=", "الربح التشغيلي", r.OperatingProfit, r.OperatingProfitUsd, isResult: true)
                ],
                Note = ShowMultiCurrency
                    ? $"دولار: مبيعات {AccountingCurrencyHelper.Format(r.TotalSalesUsd, AccountingCurrency.USD)} − مصروفات {AccountingCurrencyHelper.Format(r.TotalExpensesUsd, AccountingCurrency.USD)} = {AccountingCurrencyHelper.Format(r.OperatingProfitUsd, AccountingCurrency.USD)}"
                    : null
            },
            "distributions" => Single("توزيعات الأرباح", "مجموع توزيعات الفترة", r.DistributedProfits, 0, period),
            "netProfit" => new AmountBreakdownModel
            {
                Title = "تفاصيل صافي الربح",
                Subtitle = period,
                Formula = "الصافي = مجمل − مصاريف − رسوم − توزيعات + افتتاحي أرباح · دولار: مبيعات $ − مصروفات $",
                ResultLabel = "صافي الربح",
                ResultAmount = r.NetProfit,
                Lines =
                [
                    Line("+", "مجمل الربح", r.GrossProfit),
                    Line("−", "المصروفات", r.TotalExpenses, r.TotalExpensesUsd),
                    Line("−", "الرسوم البنكية", r.TotalBankFees),
                    Line("−", "توزيعات الأرباح", r.DistributedProfits),
                    Line("+", "رصيد افتتاحي للأرباح", r.ProfitOpeningBalance),
                    Line("=", "صافي الربح", r.NetProfit, r.NetProfitUsd, isResult: true)
                ],
                Note = ShowMultiCurrency
                    ? $"إفصاح دولار: {AccountingCurrencyHelper.Format(r.NetProfitUsd, AccountingCurrency.USD)} (مبيعات − مصروفات دون خلط تكلفة الدينار)."
                    : null
            },
            "netMargin" => new AmountBreakdownModel
            {
                Title = "تفاصيل هامش الصافي",
                Subtitle = period,
                Formula = "الهامش % = (صافي الربح ÷ المبيعات) × 100",
                ResultLabel = "هامش الصافي %",
                ResultAmount = r.NetMarginPercent,
                Lines =
                [
                    Line("+", "صافي الربح", r.NetProfit),
                    Line("÷", "المبيعات", r.TotalSales),
                    Line("=", "الهامش %", r.NetMarginPercent, isResult: true)
                ]
            },
            "activeCustomers" => Single("عملاء نشطون", "Distinct CustomerId على فواتير البيع (دينار + دولار)", r.ActiveCustomersCount, 0, period, isMoney: false),
            "customerCollections" => new AmountBreakdownModel
            {
                Title = "تفاصيل المحصّل من العملاء",
                Subtitle = period,
                Formula = "سندات قبض/دين + أقساط محصّلة (لكل عملة)",
                ResultLabel = "المحصّل",
                ResultAmount = r.CustomerCollections,
                Lines =
                [
                    Line("+", "سندات قبض/دين", r.ReceiptVouchersAmount, r.ReceiptVouchersAmountUsd),
                    Line("+", "أقساط محصّلة", r.CollectedInstallments, r.CollectedInstallmentsUsd),
                    Line("=", "المحصّل من العملاء", r.CustomerCollections, r.CustomerCollectionsUsd, isResult: true)
                ]
            },
            "customersWithBalance" => Single("عملاء برصيد", "بعد خصم السندات غير المطبّقة", r.CustomersWithBalanceCount, 0, period, isMoney: false),
            "highestCustomer" => Single("أعلى رصيد عميل", "أقصى رصيد مستحق لعميل (دينار ودولار منفصلان)", r.HighestCustomerBalance, r.HighestCustomerBalanceUsd, period),
            "activeSuppliers" => Single("موردون نشطون", "Distinct SupplierId على فواتير الشراء (دينار + دولار)", r.ActiveSuppliersCount, 0, period, isMoney: false),
            "supplierPayments" => new AmountBreakdownModel
            {
                Title = "تفاصيل المدفوع للموردين",
                Subtitle = period,
                Formula = "سندات صرف + مشتريات نقدية (لكل عملة)",
                ResultLabel = "المدفوع",
                ResultAmount = r.SupplierPayments,
                Lines =
                [
                    Line("+", "سندات صرف", r.PaymentVouchersAmount, r.PaymentVouchersAmountUsd),
                    Line("+", "مشتريات نقدية", r.CashPurchases, r.CashPurchasesUsd),
                    Line("=", "المدفوع للموردين", r.SupplierPayments, r.SupplierPaymentsUsd, isResult: true)
                ]
            },
            "suppliersWithBalance" => Single("موردون برصيد", "بعد خصم سندات الصرف غير المطبّقة", r.SuppliersWithBalanceCount, 0, period, isMoney: false),
            "highestSupplier" => Single("أعلى رصيد مورد", "أقصى رصيد آجل لمورد (دينار ودولار منفصلان)", r.HighestSupplierBalance, r.HighestSupplierBalanceUsd, period),
            "stockedProducts" => Single("منتجات ذات رصيد", "منتجات بكمية > 0", r.StockedProductCount, 0, period, isMoney: false),
            "inventorySale" => Single("قيمة المخزون بسعر البيع", "كمية × سعر البيع", r.InventorySaleValue, 0, period),
            "inventoryPotential" => new AmountBreakdownModel
            {
                Title = "تفاصيل الربح المحتمل بالمخزن",
                Subtitle = period,
                Formula = "الربح المحتمل = قيمة البيع − قيمة التكلفة",
                ResultLabel = "ربح محتمل",
                ResultAmount = r.InventoryPotentialProfit,
                Lines =
                [
                    Line("+", "قيمة المخزون (بيع)", r.InventorySaleValue),
                    Line("−", "قيمة المخزون (تكلفة)", r.InventoryCostValue),
                    Line("=", "ربح محتمل بالمخزن", r.InventoryPotentialProfit, isResult: true)
                ]
            },
            "belowMin" => Single("تحت الحد الأدنى", "Quantity < MinQuantity", r.BelowMinimumStockCount, 0, period, isMoney: false),
            "overdueCount" => Single("أقساط متأخرة (عدد)", $"DueDate قبل تاريخ النهاية ومتبقي > 0 · دولار: {r.OverdueInstallmentsCountUsd}", r.OverdueInstallmentsCount, 0, period, isMoney: false),
            "overdueAmount" => Single("أقساط متأخرة (مبلغ)", "مجموع المتبقي للأقساط المتأخرة حسب العملة", r.OverdueInstallmentsAmount, r.OverdueInstallmentsAmountUsd, period),
            "collectedInstallments" => Single("أقساط محصّلة", "PaidAmount ضمن الفترة حسب العملة", r.CollectedInstallments, r.CollectedInstallmentsUsd, period),
            "receipts" => Single("سندات قبض", "Receipt + DebtReceipt في الفترة حسب العملة", r.ReceiptVouchersAmount, r.ReceiptVouchersAmountUsd, period),
            "payments" => Single("سندات صرف", "Payment في الفترة حسب العملة", r.PaymentVouchersAmount, r.PaymentVouchersAmountUsd, period),
            "transfersAmount" => Single("تحويلات (مبلغ)", "مجموع مبالغ التحويلات حسب العملة", r.TransfersAmount, r.TransfersAmountUsd, period),
            "transfersCount" => Single("عدد التحويلات", "عدد سجلات التحويل", r.TransfersCount, 0, period, isMoney: false),
            _ => null
        };

        return AttachEntityDetails(key, model, r);
    }

    private static AmountBreakdownModel? AttachEntityDetails(
        string key, AmountBreakdownModel? model, ExecutiveBusinessSummaryResult r)
    {
        if (model is null) return null;

        var (title, points, showAmount) = key switch
        {
            "customerCredit" or "highestCustomer" => ("العملاء حسب متبقي الآجل", r.CustomerCreditDetail, true),
            "supplierCredit" or "highestSupplier" => ("الموردون حسب متبقي الآجل", r.SupplierCreditDetail, true),
            "activeCustomers" or "totalSales" or "cashSales" or "creditSales" or "installmentSales"
                => ("العملاء النشطون (مبيعات الفترة)", r.ActiveCustomersDetail, true),
            "customersWithBalance" or "customerCollections"
                => ("العملاء ذوو الرصيد", r.CustomersWithBalanceDetail, true),
            "activeSuppliers" or "totalPurchases" or "cashPurchases" or "creditPurchases"
                => ("الموردون النشطون (مشتريات الفترة)", r.ActiveSuppliersDetail, true),
            "suppliersWithBalance" or "supplierPayments"
                => ("الموردون ذوو الرصيد", r.SuppliersWithBalanceDetail, true),
            "cashBoxes" => ("تفاصيل القاصات", r.CashBoxesDetail, true),
            "banks" => ("تفاصيل المصارف", r.BanksDetail, true),
            "inventoryCost" or "inventorySale" or "inventoryPotential" or "stockedProducts" or "inventoryQty"
                => ("أعلى المنتجات قيمة بالمخزن", r.TopStockByValueDetail, true),
            "belowMin" => ("الأصناف تحت الحد الأدنى (الكمية الحالية)", r.BelowMinimumStockDetail, true),
            "overdueCount" or "overdueAmount" or "installmentAr"
                => ("العملاء ذوو الأقساط المتأخرة", r.OverdueInstallmentsDetail, true),
            _ => ((string?)null, (List<NameAmountPoint>?)null, true)
        };

        if (title is null || points is null || points.Count == 0)
            return model;

        return new AmountBreakdownModel
        {
            Title = model.Title,
            Subtitle = model.Subtitle,
            Formula = model.Formula,
            ResultLabel = model.ResultLabel,
            ResultAmount = model.ResultAmount,
            Note = model.Note,
            Lines = model.Lines,
            EntitiesTitle = $"{title} — {points.Count}",
            Entities = points.Select(p => new AmountBreakdownEntityRow
            {
                Name = p.Name,
                Amount = p.Amount,
                AmountUsd = p.AmountUsd,
                ShowAmount = showAmount
            }).ToList()
        };
    }

    private AmountBreakdownModel AttachUsdResult(
        string key, AmountBreakdownModel model, ExecutiveBusinessSummaryResult r)
    {
        if (!ShowMultiCurrency) return model;
        var usd = ResolveUsdAmount(key, r);
        if (usd is null) return model;

        return new AmountBreakdownModel
        {
            Title = model.Title,
            Subtitle = model.Subtitle,
            Formula = model.Formula,
            ResultLabel = model.ResultLabel,
            ResultAmount = model.ResultAmount,
            ResultCurrency = model.ResultCurrency,
            ResultAmountUsd = usd,
            ResultLabelUsd = "بالدولار $",
            Note = model.Note,
            Lines = model.Lines,
            EntitiesTitle = model.EntitiesTitle,
            Entities = model.Entities
        };
    }

    private static decimal? ResolveUsdAmount(string key, ExecutiveBusinessSummaryResult r) => key switch
    {
        "customerCredit" => r.CustomerReceivablesUsd,
        "installmentAr" => r.InstallmentReceivablesUsd,
        "supplierCredit" => r.SupplierPayablesUsd,
        "cashBoxes" => r.CashBoxesBalanceUsd,
        "banks" => r.BankBalanceUsd,
        "nwc" => r.NetWorkingCapitalUsd,
        "totalSales" => r.TotalSalesUsd,
        "cashSales" => r.CashSalesUsd,
        "creditSales" => r.CreditSalesUsd,
        "installmentSales" => r.InstallmentSalesUsd,
        "avgSale" => r.AverageSaleInvoiceUsd,
        "totalPurchases" => r.TotalPurchasesUsd,
        "cashPurchases" => r.CashPurchasesUsd,
        "creditPurchases" => r.CreditPurchasesUsd,
        "expenses" => r.TotalExpensesUsd,
        "operatingProfit" => r.OperatingProfitUsd,
        "netProfit" => r.NetProfitUsd,
        "customerCollections" => r.CustomerCollectionsUsd,
        "highestCustomer" => r.HighestCustomerBalanceUsd,
        "supplierPayments" => r.SupplierPaymentsUsd,
        "highestSupplier" => r.HighestSupplierBalanceUsd,
        "overdueAmount" => r.OverdueInstallmentsAmountUsd,
        "collectedInstallments" => r.CollectedInstallmentsUsd,
        "receipts" => r.ReceiptVouchersAmountUsd,
        "payments" => r.PaymentVouchersAmountUsd,
        "transfersAmount" => r.TransfersAmountUsd,
        _ => null
    };

    private AmountBreakdownModel Single(
        string title, string formula, decimal amount, decimal amountUsd, string period, bool isMoney = true)
        => new()
        {
            Title = $"تفاصيل {title}",
            Subtitle = period,
            Formula = formula,
            ResultLabel = title,
            ResultAmount = amount,
            Lines =
            [
                Line("=", title, amount, amountUsd, isResult: true, isMoney: isMoney)
            ]
        };

    private AmountBreakdownLine Line(
        string op, string label, decimal amount, decimal amountUsd = 0,
        string? description = null, bool isResult = false, bool isMoney = true)
    {
        _ = isMoney;
        return new AmountBreakdownLine
        {
            Operator = op,
            Label = label,
            Amount = amount,
            AmountUsd = ShowMultiCurrency ? amountUsd : 0,
            Description = description,
            IsResult = isResult
        };
    }

    private static string PeriodSubtitle(ExecutiveBusinessSummaryResult r)
        => $"حتى {(r.DateTo ?? DateTime.Today):yyyy/MM/dd} · من {(r.DateFrom ?? DateTime.Today.AddMonths(-1)):yyyy/MM/dd}";

    private static ExecutiveSummarySectionItem MakeSection(
        string title, string accent, IEnumerable<ExecutiveSummaryCardItem> cards)
        => new()
        {
            Title = title,
            AccentBrush = BrushFrom(accent),
            Cards = new ObservableCollection<ExecutiveSummaryCardItem>(cards)
        };

    private ExecutiveSummaryCardItem MoneyCard(
        string key, string title, string hint, decimal amount, decimal amountUsd,
        PackIconKind icon, string accent, string light)
    {
        var showUsd = ShowMultiCurrency;
        return Card(
            key, title, hint, amount, amountUsd, "د.ع", FormatCurrency(amount), icon, accent, light,
            showUsd ? AccountingCurrencyHelper.Format(amountUsd, AccountingCurrency.USD) : null,
            showUsd);
    }

    private static ExecutiveSummaryCardItem CountCard(
        string key, string title, string hint, int amount, PackIconKind icon, string accent, string light)
        => Card(key, title, hint, amount, 0, null, amount.ToString("N0"), icon, accent, light);

    private static ExecutiveSummaryCardItem QtyCard(
        string key, string title, string hint, decimal amount, PackIconKind icon, string accent, string light)
        => Card(key, title, hint, amount, 0, null, amount.ToString("N0"), icon, accent, light);

    private static ExecutiveSummaryCardItem PercentCard(
        string key, string title, string hint, decimal amount, PackIconKind icon, string accent, string light)
        => Card(key, title, hint, amount, 0, "%", $"{amount:N1} %", icon, accent, light);

    private static ExecutiveSummaryCardItem Card(
        string key, string title, string hint, decimal amount, decimal amountUsd, string? suffix, string display,
        PackIconKind icon, string accent, string light,
        string? secondaryValue = null, bool showSecondary = false)
    {
        var accentBrush = BrushFrom(accent);
        return new ExecutiveSummaryCardItem
        {
            MetricKey = key,
            Title = title,
            Hint = hint,
            Amount = amount,
            AmountUsd = amountUsd,
            Suffix = suffix,
            Value = display,
            SecondaryValue = secondaryValue,
            ShowSecondaryValue = showSecondary,
            Icon = icon,
            AccentBrush = accentBrush,
            AccentLightBrush = BrushFrom(light),
            ValueBrush = accentBrush
        };
    }

    private static Brush BrushFrom(string hex)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        if (brush.CanFreeze) brush.Freeze();
        return brush;
    }
}
