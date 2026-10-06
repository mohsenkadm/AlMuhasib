namespace AlMuhasib.Core.Helpers;

/// <summary>
/// حسابات صافية ونسب للتقرير اليومي — منطق نقي قابل للاختبار بدون قاعدة بيانات.
/// </summary>
public static class DailyOperationsReportCalculator
{
    /// <summary>
    /// الصافي التشغيلي اليومي =
    /// مبيعات نقدية − مرتجعات − مصاريف − مدفوعات الموردين + وصولات القبض
    /// </summary>
    public static decimal ComputeNet(
        decimal cashSales,
        decimal salesReturns,
        decimal expenses,
        decimal supplierPayments,
        decimal receipts)
        => cashSales - salesReturns - expenses - supplierPayments + receipts;

    /// <summary>نسبة الحصة من الإجمالي (مقربة لمنزلة عشرية واحدة).</summary>
    public static decimal SharePercent(decimal part, decimal total)
        => total != 0 ? Math.Round(part / total * 100m, 1) : 0m;

    /// <summary>تطبيق النسب على قائمة من المبالغ.</summary>
    public static IReadOnlyList<decimal> SharePercents(IReadOnlyList<decimal> amounts)
    {
        var total = amounts.Sum();
        return amounts.Select(a => SharePercent(a, total)).ToList();
    }
}
