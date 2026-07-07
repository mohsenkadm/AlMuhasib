using AlMuhasib.Infrastructure.Data.CarTrade;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public static class CarTradeNumberHelper
{
    private const string Prefix = "TRD";

    public static async Task<string> GenerateNextAsync(CarTradeDbContext context, int? year = null)
    {
        var transactionYear = year ?? DateTime.Now.Year;
        var numberPrefix = $"{Prefix}-{transactionYear}-";
        var numbers = await context.CarTradeTransactions
            .IgnoreQueryFilters()
            .Where(t => t.TransactionNumber.StartsWith(numberPrefix))
            .Select(t => t.TransactionNumber)
            .ToListAsync();

        var max = 0;
        foreach (var number in numbers)
        {
            if (TryParseSequence(number, numberPrefix, out var sequence))
                max = Math.Max(max, sequence);
        }

        return $"{numberPrefix}{(max + 1):D5}";
    }

    private static bool TryParseSequence(string transactionNumber, string numberPrefix, out int sequence)
    {
        sequence = 0;
        if (!transactionNumber.StartsWith(numberPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var suffix = transactionNumber[numberPrefix.Length..];
        return int.TryParse(suffix, out sequence) && sequence > 0;
    }
}
