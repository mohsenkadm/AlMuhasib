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

    public static string EmptyResult(string title)
        => $"لا توجد بيانات لعرضها في «{title}» حالياً.";

    public static string GenericError()
        => "تعذّر جلب التقرير الآن. حاول لاحقاً أو أعد ربط الحساب إن استمرت المشكلة.";

    public static string SessionExpired()
        => "انتهت جلسة الحساب. افتح صفحة الربط من جديد عبر /link ثم أدخل الرمز الجديد.";

    public static string NotLinked(string linkUrl)
        => $"حسابك غير مربوط بنظام قيد.\nاربط حسابك من الصفحة الآمنة:\n{linkUrl}\nثم أرسل الرمز هنا أو استخدم /link الرمز";

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
