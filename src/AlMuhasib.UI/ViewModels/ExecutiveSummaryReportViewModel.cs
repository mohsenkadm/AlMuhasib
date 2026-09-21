using System.Collections.ObjectModel;
using System.Windows.Media;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
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
    public string? Suffix { get; init; }
    public string Value { get; init; } = string.Empty;
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
    private ExecutiveBusinessSummaryResult? _lastResult;

    public ObservableCollection<ExecutiveSummarySectionItem> Sections { get; } = [];

    [ObservableProperty] private string _periodHint = string.Empty;
    [ObservableProperty] private int _cardCount;

    public ExecutiveSummaryReportViewModel(
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "الملخص التنفيذي للأعمال";
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

        var cols = new[] { "القسم", "المؤشر", "القيمة" };
        var rows = Sections
            .SelectMany(s => s.Cards.Select(c => new object[] { s.Title, c.Title, c.Value }))
            .ToList();
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

        var cols = new[] { "القسم", "المؤشر", "القيمة" };
        var rows = Sections
            .SelectMany(s => s.Cards.Select(c => new object[] { s.Title, c.Title, c.Value }))
            .ToList();
        _exportService.PrintTable("الملخص التنفيذي للأعمال", cols, rows);
    }

    private void BuildSections(ExecutiveBusinessSummaryResult r)
    {
        Sections.Clear();

        Sections.Add(MakeSection("المركز المالي", "#00695C",
        [
            MoneyCard("customerCredit", "رصيد الآجل للعملاء",
                "متبقي فواتير الآجل بعد خصم سندات القبض/الدين غير المطبّقة",
                r.CustomerReceivables, PackIconKind.AccountCash, "#AD1457", "#FCE4EC"),
            MoneyCard("installmentAr", "ذمم الأقساط",
                "مجموع المتبقي من أقساط غير مسددة بالكامل",
                r.InstallmentReceivables, PackIconKind.CalendarClock, "#EF6C00", "#FFF3E0"),
            MoneyCard("supplierCredit", "رصيد الآجل للموردين",
                "متبقي مشتريات الآجل بعد خصم سندات الصرف غير المطبّقة",
                r.SupplierPayables, PackIconKind.TruckDelivery, "#00695C", "#E0F2F1"),
            MoneyCard("inventoryCost", "قيمة المخزون (تكلفة)",
                "كمية المخزن × متوسط تكلفة الشراء لكل منتج",
                r.InventoryCostValue, PackIconKind.Warehouse, "#1565C0", "#E3F2FD"),
            QtyCard("inventoryQty", "كمية المخزون",
                "إجمالي الكميات المتوفرة في كل المخازن",
                r.InventoryQuantity, PackIconKind.PackageVariant, "#283593", "#E8EAF6"),
            MoneyCard("cashBoxes", "أرصدة الصناديق",
                "مجموع أرصدة القاصات الحالية",
                r.CashBoxesBalance, PackIconKind.CashRegister, "#00838F", "#E0F7FA"),
            MoneyCard("banks", "أرصدة المصارف",
                "مجموع أرصدة الحسابات المصرفية",
                r.BankBalance, PackIconKind.Bank, "#1565C0", "#E3F2FD"),
            MoneyCard("nwc", "رأس المال العامل",
                "نقد + مصارف + ذمم عملاء + أقساط − ذمم موردين",
                r.NetWorkingCapital, PackIconKind.SwapHorizontal, "#2E7D32", "#E8F5E9"),
            MoneyCard("totalAssets", "إجمالي الأصول",
                "من الميزانية العمومية حتى تاريخ النهاية",
                r.TotalAssets, PackIconKind.ChartBox, "#2E7D32", "#E8F5E9"),
            MoneyCard("totalLiabilities", "إجمالي الالتزامات",
                "ذمم الموردين + ودائع المستثمرين",
                r.TotalLiabilities, PackIconKind.ScaleBalance, "#C62828", "#FFEBEE"),
            MoneyCard("totalEquity", "حقوق الملكية",
                "رأس المال + التعديلات + الأرباح المتراكمة",
                r.TotalEquity, PackIconKind.AccountBalance, "#1565C0", "#E3F2FD"),
            MoneyCard("accumulatedProfits", "الأرباح المتراكمة",
                "أرباح متراكمة من بداية النشاط حتى تاريخ النهاية",
                r.AccumulatedProfits, PackIconKind.ChartTimelineVariant, "#6A1B9A", "#F3E5F5"),
        ]));

        Sections.Add(MakeSection("المبيعات", "#2E7D32",
        [
            MoneyCard("totalSales", "إجمالي المبيعات",
                "صافي فواتير البيع والأقساط خلال الفترة",
                r.TotalSales, PackIconKind.PointOfSale, "#2E7D32", "#E8F5E9"),
            MoneyCard("cashSales", "مبيعات نقدية",
                "فواتير بيع بطريقة الدفع نقدي",
                r.CashSales, PackIconKind.Cash, "#00838F", "#E0F7FA"),
            MoneyCard("creditSales", "مبيعات آجل",
                "فواتير بيع بطريقة الدفع آجل",
                r.CreditSales, PackIconKind.CreditCardOutline, "#AD1457", "#FCE4EC"),
            MoneyCard("installmentSales", "مبيعات أقساط",
                "فواتير بيع بطريقة الدفع أقساط",
                r.InstallmentSales, PackIconKind.CalendarMonth, "#EF6C00", "#FFF3E0"),
            CountCard("salesCount", "عدد فواتير البيع",
                "عدد فواتير البيع/الأقساط في الفترة",
                r.SalesInvoiceCount, PackIconKind.FileDocumentOutline, "#1565C0", "#E3F2FD"),
            MoneyCard("avgSale", "متوسط قيمة الفاتورة",
                "إجمالي المبيعات ÷ عدد فواتير البيع",
                r.AverageSaleInvoice, PackIconKind.ChartBar, "#283593", "#E8EAF6"),
        ]));

        Sections.Add(MakeSection("المشتريات والتكاليف", "#EF6C00",
        [
            MoneyCard("totalPurchases", "إجمالي المشتريات",
                "صافي فواتير الشراء خلال الفترة",
                r.TotalPurchases, PackIconKind.CartArrowDown, "#EF6C00", "#FFF3E0"),
            MoneyCard("cashPurchases", "مشتريات نقدية",
                "فواتير شراء نقدية",
                r.CashPurchases, PackIconKind.CashMinus, "#00838F", "#E0F7FA"),
            MoneyCard("creditPurchases", "مشتريات آجل",
                "فواتير شراء آجلة",
                r.CreditPurchases, PackIconKind.TruckCheck, "#00695C", "#E0F2F1"),
            CountCard("purchaseCount", "عدد فواتير الشراء",
                "عدد فواتير الشراء في الفترة",
                r.PurchaseInvoiceCount, PackIconKind.FileDocumentMultiple, "#1565C0", "#E3F2FD"),
            MoneyCard("cogs", "تكلفة البضاعة المباعة",
                "تكلفة الكميات المباعة (متوسط التكلفة × الكمية)",
                r.CostOfGoodsSold, PackIconKind.PackageDown, "#C62828", "#FFEBEE"),
        ]));

        Sections.Add(MakeSection("الأرباح", "#1565C0",
        [
            MoneyCard("grossProfit", "مجمل الربح",
                "المبيعات − تكلفة البضاعة المباعة",
                r.GrossProfit, PackIconKind.TrendingUp, "#2E7D32", "#E8F5E9"),
            PercentCard("grossMargin", "هامش المجمل %",
                "(مجمل الربح ÷ المبيعات) × 100",
                r.GrossMarginPercent, PackIconKind.Percent, "#00838F", "#E0F7FA"),
            MoneyCard("expenses", "المصروفات",
                "مجموع المصاريف المسجّلة في الفترة",
                r.TotalExpenses, PackIconKind.CashMinus, "#C62828", "#FFEBEE"),
            MoneyCard("bankFees", "الرسوم البنكية",
                "رسوم سندات القبض البنكي في الفترة",
                r.TotalBankFees, PackIconKind.BankTransfer, "#6A1B9A", "#F3E5F5"),
            MoneyCard("operatingProfit", "الربح التشغيلي",
                "مجمل الربح − المصاريف − الرسوم البنكية",
                r.OperatingProfit, PackIconKind.ChartAreaspline, "#1565C0", "#E3F2FD"),
            MoneyCard("distributions", "توزيعات الأرباح",
                "مبالغ توزيع الأرباح على المستثمرين",
                r.DistributedProfits, PackIconKind.AccountCash, "#EF6C00", "#FFF3E0"),
            MoneyCard("netProfit", "صافي الربح",
                "التشغيلي − التوزيعات + رصيد افتتاحي للأرباح",
                r.NetProfit, PackIconKind.Finance, "#2E7D32", "#E8F5E9"),
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
                r.CustomerCollections, PackIconKind.CashPlus, "#2E7D32", "#E8F5E9"),
            CountCard("customersWithBalance", "عملاء برصيد",
                "عملاء رصيدهم المستحق أكبر من صفر",
                r.CustomersWithBalanceCount, PackIconKind.AccountAlert, "#EF6C00", "#FFF3E0"),
            MoneyCard("highestCustomer", "أعلى رصيد عميل",
                "أكبر رصيد مستحق لعميل واحد",
                r.HighestCustomerBalance, PackIconKind.AccountStar, "#C62828", "#FFEBEE"),
        ]));

        Sections.Add(MakeSection("الموردين", "#00695C",
        [
            CountCard("activeSuppliers", "موردون نشطون (الفترة)",
                "موردون لديهم فواتير شراء ضمن الفترة",
                r.ActiveSuppliersCount, PackIconKind.Truck, "#00695C", "#E0F2F1"),
            MoneyCard("supplierPayments", "المدفوع للموردين",
                "سندات صرف + مشتريات نقدية في الفترة",
                r.SupplierPayments, PackIconKind.CashRefund, "#EF6C00", "#FFF3E0"),
            CountCard("suppliersWithBalance", "موردون برصيد",
                "موردون رصيدهم الآجل أكبر من صفر",
                r.SuppliersWithBalanceCount, PackIconKind.TruckAlert, "#C62828", "#FFEBEE"),
            MoneyCard("highestSupplier", "أعلى رصيد مورد",
                "أكبر رصيد آجل لمورد واحد",
                r.HighestSupplierBalance, PackIconKind.TruckCheckOutline, "#1565C0", "#E3F2FD"),
        ]));

        Sections.Add(MakeSection("المخزون", "#283593",
        [
            CountCard("stockedProducts", "منتجات ذات رصيد",
                "عدد المنتجات التي كميتها أكبر من صفر",
                r.StockedProductCount, PackIconKind.Barcode, "#283593", "#E8EAF6"),
            MoneyCard("inventorySale", "قيمة المخزون (بيع)",
                "كمية المخزن × سعر البيع",
                r.InventorySaleValue, PackIconKind.Tag, "#1565C0", "#E3F2FD"),
            MoneyCard("inventoryPotential", "ربح محتمل بالمخزن",
                "قيمة البيع − قيمة التكلفة للمخزون الحالي",
                r.InventoryPotentialProfit, PackIconKind.ChartLineVariant, "#2E7D32", "#E8F5E9"),
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
                r.OverdueInstallmentsAmount, PackIconKind.CashRemove, "#AD1457", "#FCE4EC"),
            MoneyCard("collectedInstallments", "أقساط محصّلة",
                "مبالغ الأقساط المسددة خلال الفترة",
                r.CollectedInstallments, PackIconKind.CashCheck, "#2E7D32", "#E8F5E9"),
            MoneyCard("receipts", "سندات قبض",
                "مجموع سندات القبض والدين في الفترة",
                r.ReceiptVouchersAmount, PackIconKind.Receipt, "#00838F", "#E0F7FA"),
            MoneyCard("payments", "سندات صرف",
                "مجموع سندات الصرف في الفترة",
                r.PaymentVouchersAmount, PackIconKind.NoteMinus, "#EF6C00", "#FFF3E0"),
            MoneyCard("transfersAmount", "تحويلات (مبلغ)",
                "إجمالي مبالغ التحويلات بين الحسابات",
                r.TransfersAmount, PackIconKind.SwapHorizontal, "#1565C0", "#E3F2FD"),
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
                Formula = "الرصيد = متبقي فواتير الآجل − سندات دين غير مطبّقة − سندات قبض غير مطبّقة",
                ResultLabel = "رصيد الآجل للعملاء",
                ResultAmount = r.CustomerReceivables,
                Lines =
                [
                    Line("+", "متبقي فواتير الآجل", r.CustomerCreditInvoiceRemaining, "فواتير بيع/أقساط آجلة غير مسددة"),
                    Line("−", "سندات دين غير مطبّقة", r.CustomerUnappliedDebt),
                    Line("−", "سندات قبض غير مطبّقة", r.CustomerUnappliedReceipts, "غير مرتبطة بفاتورة أو قسط"),
                    Line("=", "رصيد الآجل للعملاء", r.CustomerReceivables, isResult: true)
                ]
            },
            "supplierCredit" => new AmountBreakdownModel
            {
                Title = "تفاصيل رصيد الآجل للموردين",
                Subtitle = period,
                Formula = "الرصيد = متبقي مشتريات الآجل − سندات صرف غير مطبّقة",
                ResultLabel = "رصيد الآجل للموردين",
                ResultAmount = r.SupplierPayables,
                Lines =
                [
                    Line("+", "متبقي مشتريات الآجل", r.SupplierCreditInvoiceRemaining),
                    Line("−", "سندات صرف غير مطبّقة", r.SupplierUnappliedPayments, "غير مرتبطة بفاتورة شراء"),
                    Line("=", "رصيد الآجل للموردين", r.SupplierPayables, isResult: true)
                ]
            },
            "installmentAr" => Single("ذمم الأقساط", "مجموع RemainingAmount للأقساط غير المسددة", r.InstallmentReceivables, period),
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
            "inventoryQty" => Single("كمية المخزون", "مجموع كميات WarehouseStocks > 0", r.InventoryQuantity, period, isMoney: false),
            "cashBoxes" => Single("أرصدة الصناديق", "مجموع أرصدة القاصات", r.CashBoxesBalance, period),
            "banks" => Single("أرصدة المصارف", "مجموع أرصدة الحسابات المصرفية", r.BankBalance, period),
            "nwc" => new AmountBreakdownModel
            {
                Title = "تفاصيل رأس المال العامل",
                Subtitle = period,
                Formula = "صناديق + مصارف + آجل عملاء + أقساط − آجل موردين",
                ResultLabel = "رأس المال العامل",
                ResultAmount = r.NetWorkingCapital,
                Lines =
                [
                    Line("+", "الصناديق", r.CashBoxesBalance),
                    Line("+", "المصارف", r.BankBalance),
                    Line("+", "آجل العملاء", r.CustomerReceivables),
                    Line("+", "ذمم الأقساط", r.InstallmentReceivables),
                    Line("−", "آجل الموردين", r.SupplierPayables),
                    Line("=", "رأس المال العامل", r.NetWorkingCapital, isResult: true)
                ]
            },
            "totalAssets" => Single("إجمالي الأصول", "من الميزانية العمومية", r.TotalAssets, period),
            "totalLiabilities" => Single("إجمالي الالتزامات", "من الميزانية العمومية", r.TotalLiabilities, period),
            "totalEquity" => Single("حقوق الملكية", "من الميزانية العمومية", r.TotalEquity, period),
            "accumulatedProfits" => Single("الأرباح المتراكمة", "من الميزانية العمومية حتى تاريخ النهاية", r.AccumulatedProfits, period),
            "totalSales" => new AmountBreakdownModel
            {
                Title = "تفاصيل إجمالي المبيعات",
                Subtitle = period,
                Formula = "نقدي + آجل + أقساط",
                ResultLabel = "إجمالي المبيعات",
                ResultAmount = r.TotalSales,
                Lines =
                [
                    Line("+", "مبيعات نقدية", r.CashSales),
                    Line("+", "مبيعات آجل", r.CreditSales),
                    Line("+", "مبيعات أقساط", r.InstallmentSales),
                    Line("=", "إجمالي المبيعات", r.TotalSales, isResult: true)
                ]
            },
            "cashSales" => Single("مبيعات نقدية", "فواتير بيع نقدي في الفترة", r.CashSales, period),
            "creditSales" => Single("مبيعات آجل", "فواتير بيع آجل في الفترة", r.CreditSales, period),
            "installmentSales" => Single("مبيعات أقساط", "فواتير بيع أقساط في الفترة", r.InstallmentSales, period),
            "salesCount" => Single("عدد فواتير البيع", "عدد فواتير البيع/الأقساط", r.SalesInvoiceCount, period, isMoney: false),
            "avgSale" => new AmountBreakdownModel
            {
                Title = "تفاصيل متوسط قيمة الفاتورة",
                Subtitle = period,
                Formula = "المتوسط = إجمالي المبيعات ÷ عدد الفواتير",
                ResultLabel = "متوسط قيمة الفاتورة",
                ResultAmount = r.AverageSaleInvoice,
                Lines =
                [
                    Line("+", "إجمالي المبيعات", r.TotalSales),
                    Line("÷", "عدد الفواتير", r.SalesInvoiceCount, isMoney: false),
                    Line("=", "المتوسط", r.AverageSaleInvoice, isResult: true)
                ]
            },
            "totalPurchases" => new AmountBreakdownModel
            {
                Title = "تفاصيل إجمالي المشتريات",
                Subtitle = period,
                Formula = "نقدي + آجل (+ طرق أخرى إن وجدت)",
                ResultLabel = "إجمالي المشتريات",
                ResultAmount = r.TotalPurchases,
                Lines =
                [
                    Line("+", "مشتريات نقدية", r.CashPurchases),
                    Line("+", "مشتريات آجل", r.CreditPurchases),
                    Line("=", "إجمالي المشتريات", r.TotalPurchases, isResult: true)
                ]
            },
            "cashPurchases" => Single("مشتريات نقدية", "فواتير شراء نقدي", r.CashPurchases, period),
            "creditPurchases" => Single("مشتريات آجل", "فواتير شراء آجل", r.CreditPurchases, period),
            "purchaseCount" => Single("عدد فواتير الشراء", "عدد فواتير الشراء في الفترة", r.PurchaseInvoiceCount, period, isMoney: false),
            "cogs" => Single("تكلفة البضاعة المباعة", "متوسط التكلفة × الكميات المباعة", r.CostOfGoodsSold, period),
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
                ]
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
            "expenses" => Single("المصروفات", "مجموع مصاريف الفترة", r.TotalExpenses, period),
            "bankFees" => Single("الرسوم البنكية", "BankFees من سندات القبض البنكي", r.TotalBankFees, period),
            "operatingProfit" => new AmountBreakdownModel
            {
                Title = "تفاصيل الربح التشغيلي",
                Subtitle = period,
                Formula = "تشغيلي = مجمل − مصاريف − رسوم بنكية",
                ResultLabel = "الربح التشغيلي",
                ResultAmount = r.OperatingProfit,
                Lines =
                [
                    Line("+", "مجمل الربح", r.GrossProfit),
                    Line("−", "المصروفات", r.TotalExpenses),
                    Line("−", "الرسوم البنكية", r.TotalBankFees),
                    Line("=", "الربح التشغيلي", r.OperatingProfit, isResult: true)
                ]
            },
            "distributions" => Single("توزيعات الأرباح", "مجموع توزيعات الفترة", r.DistributedProfits, period),
            "netProfit" => new AmountBreakdownModel
            {
                Title = "تفاصيل صافي الربح",
                Subtitle = period,
                Formula = "الصافي = مجمل − مصاريف − رسوم − توزيعات + افتتاحي أرباح",
                ResultLabel = "صافي الربح",
                ResultAmount = r.NetProfit,
                Lines =
                [
                    Line("+", "مجمل الربح", r.GrossProfit),
                    Line("−", "المصروفات", r.TotalExpenses),
                    Line("−", "الرسوم البنكية", r.TotalBankFees),
                    Line("−", "توزيعات الأرباح", r.DistributedProfits),
                    Line("+", "رصيد افتتاحي للأرباح", r.ProfitOpeningBalance),
                    Line("=", "صافي الربح", r.NetProfit, isResult: true)
                ]
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
            "activeCustomers" => Single("عملاء نشطون", "Distinct CustomerId على فواتير البيع", r.ActiveCustomersCount, period, isMoney: false),
            "customerCollections" => new AmountBreakdownModel
            {
                Title = "تفاصيل المحصّل من العملاء",
                Subtitle = period,
                Formula = "سندات قبض/دين + أقساط محصّلة",
                ResultLabel = "المحصّل",
                ResultAmount = r.CustomerCollections,
                Lines =
                [
                    Line("+", "سندات قبض/دين", r.ReceiptVouchersAmount),
                    Line("+", "أقساط محصّلة", r.CollectedInstallments),
                    Line("=", "المحصّل من العملاء", r.CustomerCollections, isResult: true)
                ]
            },
            "customersWithBalance" => Single("عملاء برصيد", "بعد خصم السندات غير المطبّقة", r.CustomersWithBalanceCount, period, isMoney: false),
            "highestCustomer" => Single("أعلى رصيد عميل", "أقصى رصيد مستحق لعميل", r.HighestCustomerBalance, period),
            "activeSuppliers" => Single("موردون نشطون", "Distinct SupplierId على فواتير الشراء", r.ActiveSuppliersCount, period, isMoney: false),
            "supplierPayments" => new AmountBreakdownModel
            {
                Title = "تفاصيل المدفوع للموردين",
                Subtitle = period,
                Formula = "سندات صرف + مشتريات نقدية",
                ResultLabel = "المدفوع",
                ResultAmount = r.SupplierPayments,
                Lines =
                [
                    Line("+", "سندات صرف", r.PaymentVouchersAmount),
                    Line("+", "مشتريات نقدية", r.CashPurchases),
                    Line("=", "المدفوع للموردين", r.SupplierPayments, isResult: true)
                ]
            },
            "suppliersWithBalance" => Single("موردون برصيد", "بعد خصم سندات الصرف غير المطبّقة", r.SuppliersWithBalanceCount, period, isMoney: false),
            "highestSupplier" => Single("أعلى رصيد مورد", "أقصى رصيد آجل لمورد", r.HighestSupplierBalance, period),
            "stockedProducts" => Single("منتجات ذات رصيد", "منتجات بكمية > 0", r.StockedProductCount, period, isMoney: false),
            "inventorySale" => Single("قيمة المخزون بسعر البيع", "كمية × سعر البيع", r.InventorySaleValue, period),
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
            "belowMin" => Single("تحت الحد الأدنى", "Quantity < MinQuantity", r.BelowMinimumStockCount, period, isMoney: false),
            "overdueCount" => Single("أقساط متأخرة (عدد)", "DueDate قبل تاريخ النهاية ومتبقي > 0", r.OverdueInstallmentsCount, period, isMoney: false),
            "overdueAmount" => Single("أقساط متأخرة (مبلغ)", "مجموع المتبقي للأقساط المتأخرة", r.OverdueInstallmentsAmount, period),
            "collectedInstallments" => Single("أقساط محصّلة", "PaidAmount ضمن الفترة", r.CollectedInstallments, period),
            "receipts" => Single("سندات قبض", "Receipt + DebtReceipt في الفترة", r.ReceiptVouchersAmount, period),
            "payments" => Single("سندات صرف", "Payment في الفترة", r.PaymentVouchersAmount, period),
            "transfersAmount" => Single("تحويلات (مبلغ)", "مجموع مبالغ التحويلات", r.TransfersAmount, period),
            "transfersCount" => Single("عدد التحويلات", "عدد سجلات التحويل", r.TransfersCount, period, isMoney: false),
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
                ShowAmount = showAmount
            }).ToList()
        };
    }

    private static AmountBreakdownModel Single(
        string title, string formula, decimal amount, string period, bool isMoney = true)
        => new()
        {
            Title = $"تفاصيل {title}",
            Subtitle = period,
            Formula = formula,
            ResultLabel = title,
            ResultAmount = amount,
            Lines =
            [
                Line("=", title, amount, isResult: true, isMoney: isMoney)
            ]
        };

    private static AmountBreakdownLine Line(
        string op, string label, decimal amount, string? description = null, bool isResult = false, bool isMoney = true)
    {
        _ = isMoney;
        return new AmountBreakdownLine
        {
            Operator = op,
            Label = label,
            Amount = amount,
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

    private static ExecutiveSummaryCardItem MoneyCard(
        string key, string title, string hint, decimal amount, PackIconKind icon, string accent, string light)
        => Card(key, title, hint, amount, "د.ع", FormatCurrency(amount), icon, accent, light);

    private static ExecutiveSummaryCardItem CountCard(
        string key, string title, string hint, int amount, PackIconKind icon, string accent, string light)
        => Card(key, title, hint, amount, null, amount.ToString("N0"), icon, accent, light);

    private static ExecutiveSummaryCardItem QtyCard(
        string key, string title, string hint, decimal amount, PackIconKind icon, string accent, string light)
        => Card(key, title, hint, amount, null, amount.ToString("N0"), icon, accent, light);

    private static ExecutiveSummaryCardItem PercentCard(
        string key, string title, string hint, decimal amount, PackIconKind icon, string accent, string light)
        => Card(key, title, hint, amount, "%", $"{amount:N1} %", icon, accent, light);

    private static ExecutiveSummaryCardItem Card(
        string key, string title, string hint, decimal amount, string? suffix, string display,
        PackIconKind icon, string accent, string light)
    {
        var accentBrush = BrushFrom(accent);
        return new ExecutiveSummaryCardItem
        {
            MetricKey = key,
            Title = title,
            Hint = hint,
            Amount = amount,
            Suffix = suffix,
            Value = display,
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
