namespace AlMuhasib.Core.Entities.Hotel;

public class ReservationPayment : BaseEntity
{
    public int ReservationId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "نقد";
    public string Notes { get; set; } = string.Empty;
    public int? HotelCashBoxId { get; set; }

    public Reservation Reservation { get; set; } = null!;
    public HotelCashBox? HotelCashBox { get; set; }
}
