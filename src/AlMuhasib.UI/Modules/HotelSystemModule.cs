using AlMuhasib.Core.Enums;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.ViewModels.Hotel;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Modules;

public sealed class HotelSystemModule : ISystemModule
{
    public ApplicationSystemType SystemType => ApplicationSystemType.HotelManagement;
    public string DisplayName => "نظام إدارة الفنادق";
    public Type DashboardViewModelType => typeof(HotelDashboardViewModel);
    public Type? SetupWizardViewModelType => typeof(HotelSetupWizardViewModel);

    public IReadOnlyList<(string Name, string Label)> PermissionScreens { get; } =
    [
        (HotelPermissionRegistry.Dashboard, "لوحة التحكم"),
        (HotelPermissionRegistry.Reservations, "الحجوزات"),
        (HotelPermissionRegistry.ReservationsCalendar, "تقويم الحجوزات"),
        (HotelPermissionRegistry.ReservationForm, "حجز جديد"),
        (HotelPermissionRegistry.CheckInOut, "تسجيل دخول/خروج"),
        (HotelPermissionRegistry.Rooms, "الغرف"),
        (HotelPermissionRegistry.RoomTypes, "أنواع الغرف"),
        (HotelPermissionRegistry.Floors, "الطوابق"),
        (HotelPermissionRegistry.Guests, "النزلاء"),
        (HotelPermissionRegistry.RatePlans, "خطط الأسعار"),
        (HotelPermissionRegistry.Housekeeping, "النظافة"),
        (HotelPermissionRegistry.RestaurantPos, "كاشير المطعم"),
        (HotelPermissionRegistry.RestaurantMenu, "قائمة المطعم"),
        (HotelPermissionRegistry.RestaurantInventory, "مخزون المطبخ"),
        (HotelPermissionRegistry.RestaurantTables, "طاولات الصالة"),
        (HotelPermissionRegistry.RestaurantReports, "تقارير المطعم"),
        (HotelPermissionRegistry.RestaurantKitchen, "شاشة المطبخ"),
        (HotelPermissionRegistry.HotelCash, "الصندوق"),
        (HotelPermissionRegistry.HotelExpenses, "المصاريف"),
        (HotelPermissionRegistry.HotelReports, "التقارير"),
        (HotelPermissionRegistry.Users, "المستخدمون"),
        (HotelPermissionRegistry.Permissions, "الصلاحيات"),
        (HotelPermissionRegistry.PrintSettings, "إعدادات الطباعة"),
        (HotelPermissionRegistry.Backup, "النسخ الاحتياطي"),
        (HotelPermissionRegistry.CloudSync, "المزامنة السحابية")
    ];

    public IReadOnlyList<NavigationMenuItem> BuildMenuItems()
    {
        var operations = CreateGroup("العمليات", PackIconKind.BriefcaseClockOutline, isExpanded: true,
            Item("لوحة التحكم", PackIconKind.ViewDashboard, typeof(HotelDashboardViewModel), HotelPermissionRegistry.Dashboard),
            Item("حجز جديد", PackIconKind.CalendarPlus, typeof(HotelReservationFormViewModel), HotelPermissionRegistry.ReservationForm),
            Item("الحجوزات", PackIconKind.CalendarClock, typeof(HotelReservationsViewModel), HotelPermissionRegistry.Reservations),
            Item("تقويم الحجوزات", PackIconKind.CalendarMonth, typeof(HotelReservationsCalendarViewModel), HotelPermissionRegistry.ReservationsCalendar),
            Item("تسجيل دخول/خروج", PackIconKind.Login, typeof(HotelCheckInOutViewModel), HotelPermissionRegistry.CheckInOut));

        var roomsGuests = CreateGroup("الغرف والنزلاء", PackIconKind.Bed, isExpanded: true,
            Item("الغرف", PackIconKind.Door, typeof(HotelRoomsViewModel), HotelPermissionRegistry.Rooms),
            Item("أنواع الغرف", PackIconKind.Bed, typeof(HotelRoomTypesViewModel), HotelPermissionRegistry.RoomTypes),
            Item("الطوابق", PackIconKind.Stairs, typeof(HotelFloorsViewModel), HotelPermissionRegistry.Floors),
            Item("النزلاء", PackIconKind.AccountGroup, typeof(HotelGuestsViewModel), HotelPermissionRegistry.Guests),
            Item("خطط الأسعار", PackIconKind.CurrencyUsd, typeof(HotelRatePlansViewModel), HotelPermissionRegistry.RatePlans),
            Item("النظافة", PackIconKind.Broom, typeof(HotelHousekeepingViewModel), HotelPermissionRegistry.Housekeeping));

        var restaurant = CreateGroup("المطعم", PackIconKind.SilverwareForkKnife, isExpanded: false,
            Item("كاشير المطعم", PackIconKind.SilverwareForkKnife, typeof(RestaurantPosViewModel), HotelPermissionRegistry.RestaurantPos),
            Item("قائمة المطعم", PackIconKind.Food, typeof(RestaurantMenuViewModel), HotelPermissionRegistry.RestaurantMenu),
            Item("مخزون المطبخ", PackIconKind.PackageVariant, typeof(RestaurantInventoryViewModel), HotelPermissionRegistry.RestaurantInventory),
            Item("طاولات الصالة", PackIconKind.TableChair, typeof(RestaurantTablesViewModel), HotelPermissionRegistry.RestaurantTables),
            Item("تقارير المطعم", PackIconKind.ChartPie, typeof(RestaurantReportsViewModel), HotelPermissionRegistry.RestaurantReports),
            Item("شاشة المطبخ", PackIconKind.Stove, typeof(RestaurantKitchenViewModel), HotelPermissionRegistry.RestaurantKitchen));

        var finance = CreateGroup("المالية", PackIconKind.CashMultiple, isExpanded: false,
            Item("الصندوق", PackIconKind.CashRegister, typeof(HotelCashViewModel), HotelPermissionRegistry.HotelCash),
            Item("المصاريف", PackIconKind.CashMinus, typeof(HotelExpensesViewModel), HotelPermissionRegistry.HotelExpenses),
            Item("التقارير", PackIconKind.ChartBar, typeof(HotelReportsViewModel), HotelPermissionRegistry.HotelReports));

        var system = CreateGroup("النظام", PackIconKind.CogOutline, isExpanded: false,
            Item("المستخدمون", PackIconKind.AccountMultiple, typeof(UsersViewModel), HotelPermissionRegistry.Users),
            Item("الصلاحيات", PackIconKind.ShieldKey, typeof(PermissionsViewModel), HotelPermissionRegistry.Permissions),
            Item("إعدادات الطباعة", PackIconKind.PrinterSettings, typeof(PrintLayoutSettingsViewModel), HotelPermissionRegistry.PrintSettings),
            Item("النسخ الاحتياطي", PackIconKind.DatabaseCog, typeof(BackupRestoreViewModel), HotelPermissionRegistry.Backup),
            Item("المزامنة السحابية", PackIconKind.CloudSync, typeof(CloudSyncSettingsViewModel), HotelPermissionRegistry.CloudSync),
            Item("تحديث النظام", PackIconKind.CloudDownload, typeof(SystemUpdateViewModel), ScreenPermissionRegistry.SystemUpdate),
            Item("تبديل النظام (مطور)", PackIconKind.DeveloperBoard, typeof(DeveloperSystemSwitchViewModel), ScreenPermissionRegistry.DeveloperSystem));

        return [operations, roomsGuests, restaurant, finance, system];
    }

    private static NavigationMenuItem Item(string title, PackIconKind icon, Type viewModelType, string screenName) =>
        new()
        {
            Title = title,
            Icon = icon,
            ViewModelType = viewModelType,
            ScreenName = screenName,
            IsSubItem = true
        };

    private static NavigationMenuItem CreateGroup(string title, PackIconKind icon, bool isExpanded, params NavigationMenuItem[] children)
    {
        var group = new NavigationMenuItem
        {
            Title = title,
            Icon = icon,
            IsGroupHeader = true,
            IsExpanded = isExpanded
        };

        foreach (var child in children)
            group.Children.Add(child);

        return group;
    }

    public string GetScreenName(Type viewModelType) =>
        HotelPermissionRegistry.GetScreenName(viewModelType);

    public string GetLabel(string screenName) =>
        HotelPermissionRegistry.GetLabel(screenName);

    public Type? GetDefaultViewModelType(string screenName) =>
        HotelPermissionRegistry.GetDefaultViewModelType(screenName);
}
