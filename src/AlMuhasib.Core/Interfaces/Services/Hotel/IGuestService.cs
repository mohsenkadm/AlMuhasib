using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Models.Hotel;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IGuestService
{
    Task<Guest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<GuestListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GuestListItem>> SearchAsync(string term, int maxResults = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReservationListItem>> GetReservationsByGuestIdAsync(int guestId, int maxResults = 10, CancellationToken cancellationToken = default);
    Task<Guest> CreateAsync(Guest guest, CancellationToken cancellationToken = default);
    Task<Guest> UpdateAsync(Guest guest, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default);
}
