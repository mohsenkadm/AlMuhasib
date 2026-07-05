using AlMuhasib.UI.ViewModels.Hotel;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Services;

public sealed class HotelEntityNavigationHelper
{
    private readonly MainWindowViewModel _mainWindow;

    public HotelEntityNavigationHelper(MainWindowViewModel mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public Task OpenGuestsAsync(int? guestId = null)
    {
        HotelNavigationBridge.PendingGuestId = guestId;
        return _mainWindow.OpenTabAsync(typeof(HotelGuestsViewModel), "النزلاء", PackIconKind.AccountGroup);
    }

    public Task OpenRoomsAsync(int? roomId = null)
    {
        HotelNavigationBridge.PendingRoomId = roomId;
        return _mainWindow.OpenTabAsync(typeof(HotelRoomsViewModel), "الغرف", PackIconKind.Door);
    }

    public Task OpenReservationsAsync(int? reservationId = null)
    {
        HotelNavigationBridge.PendingReservationId = reservationId;
        return _mainWindow.OpenTabAsync(typeof(HotelReservationsViewModel), "الحجوزات", PackIconKind.CalendarClock);
    }

    public Task OpenEditReservationAsync(int reservationId, string reservationNumber)
    {
        HotelNavigationBridge.PendingEditReservationId = reservationId;
        return _mainWindow.OpenTabAsync(
            typeof(HotelReservationFormViewModel),
            $"تعديل {reservationNumber}",
            PackIconKind.FileDocumentEdit,
            activateIfExists: false);
    }

    public Task OpenCheckInOutAsync() =>
        _mainWindow.OpenTabAsync(typeof(HotelCheckInOutViewModel), "تسجيل دخول/خروج", PackIconKind.Login);
}
