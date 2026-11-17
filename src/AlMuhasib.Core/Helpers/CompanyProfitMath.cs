namespace AlMuhasib.Core.Helpers;

/// <summary>
/// معادلات ربح الشركة. الإيرادات الأخرى تُضاف بعد مجمل الربح ولا تدخل في المبيعات.
/// عند صفر تبقى النتائج مطابقة للمعادلات السابقة.
/// </summary>
public static class CompanyProfitMath
{
    public static decimal CardNet(decimal grossProfit, decimal otherIncome, decimal expenses)
        => grossProfit + otherIncome - expenses;

    public static decimal Operating(decimal grossProfit, decimal otherIncome, decimal expenses, decimal bankFees)
        => grossProfit + otherIncome - expenses - bankFees;

    public static decimal FullNet(
        decimal grossProfit,
        decimal otherIncome,
        decimal expenses,
        decimal bankFees,
        decimal distributed,
        decimal profitOpening)
        => Operating(grossProfit, otherIncome, expenses, bankFees) - distributed + profitOpening;
}
