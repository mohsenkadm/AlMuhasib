using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities.Hotel.Restaurant;

public class RestaurantOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public RestaurantOrderType OrderType { get; set; }
    public RestaurantOrderStatus Status { get; set; } = RestaurantOrderStatus.Draft;
    public RestaurantKitchenStatus KitchenStatus { get; set; } = RestaurantKitchenStatus.Pending;
    public int? RestaurantTableId { get; set; }
    public int? ReservationId { get; set; }
    public int? RoomId { get; set; }
    public int? GuestId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal CogsAmount { get; set; }
    public decimal GrossProfit { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public DateTime? PaidAt { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int? ReservationChargeId { get; set; }

    public RestaurantTable? Table { get; set; }
    public Reservation? Reservation { get; set; }
    public Room? Room { get; set; }
    public Guest? Guest { get; set; }
    public ReservationCharge? ReservationCharge { get; set; }
    public ICollection<RestaurantOrderLine> Lines { get; set; } = [];
    public ICollection<RestaurantOrderPayment> Payments { get; set; } = [];
}
