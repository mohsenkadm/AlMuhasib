using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Models.Hotel;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IReservationService
{
    Task<Reservation?> GetByIdAsync(int id, bool includeDetails = true, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ReservationListItem> Items, int TotalCount)> SearchPagedAsync(
        ReservationFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Reservation> CreateAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task<Reservation> UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task CancelAsync(int id, string cancelledBy, string? reason = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default);
    Task<string> GenerateReservationNumberAsync(CancellationToken cancellationToken = default);
}
