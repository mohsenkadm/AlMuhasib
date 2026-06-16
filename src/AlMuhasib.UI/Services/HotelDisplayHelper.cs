using AlMuhasib.Core.Enums;

namespace AlMuhasib.UI.Services;

public static class HotelDisplayHelper
{
    public static string GetReservationStatusLabel(ReservationStatus status) => status switch
    {
        ReservationStatus.Confirmed => "مؤكد",
        ReservationStatus.CheckedIn => "مسجل",
        ReservationStatus.CheckedOut => "غادر",
        ReservationStatus.Cancelled => "ملغى",
        ReservationStatus.NoShow => "لم يحضر",
        _ => status.ToString()
    };

    public static string GetReservationStatusColor(ReservationStatus status) => status switch
    {
        ReservationStatus.Confirmed => "#1565C0",
        ReservationStatus.CheckedIn => "#2E7D32",
        ReservationStatus.CheckedOut => "#616161",
        ReservationStatus.Cancelled => "#C62828",
        ReservationStatus.NoShow => "#F57C00",
        _ => "#616161"
    };

    public static string GetRoomStatusLabel(RoomStatus status) => status switch
    {
        RoomStatus.Available => "متاحة",
        RoomStatus.Occupied => "مشغولة",
        RoomStatus.Dirty => "تحتاج تنظيف",
        RoomStatus.Maintenance => "صيانة",
        RoomStatus.OutOfOrder => "خارج الخدمة",
        _ => status.ToString()
    };

    public static string GetRoomStatusColor(RoomStatus status) => status switch
    {
        RoomStatus.Available => "#2E7D32",
        RoomStatus.Occupied => "#1565C0",
        RoomStatus.Dirty => "#F57C00",
        RoomStatus.Maintenance => "#6A1B9A",
        RoomStatus.OutOfOrder => "#C62828",
        _ => "#616161"
    };

    public static string GetHousekeepingStatusLabel(HousekeepingStatus status) => status switch
    {
        HousekeepingStatus.Pending => "معلق",
        HousekeepingStatus.InProgress => "جاري",
        HousekeepingStatus.Done => "منجز",
        HousekeepingStatus.Verified => "تم التحقق",
        _ => status.ToString()
    };

    public static string GetHousekeepingStatusColor(HousekeepingStatus status) => status switch
    {
        HousekeepingStatus.Pending => "#F57C00",
        HousekeepingStatus.InProgress => "#1565C0",
        HousekeepingStatus.Done => "#2E7D32",
        HousekeepingStatus.Verified => "#6A1B9A",
        _ => "#616161"
    };

    public static string GetVoucherTypeLabel(HotelVoucherType type) => type switch
    {
        HotelVoucherType.Receipt => "قبض",
        HotelVoucherType.Payment => "صرف",
        _ => type.ToString()
    };
}
