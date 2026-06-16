using AlMuhasib.Core.Entities.Hotel;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IRatePlanService
{
    Task<IReadOnlyList<RatePlan>> GetRatePlansAsync(int? roomTypeId = null, bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<RatePlan?> GetRatePlanByIdAsync(int id, bool includeSeasons = true, CancellationToken cancellationToken = default);
    Task<RatePlan> CreateRatePlanAsync(RatePlan ratePlan, CancellationToken cancellationToken = default);
    Task<RatePlan> UpdateRatePlanAsync(RatePlan ratePlan, CancellationToken cancellationToken = default);
    Task DeleteRatePlanAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RatePlanSeason>> GetSeasonsAsync(int ratePlanId, CancellationToken cancellationToken = default);
    Task<RatePlanSeason?> GetSeasonByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<RatePlanSeason> CreateSeasonAsync(RatePlanSeason season, CancellationToken cancellationToken = default);
    Task<RatePlanSeason> UpdateSeasonAsync(RatePlanSeason season, CancellationToken cancellationToken = default);
    Task DeleteSeasonAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    Task<decimal?> GetPriceForDateAsync(int roomTypeId, DateTime date, CancellationToken cancellationToken = default);
}
