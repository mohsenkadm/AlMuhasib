namespace AlMuhasib.UI.Services;

/// <summary>
/// جسر تنقل بين شاشات الفندق — يمرّر معرف الكيان المطلوب تحديده عند فتح تبويب.
/// </summary>
public static class HotelNavigationBridge
{
    public static int? PendingReservationId { get; set; }
    public static int? PendingEditReservationId { get; set; }
    public static int? PendingGuestId { get; set; }
    public static int? PendingRoomId { get; set; }
}
