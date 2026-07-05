namespace AlMuhasib.UI.Services;

[Obsolete("Use HotelNavigationBridge instead.")]
public static class HotelReservationNavigationBridge
{
    public static int? PendingEditReservationId
    {
        get => HotelNavigationBridge.PendingEditReservationId;
        set => HotelNavigationBridge.PendingEditReservationId = value;
    }
}
