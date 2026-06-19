using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Models.Hotel;

public sealed class RestaurantProfitSummary
{
    public decimal Revenue { get; set; }
    public decimal Cogs { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPercent { get; set; }
    public int OrderCount { get; set; }
}

public sealed class RestaurantChannelSales
{
    public RestaurantOrderType OrderType { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public sealed class RestaurantTopItem
{
    public string ItemName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class RestaurantFinancialOverview
{
    public decimal RestaurantRevenue { get; set; }
    public decimal RestaurantCogs { get; set; }
    public decimal RestaurantGrossProfit { get; set; }
    public decimal HotelExpenses { get; set; }
    public decimal NetOperating { get; set; }
}

public sealed class ActiveRoomForService
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int ReservationId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public int GuestId { get; set; }
}
