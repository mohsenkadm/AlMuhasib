using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities.Hotel;

public class HotelVoucher : BaseEntity
{
    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    public HotelVoucherType Type { get; set; }
    public decimal Amount { get; set; }
    public int HotelCashBoxId { get; set; }
    public int? ReservationId { get; set; }
    public int? HotelExpenseId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public HotelCashBox HotelCashBox { get; set; } = null!;
    public Reservation? Reservation { get; set; }
    public HotelExpense? HotelExpense { get; set; }
}
