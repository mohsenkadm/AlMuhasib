using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel;

public sealed class HotelRatePlanService : IRatePlanService
{
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public HotelRatePlanService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<RatePlan>> GetRatePlansAsync(
        int? roomTypeId = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.RatePlans
            .Include(p => p.RoomType)
            .AsQueryable();

        if (roomTypeId.HasValue)
            query = query.Where(p => p.RoomTypeId == roomTypeId.Value);
        if (activeOnly)
            query = query.Where(p => p.IsActive);

        return await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public async Task<RatePlan?> GetRatePlanByIdAsync(
        int id,
        bool includeSeasons = true,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<RatePlan> query = context.RatePlans.Include(p => p.RoomType);
        if (includeSeasons)
            query = query.Include(p => p.Seasons);

        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<RatePlan> CreateRatePlanAsync(RatePlan ratePlan, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.RatePlans.AddAsync(ratePlan, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return ratePlan;
    }

    public async Task<RatePlan> UpdateRatePlanAsync(RatePlan ratePlan, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.RatePlans.FirstOrDefaultAsync(p => p.Id == ratePlan.Id, cancellationToken)
            ?? throw new InvalidOperationException("خطة الأسعار غير موجودة");

        existing.Name = ratePlan.Name;
        existing.RoomTypeId = ratePlan.RoomTypeId;
        existing.BasePrice = ratePlan.BasePrice;
        existing.IsActive = ratePlan.IsActive;
        existing.Notes = ratePlan.Notes;
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteRatePlanAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var ratePlan = await context.RatePlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("خطة الأسعار غير موجودة");

        ratePlan.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RatePlanSeason>> GetSeasonsAsync(
        int ratePlanId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.RatePlanSeasons
            .Where(s => s.RatePlanId == ratePlanId)
            .OrderBy(s => s.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<RatePlanSeason?> GetSeasonByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.RatePlanSeasons
            .Include(s => s.RatePlan)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<RatePlanSeason> CreateSeasonAsync(
        RatePlanSeason season,
        CancellationToken cancellationToken = default)
    {
        if (season.EndDate < season.StartDate)
            throw new InvalidOperationException("تاريخ نهاية الموسم يجب أن يكون بعد تاريخ البداية");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.RatePlanSeasons.AddAsync(season, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return season;
    }

    public async Task<RatePlanSeason> UpdateSeasonAsync(
        RatePlanSeason season,
        CancellationToken cancellationToken = default)
    {
        if (season.EndDate < season.StartDate)
            throw new InvalidOperationException("تاريخ نهاية الموسم يجب أن يكون بعد تاريخ البداية");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await context.RatePlanSeasons.FirstOrDefaultAsync(s => s.Id == season.Id, cancellationToken)
            ?? throw new InvalidOperationException("موسم الأسعار غير موجود");

        existing.Name = season.Name;
        existing.StartDate = season.StartDate;
        existing.EndDate = season.EndDate;
        existing.PricePerNight = season.PricePerNight;
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task DeleteSeasonAsync(int id, string deletedBy, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var season = await context.RatePlanSeasons.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("موسم الأسعار غير موجود");

        season.MarkSoftDeleted(deletedBy);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal?> GetPriceForDateAsync(
        int roomTypeId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await HotelReservationAmountHelper.GetPriceForDateAsync(context, roomTypeId, date, cancellationToken);
    }
}
