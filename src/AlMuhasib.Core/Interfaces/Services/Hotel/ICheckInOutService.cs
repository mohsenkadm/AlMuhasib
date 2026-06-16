using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Models.Hotel;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface ICheckInOutService
{
    Task<Reservation> CheckInAsync(
        int reservationId,
        int? roomId = null,
        DateTime? checkInTime = null,
        string? checkedInBy = null,
        CancellationToken cancellationToken = default);

    Task<Reservation> CheckOutAsync(
        int reservationId,
        DateTime? checkOutTime = null,
        string? checkedOutBy = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationListItem>> GetTodayArrivalsAsync(
        DateTime? date = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationListItem>> GetTodayDeparturesAsync(
        DateTime? date = null,
        CancellationToken cancellationToken = default);
}
