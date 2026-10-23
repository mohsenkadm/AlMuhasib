using AlMuhasib.Infrastructure.Data.Customs;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Customs;

public static class CustomsInvoiceNumberHelper
{
    public const string Prefix = "CI";

    public static async Task<string> GetNextAsync(
        CustomsDbContext context,
        CancellationToken cancellationToken = default)
    {
        var last = await context.CustomsInvoices
            .IgnoreQueryFilters()
            .Where(i => i.InvoiceNumber.StartsWith(Prefix + "-"))
            .OrderByDescending(i => i.Id)
            .Select(i => i.InvoiceNumber)
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
