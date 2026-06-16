using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities.Hotel;

public class Reservation : BaseEntity
{
    public string ReservationNumber { get; set; } = string.Empty;
    public int GuestId { get; set; }
    public int? RoomId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public DateTime? ActualCheckIn { get; set; }
    public DateTime? ActualCheckOut { get; set; }
    public int GuestCount { get; set; } = 1;
    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Notes { get; set; } = string.Empty;

    public Guest Guest { get; set; } = null!;
    public Room? Room { get; set; }
    public ICollection<ReservationCharge> Charges { get; set; } = [];
    public ICollection<ReservationPayment> Payments { get; set; } = [];
}
