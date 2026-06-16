using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

public static class ReservationNumberHelper
{
    private const string Prefix = "RSV";

    public static async Task<string> GenerateNextAsync(HotelDbContext context, int? year = null)
    {
        var reservationYear = year ?? DateTime.Now.Year;
        var numberPrefix = $"{Prefix}-{reservationYear}-";
        var reservationNumbers = await context.Reservations
            .IgnoreQueryFilters()
            .Where(r => r.ReservationNumber.StartsWith(numberPrefix))
            .Select(r => r.ReservationNumber)
            .ToListAsync();

        var max = 0;
        foreach (var number in reservationNumbers)
        {
            if (TryParseSequence(number, numberPrefix, out var sequence))
                max = Math.Max(max, sequence);
        }

        return $"{numberPrefix}{(max + 1):D5}";
    }

    private static bool TryParseSequence(string reservationNumber, string numberPrefix, out int sequence)
    {
        sequence = 0;
        if (!reservationNumber.StartsWith(numberPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var suffix = reservationNumber[numberPrefix.Length..];
        return int.TryParse(suffix, out sequence) && sequence > 0;
    }
}
