using AlMuhasib.Core.Entities.Hotel;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IReservationPaymentService
{
    Task<ReservationPayment> AddPaymentAsync(
        int reservationId,
        decimal amount,
        DateTime paymentDate,
        string paymentMethod,
        int? cashBoxId = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationPayment>> GetPaymentsAsync(
        int reservationId,
        CancellationToken cancellationToken = default);
}
