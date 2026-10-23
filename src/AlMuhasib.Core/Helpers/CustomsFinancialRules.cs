using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Helpers;

/// <summary>
/// Non-bypassable Customs financial invariants:
/// every amount has a currency; never mix IQD+USD; remaining = total − allocated;
/// internal profit = revenue − same-currency expenses (not customer balance, not receipts).
/// </summary>
public static class CustomsFinancialRules
{
    public static decimal NormalizeFxRate(AccountingCurrency currency, decimal fxRate, string? context = null) =>
        AccountingCurrencyRules.RequireFxRateOrThrow(currency, fxRate, context);

    public static void EnsureVehicleCurrencyMatchesInvoice(
        AccountingCurrency vehicleCurrency,
        AccountingCurrency invoiceCurrency) =>
        AccountingCurrencyRules.EnsureSameCurrency(
            vehicleCurrency, invoiceCurrency, "السيارة", "الفاتورة");

    public static void EnsureReceiptMatchesInvoiceCurrency(
        AccountingCurrency receiptCurrency,
        AccountingCurrency invoiceCurrency) =>
        AccountingCurrencyRules.EnsureSameCurrency(
            receiptCurrency, invoiceCurrency, "الوصل", "الفاتورة");

    public static decimal ComputeRemaining(decimal totalAmount, decimal paidAmount) =>
        Math.Max(0m, totalAmount - paidAmount);

    /// <summary>
    /// Internal profit for one invoice: revenue − expenses in the same currency only.
    /// Other-currency expenses are excluded (they are not folded into this profit).
    /// </summary>
    public static decimal ComputeInternalProfit(
        decimal invoiceTotal,
        AccountingCurrency invoiceCurrency,
        IEnumerable<(AccountingCurrency Currency, decimal Amount)> expenses)
    {
        var sameCurrencyExpenses = expenses
            .Where(e => e.Currency == invoiceCurrency)
            .Sum(e => e.Amount);
        return invoiceTotal - sameCurrencyExpenses;
    }

    public static decimal ComputeInternalProfit(
        decimal invoiceTotal,
        AccountingCurrency invoiceCurrency,
        IEnumerable<(AccountingCurrency Currency, decimal Amount)> expenses,
        out decimal sameCurrencyExpenseSum)
    {
        sameCurrencyExpenseSum = expenses
            .Where(e => e.Currency == invoiceCurrency)
            .Sum(e => e.Amount);
        return invoiceTotal - sameCurrencyExpenseSum;
    }

    /// <summary>
    /// Chart/report builders must never pass a single Amount that is IQD+USD.
    /// Prefer dual KPIs or one currency series.
    /// </summary>
    public static void AssertSingleCurrencyChartAmount(string chartName, bool mixesCurrencies)
    {
        if (mixesCurrencies)
            throw new InvalidOperationException(
                $"لا يُسمح بجمع الدينار والدولار مباشرة في «{chartName}». استخدم أعمدة/سلاسل منفصلة لكل عملة.");
    }
}
