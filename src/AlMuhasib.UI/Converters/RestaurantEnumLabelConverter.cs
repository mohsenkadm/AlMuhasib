using System.Globalization;
using System.Windows.Data;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.UI.Converters;

public sealed class RestaurantEnumLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return string.Empty;

        return value switch
        {
            RestaurantOrderType t => t switch
            {
                RestaurantOrderType.DineIn => "صالة",
                RestaurantOrderType.Takeaway => "سفري",
                RestaurantOrderType.RoomService => "خدمة غرف",
                _ => t.ToString()
            },
            RestaurantTableStatus s => s switch
            {
                RestaurantTableStatus.Available => "متاحة",
                RestaurantTableStatus.Occupied => "مشغولة",
                _ => s.ToString()
            },
            RestaurantKitchenStatus k => k switch
            {
                RestaurantKitchenStatus.Pending => "انتظار",
                RestaurantKitchenStatus.Preparing => "قيد التحضير",
                RestaurantKitchenStatus.Ready => "جاهز",
                RestaurantKitchenStatus.Served => "تم التقديم",
                _ => k.ToString()
            },
            RestaurantPaymentMethod p => p switch
            {
                RestaurantPaymentMethod.Cash => "نقدي",
                RestaurantPaymentMethod.Card => "بطاقة",
                RestaurantPaymentMethod.RoomCharge => "على الغرفة",
                RestaurantPaymentMethod.Mixed => "مختلط",
                _ => p.ToString()
            },
            _ => value.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
