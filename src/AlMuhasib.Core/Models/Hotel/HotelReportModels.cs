using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Core.Models.Hotel;

public class HotelReportFilter
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? RoomTypeId { get; set; }
    public int? FloorId { get; set; }
    public ReservationStatus? Status { get; set; }
}

public class ReservationFilter
{
    public string? SearchText { get; set; }
    public DateTime? CheckInFrom { get; set; }
    public DateTime? CheckInTo { get; set; }
    public DateTime? CheckOutFrom { get; set; }
    public DateTime? CheckOutTo { get; set; }
    public ReservationStatus? Status { get; set; }
    public int? RoomId { get; set; }
    public int? GuestId { get; set; }
    public bool UnpaidOnly { get; set; }
}

public class HotelVoucherFilter
{
    public HotelVoucherType? Type { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? CashBoxId { get; set; }
    public int? ReservationId { get; set; }
}

public class HousekeepingTaskFilter
{
    public HousekeepingStatus? Status { get; set; }
    public int? RoomId { get; set; }
    public int? FloorId { get; set; }
    public string? AssignedTo { get; set; }
}

public class BulkAddRoomsRequest
{
    public int FloorId { get; set; }
    public int RoomTypeId { get; set; }
    public string NumberPrefix { get; set; } = string.Empty;
    public int FromNumber { get; set; }
    public int ToNumber { get; set; }
    public RoomStatus InitialStatus { get; set; } = RoomStatus.Available;
}

public class GuestListItem
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
}

public class ReservationListItem
{
    public int Id { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string? RoomNumber { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public DateTime? ActualCheckIn { get; set; }
    public DateTime? ActualCheckOut { get; set; }
    public int GuestCount { get; set; }
    public ReservationStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class RoomListItem
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string FloorName { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public RoomStatus Status { get; set; }
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public string? CurrentGuestName { get; set; }
}

public class OccupancyReportData
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int TotalRoomNights { get; set; }
    public int SoldRoomNights { get; set; }
    public decimal AverageOccupancyRate { get; set; }
    public List<OccupancyReportRow> Rows { get; set; } = [];
    public List<DailyAmountPoint> DailyOccupancyChart { get; set; } = [];
    public List<NameAmountPoint> ByRoomTypeChart { get; set; } = [];
}

public class OccupancyReportRow
{
    public DateTime Date { get; set; }
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int AvailableRooms { get; set; }
    public decimal OccupancyRate { get; set; }
    public int Arrivals { get; set; }
    public int Departures { get; set; }
}

public class RevenueReportData
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public decimal TotalRoomRevenue { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal TotalCharges { get; set; }
    public decimal OutstandingBalance { get; set; }
    public List<RevenueReportRow> Rows { get; set; } = [];
    public List<DailyAmountPoint> DailyRevenueChart { get; set; } = [];
    public List<NameAmountPoint> ByRoomTypeChart { get; set; } = [];
    public List<NameAmountPoint> ByPaymentMethodChart { get; set; } = [];
}

public class RevenueReportRow
{
    public DateTime Date { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public int Nights { get; set; }
    public decimal RoomRevenue { get; set; }
    public decimal ExtraCharges { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
}

public class NightAuditReportData
{
    public DateTime AuditDate { get; set; }
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int ArrivalsToday { get; set; }
    public int DeparturesToday { get; set; }
    public int NoShows { get; set; }
    public int WalkIns { get; set; }
    public decimal RoomRevenue { get; set; }
    public decimal PaymentsCollected { get; set; }
    public decimal ExpensesPosted { get; set; }
    public decimal CashOnHand { get; set; }
    public List<NightAuditReservationRow> InHouseGuests { get; set; } = [];
    public List<NightAuditReservationRow> ExpectedArrivals { get; set; } = [];
    public List<NightAuditReservationRow> ExpectedDepartures { get; set; } = [];
    public List<NightAuditCashBoxRow> CashBoxBalances { get; set; } = [];
}

public class NightAuditReservationRow
{
    public int ReservationId { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public ReservationStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}

public class NightAuditCashBoxRow
{
    public int CashBoxId { get; set; }
    public string CashBoxName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal Receipts { get; set; }
    public decimal Payments { get; set; }
    public decimal ClosingBalance { get; set; }
}
