using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.UI.Services;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Hotel;

public sealed class HotelReservationDetailDisplay
{
    public int Id { get; init; }
    public int GuestId { get; init; }
    public int? RoomId { get; init; }
    public string ReservationNumber { get; init; } = string.Empty;
    public string GuestName { get; init; } = string.Empty;
    public string GuestPhone { get; init; } = string.Empty;
    public string GuestEmail { get; init; } = string.Empty;
    public string RoomNumber { get; init; } = string.Empty;
    public string RoomTypeName { get; init; } = string.Empty;
    public string CheckInDate { get; init; } = string.Empty;
    public string CheckOutDate { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusColor { get; init; } = "#616161";
    public ReservationStatus StatusValue { get; init; }
    public string GuestCount { get; init; } = string.Empty;
    public string TotalAmount { get; init; } = string.Empty;
    public string AmountPaid { get; init; } = string.Empty;
    public string RemainingAmount { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public ObservableCollection<HotelReservationChargeDisplay> Charges { get; init; } = [];
    public ObservableCollection<HotelReservationPaymentDisplay> Payments { get; init; } = [];

    public static HotelReservationDetailDisplay FromEntity(Reservation r) => new()
    {
        Id = r.Id,
        GuestId = r.GuestId,
        RoomId = r.RoomId,
        ReservationNumber = r.ReservationNumber,
        GuestName = Display(r.Guest?.FullName),
        GuestPhone = Display(r.Guest?.Phone),
        GuestEmail = Display(r.Guest?.Email),
        RoomNumber = Display(r.Room?.RoomNumber),
        RoomTypeName = Display(r.Room?.RoomType?.Name),
        CheckInDate = r.CheckInDate.ToString("yyyy/MM/dd"),
        CheckOutDate = r.CheckOutDate.ToString("yyyy/MM/dd"),
        Status = HotelDisplayHelper.GetReservationStatusLabel(r.Status),
        StatusColor = HotelDisplayHelper.GetReservationStatusColor(r.Status),
        StatusValue = r.Status,
        GuestCount = r.GuestCount.ToString(),
        TotalAmount = r.TotalAmount.ToString("N0"),
        AmountPaid = r.AmountPaid.ToString("N0"),
        RemainingAmount = r.RemainingAmount.ToString("N0"),
        Notes = Display(r.Notes),
        Charges = new ObservableCollection<HotelReservationChargeDisplay>(
            r.Charges.OrderBy(c => c.Id).Select(HotelReservationChargeDisplay.FromEntity)),
        Payments = new ObservableCollection<HotelReservationPaymentDisplay>(
            r.Payments.OrderByDescending(p => p.PaymentDate).Select(HotelReservationPaymentDisplay.FromEntity))
    };

    private static string Display(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
}

public sealed class HotelReservationChargeDisplay
{
    public string Description { get; init; } = string.Empty;
    public string Amount { get; init; } = string.Empty;

    public static HotelReservationChargeDisplay FromEntity(ReservationCharge charge) => new()
    {
        Description = string.IsNullOrWhiteSpace(charge.Description) ? "—" : charge.Description,
        Amount = charge.Amount.ToString("N0")
    };
}

public sealed class HotelReservationPaymentDisplay
{
    public string PaymentDate { get; init; } = string.Empty;
    public string Amount { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;

    public static HotelReservationPaymentDisplay FromEntity(ReservationPayment payment) => new()
    {
        PaymentDate = payment.PaymentDate.ToString("yyyy/MM/dd"),
        Amount = payment.Amount.ToString("N0"),
        PaymentMethod = string.IsNullOrWhiteSpace(payment.PaymentMethod) ? "—" : payment.PaymentMethod
    };
}

public sealed class HotelGuestDetailDisplay
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string IdNumber { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public int ReservationCount { get; init; }
    public ObservableCollection<ReservationListItem> RecentReservations { get; init; } = [];

    public static HotelGuestDetailDisplay FromGuest(Guest guest, IEnumerable<ReservationListItem> reservations) => new()
    {
        Id = guest.Id,
        FullName = guest.FullName,
        Phone = Display(guest.Phone),
        IdNumber = Display(guest.IdNumber),
        Email = Display(guest.Email),
        Notes = Display(guest.Notes),
        ReservationCount = reservations.Count(),
        RecentReservations = new ObservableCollection<ReservationListItem>(reservations)
    };

    private static string Display(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
}

public sealed class HotelRoomDetailDisplay
{
    public int Id { get; init; }
    public string RoomNumber { get; init; } = string.Empty;
    public string FloorName { get; init; } = string.Empty;
    public string RoomTypeName { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string StatusColor { get; init; } = "#616161";
    public string CurrentGuestName { get; init; } = string.Empty;
    public int? CurrentGuestId { get; init; }
    public int? CurrentReservationId { get; init; }
    public string Notes { get; init; } = string.Empty;

    public static HotelRoomDetailDisplay FromRoom(Room room, RoomListItem? listItem = null) => new()
    {
        Id = room.Id,
        RoomNumber = room.RoomNumber,
        FloorName = room.Floor?.Name ?? "—",
        RoomTypeName = room.RoomType?.Name ?? "—",
        StatusLabel = HotelDisplayHelper.GetRoomStatusLabel(room.Status),
        StatusColor = HotelDisplayHelper.GetRoomStatusColor(room.Status),
        CurrentGuestName = listItem?.CurrentGuestName ?? "—",
        CurrentGuestId = listItem?.CurrentGuestId,
        CurrentReservationId = listItem?.CurrentReservationId,
        Notes = Display(room.Notes)
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
    public int? CurrentGuestId { get; set; }
    public int? CurrentReservationId { get; set; }
    public RoomStatus Status { get; set; }
    public RoomStatus PendingStatus { get; set; }
}
