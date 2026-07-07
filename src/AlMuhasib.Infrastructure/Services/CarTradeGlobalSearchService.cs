using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.Infrastructure.Data.CarTrade;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class CarTradeGlobalSearchService : IGlobalSearchService
{
    private readonly IDbContextFactory<CarTradeDbContext> _contextFactory;

    public CarTradeGlobalSearchService(IDbContextFactory<CarTradeDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<GlobalSearchHit>> SearchAsync(
        string term,
        int maxResults = 30,
        CancellationToken cancellationToken = default)
    {
        term = term?.Trim() ?? string.Empty;
        if (term.Length < 2)
            return [];

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var like = $"%{term}%";

        return await context.CarTradeTransactions.AsNoTracking()
            .Where(t =>
                EF.Functions.Like(t.TransactionNumber, like) ||
                EF.Functions.Like(t.CarName, like) ||
                EF.Functions.Like(t.SellerName, like) ||
                EF.Functions.Like(t.BuyerName, like) ||
                EF.Functions.Like(t.PlateNumber, like) ||
                EF.Functions.Like(t.ChassisNumber, like))
            .OrderByDescending(t => t.TransactionDate)
            .Take(maxResults)
            .Select(t => new GlobalSearchHit
            {
                Kind = GlobalSearchKind.Customer,
                EntityId = t.Id,
                Title = t.TransactionNumber,
                Subtitle = $"{t.CarName} — {CarTradeService.GetTradeTypeLabel(t.TradeType)}",
                ScreenName = "CarTradeList"
            })
            .ToListAsync(cancellationToken);
    }
}
