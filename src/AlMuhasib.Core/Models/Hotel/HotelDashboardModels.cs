using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Core.Models.Hotel;

public class HotelDashboardStats
{
    public int TotalRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int DirtyRooms { get; set; }
    public int MaintenanceRooms { get; set; }
    public decimal OccupancyRate { get; set; }

    public int TodayArrivals { get; set; }
    public int TodayDepartures { get; set; }
    public int InHouseGuests { get; set; }
    public int PendingHousekeepingTasks { get; set; }

    public decimal TodayRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public decimal OutstandingBalances { get; set; }

    public List<HotelNameCountPoint> RoomStatusChart { get; set; } = [];
    public List<DailyAmountPoint> RevenueTrend { get; set; } = [];
    public List<NameAmountPoint> RevenueByRoomType { get; set; } = [];
    public List<ReservationListItem> TodayArrivalList { get; set; } = [];
    public List<ReservationListItem> TodayDepartureList { get; set; } = [];
    public List<ReservationListItem> RecentReservations { get; set; } = [];
}

public class HotelNameCountPoint
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
