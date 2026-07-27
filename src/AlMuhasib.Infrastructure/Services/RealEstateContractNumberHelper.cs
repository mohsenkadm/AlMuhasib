using AlMuhasib.Infrastructure.Data.RealEstate;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public static class RealEstateContractNumberHelper
{
    private const string Prefix = "RE";

    public static async Task<string> GenerateNextAsync(RealEstateDbContext context, int? year = null)
    {
        var contractYear = year ?? DateTime.Now.Year;
        var numberPrefix = $"{Prefix}-{contractYear}-";
        var contractNumbers = await context.RealEstateContracts
            .IgnoreQueryFilters()
            .Where(c => c.ContractNumber.StartsWith(numberPrefix))
            .Select(c => c.ContractNumber)
            .ToListAsync();

        var max = 0;
        foreach (var number in contractNumbers)
        {
            if (TryParseSequence(number, numberPrefix, out var sequence))
                max = Math.Max(max, sequence);
        }

        return $"{numberPrefix}{(max + 1):D5}";
    }

    private static bool TryParseSequence(string contractNumber, string numberPrefix, out int sequence)
    {
        sequence = 0;
        if (!contractNumber.StartsWith(numberPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var suffix = contractNumber[numberPrefix.Length..];
        return int.TryParse(suffix, out sequence) && sequence > 0;
    }
}
