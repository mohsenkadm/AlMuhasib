using System.Globalization;
using System.Windows.Data;
using AlMuhasib.Core.Enums;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.Converters;

public sealed class ReservationStatusLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ReservationStatus status
            ? HotelDisplayHelper.GetReservationStatusLabel(status)
            : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class ReservationStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ReservationStatus status
            ? HotelDisplayHelper.GetReservationStatusColor(status)
            : "#616161";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class RoomStatusLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is RoomStatus status
            ? HotelDisplayHelper.GetRoomStatusLabel(status)
            : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class RoomStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is RoomStatus status
            ? HotelDisplayHelper.GetRoomStatusColor(status)
            : "#616161";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class HousekeepingStatusLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is HousekeepingStatus status
            ? HotelDisplayHelper.GetHousekeepingStatusLabel(status)
            : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class HousekeepingStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is HousekeepingStatus status
            ? HotelDisplayHelper.GetHousekeepingStatusColor(status)
            : "#616161";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
