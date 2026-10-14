using System.Globalization;
using System.Text;
using Qaid.TelegramBot.Application.Models;

namespace Qaid.TelegramBot.Application.Reports;

public static class ArabicReportFormatter
{
    private static readonly CultureInfo Ar = CultureInfo.GetCultureInfo("ar-IQ");
    private const int MaxRows = 8;
    private const int MaxMessageChars = 3500;

    public static string FormatMoney(decimal value)
        => value.ToString("#,##0.##", Ar);

    public static string FormatDate(DateTime value)
        => value.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);

    public static string FormatDateTime(DateTime? value)
        => value is null ? "غير متوفر" : value.Value.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture) + " UTC";

    public static string FormatDailySales(DailySalesReportResult r, string periodLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📊 المبيعات اليومية");
        sb.AppendLine($"الفترة: {periodLabel}");
        sb.AppendLine($"إجمالي المبيعات: {FormatMoney(r.TotalSales)} د.ع");
        if (r.TotalSalesUsd != 0)
            sb.AppendLine($"مبيعات دولار (منفصل): {FormatMoney(r.TotalSalesUsd)} $");
        sb.AppendLine($"عدد الفواتير: {r.InvoiceCount}");
        sb.AppendLine($"عدد الأيام: {r.DayCount}");
        sb.AppendLine($"متوسط يومي: {FormatMoney(r.AverageDaily)} د.ع");
        AppendRows(sb, r.Rows, row =>
            $"{FormatDate(row.Date)} | فواتير {row.InvoiceCount} | {FormatMoney(row.TotalSales)} د.ع");
        return Trim(sb);
    }

    public static string FormatExpenses(ExpensesReportResult r, string periodLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("💸 المصروفات");
        sb.AppendLine($"الفترة: {periodLabel}");
        sb.AppendLine($"إجمالي المصروفات: {FormatMoney(r.TotalExpenses)} د.ع");
        sb.AppendLine($"مصروفات اليوم: {FormatMoney(r.TodayExpenses)} د.ع");
        sb.AppendLine($"مصروفات الشهر: {FormatMoney(r.MonthExpenses)} د.ع");
        if (!string.IsNullOrWhiteSpace(r.TopExpenseType))
            sb.AppendLine($"أعلى نوع: {r.TopExpenseType}");
        AppendRows(sb, r.Rows, row =>
            $"{FormatDate(row.Date)} | {row.ExpenseTypeName} | {FormatMoney(row.Amount)} د.ع");
        return Trim(sb);
    }

    public static string FormatProfitAndLoss(ProfitAndLossReportResult r, string periodLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📈 الأرباح والخسائر");
        sb.AppendLine($"الفترة: {periodLabel}");
        sb.AppendLine($"المبيعات: {FormatMoney(r.TotalSales)} د.ع");
        if (r.TotalSalesUsd != 0)
            sb.AppendLine($"مبيعات دولار (منفصل): {FormatMoney(r.TotalSalesUsd)} $");
        sb.AppendLine($"تكلفة البضاعة: {FormatMoney(r.CostOfGoodsSold)} د.ع");
        sb.AppendLine($"مجمل الربح: {FormatMoney(r.GrossProfit)} د.ع");
        sb.AppendLine($"المصروفات: {FormatMoney(r.TotalExpenses)} د.ع");
        sb.AppendLine($"ربح التشغيل: {FormatMoney(r.OperatingProfit)} د.ع");
        sb.AppendLine($"صافي الربح: {FormatMoney(r.NetProfit)} د.ع");
        sb.AppendLine($"هامش مجمل: {FormatMoney(r.GrossMarginPercent)}%");
        sb.AppendLine($"هامش صافي: {FormatMoney(r.NetMarginPercent)}%");
        return Trim(sb);
    }

    public static string FormatCashBalances(CashBalancesSummaryReportResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🏦 أرصدة النقد والبنوك");
        sb.AppendLine($"الصناديق: {FormatMoney(r.CashBoxesTotal)} د.ع");
        sb.AppendLine($"البنوك: {FormatMoney(r.BanksTotal)} د.ع");
        sb.AppendLine($"الإجمالي السائل: {FormatMoney(r.TotalLiquid)} د.ع");
        if (r.TotalLiquidUsd != 0)
            sb.AppendLine($"إجمالي دولار (منفصل): {FormatMoney(r.TotalLiquidUsd)} $");
        sb.AppendLine($"عدد الحسابات: {r.AccountCount}");
        AppendRows(sb, r.Rows, row =>
            $"{row.AccountType} | {row.Name} | {FormatMoney(row.Balance)}");
        return Trim(sb);
    }

    public static string FormatCashFlow(CashFlowResult r, string periodLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("💵 التدفق النقدي");
        sb.AppendLine($"الفترة: {periodLabel}");
        sb.AppendLine($"الوارد: {FormatMoney(r.TotalIncoming)} د.ع");
        sb.AppendLine($"الصادر: {FormatMoney(r.TotalOutgoing)} د.ع");
        sb.AppendLine($"صافي التدفق: {FormatMoney(r.NetFlow)} د.ع");
        sb.AppendLine($"الرصيد الحالي: {FormatMoney(r.CurrentBalance)} د.ع");
        AppendRows(sb, r.Rows, row =>
            $"{FormatDate(row.Date)} | {row.Type} | +{FormatMoney(row.Incoming)} / -{FormatMoney(row.Outgoing)}");
        return Trim(sb);
    }

    public static string FormatCustomers(CustomersOverviewReportResult r, string periodLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("👥 ملخص الزبائن");
        sb.AppendLine($"الفترة: {periodLabel}");
        sb.AppendLine($"عدد الزبائن: {r.CustomerCount}");
        sb.AppendLine($"إجمالي المبيعات: {FormatMoney(r.TotalSales)} د.ع");
        sb.AppendLine($"المحصّل: {FormatMoney(r.TotalCollected)} د.ع");
        sb.AppendLine($"المتبقي: {FormatMoney(r.TotalOutstanding)} د.ع");
        if (r.TotalOutstandingUsd != 0)
            sb.AppendLine($"متبقي دولار (منفصل): {FormatMoney(r.TotalOutstandingUsd)} $");
        AppendRows(sb, r.Rows, row =>
            $"{row.CustomerName} | مبيعات {FormatMoney(row.SalesAmount)} | متبقي {FormatMoney(row.OutstandingBalance)}");
        return Trim(sb);
    }

    public static string FormatSyncStatus(SyncStatusResponse r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🔄 حالة المزامنة");
        sb.AppendLine($"آخر مزامنة: {FormatDateTime(r.LastSyncAt)}");
        sb.AppendLine($"بانتظار الإرسال: {r.PendingPushCount}");
        sb.AppendLine($"الترخيص نشط: {(r.IsLicensed ? "نعم" : "لا")}");
        if (!string.IsNullOrWhiteSpace(r.LicenseMessage))
            sb.AppendLine(r.LicenseMessage);
        return Trim(sb);
    }

    public static string FormatDailyOperations(DailyOperationsReportResult r, string periodLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📋 ملخص العمليات");
        sb.AppendLine($"الفترة: {periodLabel}");
        sb.AppendLine($"مبيعات نقدية: {FormatMoney(r.CashSales)} د.ع ({r.CashSalesInvoiceCount})");
        sb.AppendLine($"مرتجعات: {FormatMoney(r.SalesReturns)} د.ع");
        sb.AppendLine($"مصروفات: {FormatMoney(r.Expenses)} د.ع ({r.ExpenseCount})");
        sb.AppendLine($"دفعات موردين: {FormatMoney(r.SupplierPayments)} د.ع");
        sb.AppendLine($"مقبوضات: {FormatMoney(r.Receipts)} د.ع");
        sb.AppendLine($"صافي اليوم: {FormatMoney(r.NetAmount)} د.ع");
        return Trim(sb);
    }

    public static string FormatInstallmentsOverdue(OverdueResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("⏰ أقساط متأخرة");
        sb.AppendLine($"عدد الزبائن: {r.OverdueCustomerCount}");
        sb.AppendLine($"إجمالي المتأخر: {FormatMoney(r.TotalOverdueAmount)} د.ع");
        sb.AppendLine($"متوسط أيام التأخير: {r.AverageOverdueDays}");
        if (!string.IsNullOrWhiteSpace(r.TopOverdueCustomer))
            sb.AppendLine($"أعلى متأخر: {r.TopOverdueCustomer}");
        AppendRows(sb, r.Rows, row =>
            $"{row.CustomerName} | {FormatMoney(row.OverdueAmount)} د.ع | {row.OverdueDays} يوم");
        return Trim(sb);
    }

    public static string FormatInstallmentsSummary(InstallmentsSummaryResult r, string periodLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📑 ملخص الأقساط");
        sb.AppendLine($"الفترة: {periodLabel}");
        sb.AppendLine($"الإجمالي: {FormatMoney(r.TotalAmount)} د.ع");
        sb.AppendLine($"المدفوع: {FormatMoney(r.PaidAmount)} د.ع ({r.PaidCount})");
        sb.AppendLine($"غير المدفوع: {FormatMoney(r.UnpaidAmount)} د.ع ({r.UnpaidCount})");
        sb.AppendLine($"المتأخر: {FormatMoney(r.OverdueAmount)} د.ع ({r.OverdueCount})");
        AppendRows(sb, r.Rows, row =>
            $"{row.CustomerName} | متبقي {FormatMoney(row.RemainingAmount)} | {row.Status}");
        return Trim(sb);
    }

    public static string FormatOverdueCustomers(OverdueCustomersReportResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🚨 زبائن متأخرون");
        sb.AppendLine($"عدد الزبائن: {r.CustomerCount}");
        sb.AppendLine($"إجمالي المتأخر: {FormatMoney(r.TotalOverdue)} د.ع");
        if (r.TotalOverdueUsd != 0)
            sb.AppendLine($"متأخر دولار (منفصل): {FormatMoney(r.TotalOverdueUsd)} $");
        sb.AppendLine($"متوسط أيام التأخير: {FormatMoney(r.AverageDaysOverdue)}");
        AppendRows(sb, r.Rows, row =>
            $"{row.CustomerName} | {FormatMoney(row.OverdueAmount)} | {row.DaysOverdue} يوم");
        return Trim(sb);
    }

    public static string FormatReceivablesAging(ReceivablesAgingReportResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📅 أعمار الذمم");
        sb.AppendLine($"عدد الزبائن: {r.CustomerCount}");
        sb.AppendLine($"إجمالي المتبقي: {FormatMoney(r.TotalOutstanding)} د.ع");
        if (r.TotalOutstandingUsd != 0)
            sb.AppendLine($"متبقي دولار (منفصل): {FormatMoney(r.TotalOutstandingUsd)} $");
        AppendRows(sb, r.Buckets, b => $"{b.BucketName}: {FormatMoney(b.Amount)} د.ع ({b.Count})");
        AppendRows(sb, r.Rows, row =>
            $"{row.CustomerName} | {FormatMoney(row.RemainingAmount)} | {row.AgingBucket}");
        return Trim(sb);
    }

    public static string FormatMinimumQuantity(MinimumQuantityReportResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📦 حد أدنى للمخزون");
        sb.AppendLine($"تحت الحد: {r.BelowMinimumCount}");
        sb.AppendLine($"عند الحد: {r.AtMinimumCount}");
        sb.AppendLine($"فوق الحد: {r.AboveMinimumCount}");
        sb.AppendLine($"إجمالي النقص: {FormatMoney(r.TotalShortage)}");
        var critical = r.Rows.Where(x => x.Status == 0).ToList();
        AppendRows(sb, critical.Count > 0 ? critical : r.Rows, row =>
            $"{row.ProductName} | {row.WarehouseName} | {FormatMoney(row.CurrentQuantity)}/{FormatMoney(row.MinQuantity)}");
        return Trim(sb);
    }

    public static string FormatStockHealth(StockHealthReportResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🩺 صحة المخزون");
        sb.AppendLine($"مخزون منخفض: {r.LowStockCount}");
        sb.AppendLine($"مخزون راكد: {r.DeadStockCount}");
        sb.AppendLine($"قيمة الراكد: {FormatMoney(r.TotalDeadStockValue)} د.ع");
        AppendRows(sb, r.Rows, row =>
            $"{row.ProductName} | {(row.Status == 1 ? "راكد" : "منخفض")} | كمية {FormatMoney(row.Quantity)}");
        return Trim(sb);
    }

    public static string FormatTopProducts(TopProductsReportResult r, string periodLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("⭐ أفضل المنتجات");
        sb.AppendLine($"الفترة: {periodLabel}");
        sb.AppendLine($"عدد المنتجات: {r.ProductCount}");
        sb.AppendLine($"إجمالي الإيراد: {FormatMoney(r.TotalRevenue)} د.ع");
        AppendRows(sb, r.Rows, row =>
            $"#{row.Rank} {row.ProductName} | {FormatMoney(row.Revenue)} د.ع | {FormatMoney(row.SharePercent)}%");
        return Trim(sb);
    }

    public static string FormatPurchases(PurchasesReportResult r, string periodLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🛒 المشتريات");
        sb.AppendLine($"الفترة: {periodLabel}");
        sb.AppendLine($"إجمالي المشتريات: {FormatMoney(r.TotalPurchases)} د.ع");
        if (r.TotalPurchasesUsd != 0)
            sb.AppendLine($"مشتريات دولار (منفصل): {FormatMoney(r.TotalPurchasesUsd)} $");
        sb.AppendLine($"عدد الفواتير: {r.InvoiceCount}");
        AppendRows(sb, r.Rows, row =>
            $"{FormatDate(row.Date)} | {row.SupplierName} | {FormatMoney(row.NetAmount)}");
        return Trim(sb);
    }

    public static string FormatSuppliers(SuppliersOverviewReportResult r, string periodLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🏭 ملخص الموردين");
        sb.AppendLine($"الفترة: {periodLabel}");
        sb.AppendLine($"عدد الموردين: {r.SupplierCount}");
        sb.AppendLine($"المشتريات: {FormatMoney(r.TotalPurchases)} د.ع");
        sb.AppendLine($"المدفوع: {FormatMoney(r.TotalPaid)} د.ع");
        sb.AppendLine($"المتبقي: {FormatMoney(r.TotalOutstanding)} د.ع");
        AppendRows(sb, r.Rows, row =>
            $"{row.SupplierName} | متبقي {FormatMoney(row.OutstandingBalance)}");
        return Trim(sb);
    }

    public static string FormatSalesByPayment(SalesByPaymentMethodReportResult r, string periodLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("💳 المبيعات حسب الدفع");
        sb.AppendLine($"الفترة: {periodLabel}");
        sb.AppendLine($"الإجمالي: {FormatMoney(r.TotalSales)} د.ع");
        sb.AppendLine($"نقد: {FormatMoney(r.CashSales)} | آجل: {FormatMoney(r.CreditSales)} | أقساط: {FormatMoney(r.InstallmentSales)}");
        sb.AppendLine($"عدد الفواتير: {r.InvoiceCount}");
        AppendRows(sb, r.Rows, row =>
            $"{row.PaymentMethod} | {FormatMoney(row.Amount)} د.ع | {FormatMoney(row.SharePercent)}%");
        return Trim(sb);
    }

    public static string FormatPayablesAging(PayablesAgingReportResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📉 أعمار الدائنين");
        sb.AppendLine($"عدد الموردين: {r.SupplierCount}");
        sb.AppendLine($"إجمالي المتبقي: {FormatMoney(r.TotalOutstanding)} د.ع");
        if (r.TotalOutstandingUsd != 0)
            sb.AppendLine($"متبقي دولار (منفصل): {FormatMoney(r.TotalOutstandingUsd)} $");
        AppendRows(sb, r.Buckets, b => $"{b.BucketName}: {FormatMoney(b.Amount)} د.ع ({b.Count})");
        AppendRows(sb, r.Rows, row =>
            $"{row.SupplierName} | {FormatMoney(row.RemainingAmount)} | {row.AgingBucket}");
        return Trim(sb);
    }

    public static string FormatCustomerCollections(CustomerCollectionsReportResult r, string periodLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("💰 تحصيلات الزبائن");
        sb.AppendLine($"الفترة: {periodLabel}");
        sb.AppendLine($"إجمالي التحصيل: {FormatMoney(r.TotalCollected)} د.ع");
        if (r.TotalCollectedUsd != 0)
            sb.AppendLine($"تحصيل دولار (منفصل): {FormatMoney(r.TotalCollectedUsd)} $");
        sb.AppendLine($"سندات: {FormatMoney(r.VoucherCollections)} | أقساط: {FormatMoney(r.InstallmentCollections)}");
        AppendRows(sb, r.Rows, row =>
            $"{FormatDate(row.Date)} | {row.CustomerName} | {FormatMoney(row.Amount)}");
        return Trim(sb);
    }

    public static string FormatSupplierPayments(SupplierPaymentsReportResult r, string periodLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🧾 مدفوعات الموردين");
        sb.AppendLine($"الفترة: {periodLabel}");
        sb.AppendLine($"إجمالي المدفوع: {FormatMoney(r.TotalPaid)} د.ع");
        sb.AppendLine($"سندات: {FormatMoney(r.VoucherPayments)} | مشتريات نقد: {FormatMoney(r.CashPurchases)}");
        AppendRows(sb, r.Rows, row =>
            $"{FormatDate(row.Date)} | {row.SupplierName} | {FormatMoney(row.Amount)}");
        return Trim(sb);
    }

    public static string FormatFinancialPosition(FinancialPositionSummaryReportResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🏛️ ملخص الوضع المالي");
        sb.AppendLine($"الأصول: {FormatMoney(r.TotalAssets)} د.ع");
        sb.AppendLine($"الالتزامات: {FormatMoney(r.TotalLiabilities)} د.ع");
        sb.AppendLine($"حقوق الملكية: {FormatMoney(r.TotalEquity)} د.ع");
        sb.AppendLine($"رأس المال العامل: {FormatMoney(r.NetWorkingCapital)} د.ع");
        sb.AppendLine($"متوازن: {(r.IsBalanced ? "نعم" : "لا")} (فرق {FormatMoney(r.Difference)})");
        AppendRows(sb, r.Rows, row =>
            $"{row.Section} | {row.LineName} | {FormatMoney(row.Amount)}");
        return Trim(sb);
    }

    public static string FormatCogs(CogsReportResult r, string periodLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📐 تكلفة البضاعة المباعة");
        sb.AppendLine($"الفترة: {periodLabel}");
        sb.AppendLine($"التكلفة: {FormatMoney(r.TotalCogs)} د.ع");
        sb.AppendLine($"الإيراد: {FormatMoney(r.TotalRevenue)} د.ع");
        if (r.TotalRevenueUsd != 0)
            sb.AppendLine($"إيراد دولار (منفصل): {FormatMoney(r.TotalRevenueUsd)} $");
        sb.AppendLine($"مجمل الربح: {FormatMoney(r.GrossProfit)} د.ع");
        sb.AppendLine($"عدد المنتجات: {r.ProductCount}");
        AppendRows(sb, r.Rows, row =>
            $"{row.ProductName} | تكلفة {FormatMoney(row.CogsAmount)} | ربح {FormatMoney(row.GrossProfit)}");
        return Trim(sb);
    }

    public static string FormatInventoryValuation(InventoryValuationReportResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🏷️ تقييم المخزون");
        sb.AppendLine($"إجمالي القيمة: {FormatMoney(r.TotalValue)} د.ع");
        sb.AppendLine($"إجمالي الكمية: {FormatMoney(r.TotalQuantity)}");
        sb.AppendLine($"منتجات: {r.ProductCount} | مخازن: {r.WarehouseCount}");
        AppendRows(sb, r.Rows, row =>
            $"{row.ProductName} | {row.WarehouseName} | {FormatMoney(row.TotalValue)}");
        return Trim(sb);
    }

    public static string FormatTransfers(TransfersReportResult r, string periodLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("↔️ التحويلات");
        sb.AppendLine($"الفترة: {periodLabel}");
        sb.AppendLine($"عدد التحويلات: {r.TransferCount}");
        sb.AppendLine($"الإجمالي: {FormatMoney(r.TotalAmount)} د.ع");
        if (r.TotalAmountUsd != 0)
            sb.AppendLine($"إجمالي دولار (منفصل): {FormatMoney(r.TotalAmountUsd)} $");
        AppendRows(sb, r.Rows, row =>
            $"{FormatDate(row.Date)} | {row.FromAccount} → {row.ToAccount} | {FormatMoney(row.Amount)}");
        return Trim(sb);
    }

    public static string FormatGrossProfitMargin(GrossProfitMarginReportResult r, string periodLabel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📊 هامش مجمل الربح");
        sb.AppendLine($"الفترة: {periodLabel}");
        sb.AppendLine($"المبيعات: {FormatMoney(r.TotalSales)} د.ع");
        sb.AppendLine($"التكلفة: {FormatMoney(r.CostOfGoodsSold)} د.ع");
        sb.AppendLine($"مجمل الربح: {FormatMoney(r.GrossProfit)} د.ع");
        sb.AppendLine($"الهامش: {FormatMoney(r.GrossMarginPercent)}%");
        AppendRows(sb, r.Rows, row =>
            $"{FormatDate(row.Date)} | {row.CustomerName} | {FormatMoney(row.GrossProfit)} ({FormatMoney(row.MarginPercent)}%)");
        return Trim(sb);
    }

    public static string FormatCustomerStatement(CustomerStatementResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("👤 كشف حساب زبون");
        sb.AppendLine($"الاسم: {r.CustomerName}");
        if (!string.IsNullOrWhiteSpace(r.CustomerFileNumber))
            sb.AppendLine($"الملف: {r.CustomerFileNumber}");
        sb.AppendLine($"مدين: {FormatMoney(r.TotalDebit)} | دائن: {FormatMoney(r.TotalCredit)}");
        sb.AppendLine($"الرصيد: {FormatMoney(r.Balance)} د.ع");
        if (r.BalanceUsd != 0)
            sb.AppendLine($"رصيد دولار (منفصل): {FormatMoney(r.BalanceUsd)} $");
        sb.AppendLine($"عدد الحركات: {r.TransactionCount}");
        AppendRows(sb, r.Rows, row =>
            $"{FormatDate(row.Date)} | {row.Description} | م {FormatMoney(row.Debit)} / د {FormatMoney(row.Credit)}");
        return Trim(sb);
    }

    public static string FormatSupplierStatement(SupplierStatementResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🏭 كشف حساب مورد");
        sb.AppendLine($"الاسم: {r.SupplierName}");
        sb.AppendLine($"مدين: {FormatMoney(r.TotalDebit)} | دائن: {FormatMoney(r.TotalCredit)}");
        sb.AppendLine($"الرصيد: {FormatMoney(r.Balance)} د.ع");
        if (r.BalanceUsd != 0)
            sb.AppendLine($"رصيد دولار (منفصل): {FormatMoney(r.BalanceUsd)} $");
        AppendRows(sb, r.Rows, row =>
            $"{FormatDate(row.Date)} | {row.Description} | م {FormatMoney(row.Debit)} / د {FormatMoney(row.Credit)}");
        return Trim(sb);
    }

    public static string FormatCarDashboard(CarDashboardDto r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🚗 لوحة عقود السيارات");
        sb.AppendLine($"عقود اليوم: {r.TodayContracts} | هذا الشهر: {r.MonthContracts}");
        sb.AppendLine($"الإجمالي: {r.TotalContracts} | غير المسدد: {r.UnpaidContracts}");
        sb.AppendLine($"قيمة السيارات: {FormatMoney(r.TotalCarValue)} د.ع");
        sb.AppendLine($"المستلم: {FormatMoney(r.TotalReceived)} | المتبقي: {FormatMoney(r.TotalRemaining)}");
        return Trim(sb);
    }

    public static string FormatCarTradeDashboard(CarTradeDashboardDto r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🚘 لوحة تجارة السيارات");
        sb.AppendLine($"اليوم: {r.TodayTransactions} | الشهر: {r.MonthTransactions} | الكل: {r.TotalTransactions}");
        sb.AppendLine($"شراء: {r.BuyCount} ({FormatMoney(r.TotalBuyValue)}) | بيع: {r.SellCount} ({FormatMoney(r.TotalSellValue)})");
        sb.AppendLine($"المدفوع: {FormatMoney(r.TotalPaid)} | المتبقي: {FormatMoney(r.TotalRemaining)}");
        return Trim(sb);
    }

    public static string FormatRealEstateDashboard(RealEstateDashboardDto r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🏠 لوحة العقارات");
        sb.AppendLine($"عقود اليوم: {r.TodayContracts} | الشهر: {r.MonthContracts} | الكل: {r.TotalContracts}");
        sb.AppendLine($"غير المسدد: {r.UnpaidContracts} | ديون متأخرة: {r.OverdueDebts}");
        sb.AppendLine($"القيمة: {FormatMoney(r.TotalValue)} | المستلم: {FormatMoney(r.TotalReceived)} | المتبقي: {FormatMoney(r.TotalRemaining)}");
        return Trim(sb);
    }

    public static string FormatHotelSummary(HotelRestaurantSummaryDto r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🏨 ملخص مطعم الفندق");
        sb.AppendLine($"الإيراد: {FormatMoney(r.Revenue)} د.ع");
        sb.AppendLine($"التكلفة: {FormatMoney(r.Cogs)} د.ع");
        sb.AppendLine($"مجمل الربح: {FormatMoney(r.GrossProfit)} د.ع ({FormatMoney(r.MarginPercent)}%)");
        sb.AppendLine($"عدد الطلبات: {r.OrderCount} | متوسط الطلب: {FormatMoney(r.AverageOrderValue)}");
        return Trim(sb);
    }

    public static string FormatGoldStock(GoldStockReportDto r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🥇 مخزون الذهب");
        sb.AppendLine($"إجمالي الغرامات: {FormatMoney(r.TotalGrams)}");
        sb.AppendLine($"إجمالي القيمة: {FormatMoney(r.TotalValue)} د.ع");
        sb.AppendLine($"تنبيهات مخزون منخفض: {r.LowStockCount}");
        return Trim(sb);
    }

    public static string EmptyResult(string title)
        => $"لا توجد بيانات لعرضها في «{title}» حالياً.";

    public static string GenericError()
        => "تعذّر جلب التقرير الآن. حاول لاحقاً أو أعد ربط الحساب إن استمرت المشكلة.";

    public static string SessionExpired()
        => "انتهت صلاحية الربط (10 أيام). افتح صفحة الربط من جديد ثم أدخل الرمز.";

    public static string NeedBranch()
        => "يجب اختيار الفرع أولاً قبل عرض التقارير.";

    public static string NotLinked(string linkUrl)
        => "مرحباً بك في بوت تقارير قيد.\n\nحسابك غير مربوط بعد.\nاضغط «تسجيل الدخول إلى قيد» لتسجيل الدخول بحسابك، ثم أرسل رمز الربط هنا.";

    public static string WelcomeLogin(string linkUrl)
        => $"مرحباً بك في بوت تقارير قيد.\n\nللبدء: اضغط الزر أدناه لتسجيل الدخول بحساب قيد، ثم أرسل رمز الربط الذي يظهر لك.\n\n{linkUrl}";

    private static void AppendRows<T>(StringBuilder sb, IReadOnlyList<T> rows, Func<T, string> format)
    {
        if (rows.Count == 0)
            return;
        sb.AppendLine();
        sb.AppendLine("أحدث السجلات:");
        foreach (var row in rows.Take(MaxRows))
            sb.AppendLine("• " + format(row));
        if (rows.Count > MaxRows)
            sb.AppendLine($"… و{rows.Count - MaxRows} سجلاً إضافياً (مختصر)");
    }

    private static string Trim(StringBuilder sb)
    {
        var text = sb.ToString().Trim();
        if (text.Length <= MaxMessageChars)
            return text;
        return text[..MaxMessageChars] + "\n… (تم اختصار النتيجة)";
    }
}
