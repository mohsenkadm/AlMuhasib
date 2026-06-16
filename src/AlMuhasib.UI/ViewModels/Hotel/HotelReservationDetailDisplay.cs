using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.ViewModels.Hotel;

public sealed class HotelReservationDetailDisplay
{
    public int Id { get; init; }
    public int? RoomId { get; init; }
    public string ReservationNumber { get; init; } = string.Empty;
    public string GuestName { get; init; } = string.Empty;
    public string GuestPhone { get; init; } = string.Empty;
    public string RoomNumber { get; init; } = string.Empty;
    public string RoomTypeName { get; init; } = string.Empty;
    public string CheckInDate { get; init; } = string.Empty;
    public string CheckOutDate { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string GuestCount { get; init; } = string.Empty;
    public string TotalAmount { get; init; } = string.Empty;
    public string AmountPaid { get; init; } = string.Empty;
    public string RemainingAmount { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;

    public static HotelReservationDetailDisplay FromEntity(Reservation r) => new()
    {
        Id = r.Id,
        RoomId = r.RoomId,
        ReservationNumber = r.ReservationNumber,
        GuestName = Display(r.Guest?.FullName),
        GuestPhone = Display(r.Guest?.Phone),
        RoomNumber = Display(r.Room?.RoomNumber),
        RoomTypeName = Display(r.Room?.RoomType?.Name),
        CheckInDate = r.CheckInDate.ToString("yyyy/MM/dd"),
        CheckOutDate = r.CheckOutDate.ToString("yyyy/MM/dd"),
        Status = HotelDisplayHelper.GetReservationStatusLabel(r.Status),
        GuestCount = r.GuestCount.ToString(),
        TotalAmount = r.TotalAmount.ToString("N0"),
        AmountPaid = r.AmountPaid.ToString("N0"),
        RemainingAmount = r.RemainingAmount.ToString("N0"),
        Notes = Display(r.Notes)
    };

    private static string Display(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
}

public sealed class HotelRoomListDisplay
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string FloorName { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#616161";
    public string CurrentGuestName { get; set; } = string.Empty;
    public RoomStatus Status { get; set; }
    public RoomStatus PendingStatus { get; set; }
}
