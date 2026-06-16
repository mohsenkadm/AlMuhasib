using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.Infrastructure.Data.Car;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class CarGlobalSearchService : IGlobalSearchService
{
    private readonly IDbContextFactory<CarDbContext> _contextFactory;

    public CarGlobalSearchService(IDbContextFactory<CarDbContext> contextFactory)
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

        return await context.CarSaleContracts.AsNoTracking()
            .Where(c =>
                EF.Functions.Like(c.ContractNumber, like) ||
                EF.Functions.Like(c.SellerName, like) ||
                EF.Functions.Like(c.BuyerName, like) ||
                EF.Functions.Like(c.PlateNumber, like) ||
                EF.Functions.Like(c.ChassisNumber, like))
            .OrderByDescending(c => c.ContractDate)
            .Take(maxResults)
            .Select(c => new GlobalSearchHit
            {
                Kind = GlobalSearchKind.Customer,
                EntityId = c.Id,
                Title = c.ContractNumber,
                Subtitle = $"{c.SellerName} → {c.BuyerName}",
                ScreenName = CarPermissionRegistryScreen.CarContracts
            })
            .ToListAsync(cancellationToken);
    }
}

internal static class CarPermissionRegistryScreen
{
    public const string CarContracts = "CarContracts";
}
