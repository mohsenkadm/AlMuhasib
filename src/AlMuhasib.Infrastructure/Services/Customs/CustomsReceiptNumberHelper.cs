using AlMuhasib.Infrastructure.Data.Customs;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Customs;

public static class CustomsReceiptNumberHelper
{
    public const string Prefix = "CR";

    public static async Task<string> GetNextAsync(
        CustomsDbContext context,
        CancellationToken cancellationToken = default)
    {
        var last = await context.CustomsReceipts
            .IgnoreQueryFilters()
            .Where(r => r.ReceiptNumber.StartsWith(Prefix + "-"))
            .OrderByDescending(r => r.Id)
            .Select(r => r.ReceiptNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextNum = 1;
        if (last is not null)
        {
            var parts = last.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var lastNum))
                nextNum = lastNum + 1;
        }

        return $"{Prefix}-{nextNum:D4}";
    }
}
