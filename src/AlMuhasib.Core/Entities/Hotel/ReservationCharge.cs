namespace AlMuhasib.Core.Entities.Hotel;

public class ReservationCharge : BaseEntity
{
    public int ReservationId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ChargeDate { get; set; } = DateTime.Today;
    public string Notes { get; set; } = string.Empty;

    public Reservation Reservation { get; set; } = null!;
}
