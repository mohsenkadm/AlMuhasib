using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities.Hotel.Restaurant;

public class RestaurantOrderPayment : BaseEntity
{
    public int RestaurantOrderId { get; set; }
    public decimal Amount { get; set; }
    public RestaurantPaymentMethod PaymentMethod { get; set; }
    public int? HotelCashBoxId { get; set; }
    public string Notes { get; set; } = string.Empty;

    public RestaurantOrder Order { get; set; } = null!;
    public HotelCashBox? HotelCashBox { get; set; }
}
