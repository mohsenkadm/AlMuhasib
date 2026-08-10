using AlMuhasib.Core.Enums;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

/// <summary>توليد أرقام فواتير متسلسلة دون إعادة استخدام أرقام الفواتير المحذوفة.</summary>
public static class InvoiceNumberHelper
{
    public static string GetPrefix(InvoiceType type) => type switch
    {
        InvoiceType.Purchase => "PUR",
        InvoiceType.PurchaseReturn => "PRT",
        InvoiceType.Sale => "SAL",
        InvoiceType.SaleReturn => "SRT",
        InvoiceType.Installment => "INS",
        _ => "INV"
    };

    public static async Task<string> GenerateNextAsync(AppDbContext context, InvoiceType type, int? year = null)
    {
        var prefix = GetPrefix(type);
        var invoiceYear = year ?? DateTime.Now.Year;
        var maxSequence = await GetMaxSequenceAsync(context, type, prefix, invoiceYear);
        return $"{prefix}-{invoiceYear}-{(maxSequence + 1):D5}";
    }

    internal static async Task<int> GetMaxSequenceAsync(
        AppDbContext context, InvoiceType type, string prefix, int year)
    {
        var numberPrefix = $"{prefix}-{year}-";

        var invoiceNumbers = await context.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.InvoiceType == type && i.InvoiceNumber.StartsWith(numberPrefix))
            .Select(i => i.InvoiceNumber)
            .ToListAsync();

        var max = 0;
        foreach (var invoiceNumber in invoiceNumbers)
        {
            if (TryParseSequence(invoiceNumber, numberPrefix, out var sequence))
                max = Math.Max(max, sequence);
        }

        return max;
    }

    private static bool TryParseSequence(string invoiceNumber, string numberPrefix, out int sequence)
    {
        sequence = 0;
        if (!invoiceNumber.StartsWith(numberPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var suffix = invoiceNumber[numberPrefix.Length..];
        return int.TryParse(suffix, out sequence) && sequence > 0;
    }
}
