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
        (HotelPermissionRegistry.CloudSync, "المزامنة السحابية"),
        (ScreenPermissionRegistry.NetworkConnection, "ربط الحاسبات")
    ];

    public IReadOnlyList<NavigationMenuItem> BuildMenuItems()
    {
        var items = new List<NavigationMenuItem>
        {
            Item("لوحة التحكم", PackIconKind.ViewDashboard, typeof(HotelDashboardViewModel), HotelPermissionRegistry.Dashboard),
            FlyoutGroup(
                key: "operations",
                title: "العمليات",
                icon: PackIconKind.BriefcaseClockOutline,
                accent: "#1565C0",
                accentLight: "#E3F2FD",
                [
                    ("حجز جديد", PackIconKind.CalendarPlus, typeof(HotelReservationFormViewModel), HotelPermissionRegistry.ReservationForm),
                    ("الحجوزات", PackIconKind.CalendarClock, typeof(HotelReservationsViewModel), HotelPermissionRegistry.Reservations),
                    ("تقويم الحجوزات", PackIconKind.CalendarMonth, typeof(HotelReservationsCalendarViewModel), HotelPermissionRegistry.ReservationsCalendar),
                    ("تسجيل دخول/خروج", PackIconKind.Login, typeof(HotelCheckInOutViewModel), HotelPermissionRegistry.CheckInOut),
                ]),
            FlyoutGroup(
                key: "rooms-guests",
                title: "الغرف والنزلاء",
                icon: PackIconKind.Bed,
                accent: "#2E7D32",
                accentLight: "#E8F5E9",
                [
                    ("الغرف", PackIconKind.Door, typeof(HotelRoomsViewModel), HotelPermissionRegistry.Rooms),
                    ("أنواع الغرف", PackIconKind.Bed, typeof(HotelRoomTypesViewModel), HotelPermissionRegistry.RoomTypes),
                    ("الطوابق", PackIconKind.Stairs, typeof(HotelFloorsViewModel), HotelPermissionRegistry.Floors),
                    ("النزلاء", PackIconKind.AccountGroup, typeof(HotelGuestsViewModel), HotelPermissionRegistry.Guests),
                    ("خطط الأسعار", PackIconKind.CurrencyUsd, typeof(HotelRatePlansViewModel), HotelPermissionRegistry.RatePlans),
                    ("النظافة", PackIconKind.Broom, typeof(HotelHousekeepingViewModel), HotelPermissionRegistry.Housekeeping),
                ]),
            FlyoutGroup(
                key: "restaurant",
                title: "المطعم",
                icon: PackIconKind.SilverwareForkKnife,
                accent: "#E65100",
                accentLight: "#FFF3E0",
                [
                    ("كاشير المطعم", PackIconKind.SilverwareForkKnife, typeof(RestaurantPosViewModel), HotelPermissionRegistry.RestaurantPos),
                    ("قائمة المطعم", PackIconKind.Food, typeof(RestaurantMenuViewModel), HotelPermissionRegistry.RestaurantMenu),
                    ("مخزون المطبخ", PackIconKind.PackageVariant, typeof(RestaurantInventoryViewModel), HotelPermissionRegistry.RestaurantInventory),
                    ("طاولات الصالة", PackIconKind.TableChair, typeof(RestaurantTablesViewModel), HotelPermissionRegistry.RestaurantTables),
                    ("تقارير المطعم", PackIconKind.ChartPie, typeof(RestaurantReportsViewModel), HotelPermissionRegistry.RestaurantReports),
                    ("شاشة المطبخ", PackIconKind.Stove, typeof(RestaurantKitchenViewModel), HotelPermissionRegistry.RestaurantKitchen),
                ]),
            FlyoutGroup(
                key: "finance",
                title: "المالية",
                icon: PackIconKind.CashMultiple,
                accent: "#6A1B9A",
                accentLight: "#F3E5F5",
                [
                    ("الصندوق", PackIconKind.CashRegister, typeof(HotelCashViewModel), HotelPermissionRegistry.HotelCash),
                    ("المصاريف", PackIconKind.CashMinus, typeof(HotelExpensesViewModel), HotelPermissionRegistry.HotelExpenses),
                    ("التقارير", PackIconKind.ChartBar, typeof(HotelReportsViewModel), HotelPermissionRegistry.HotelReports),
                ]),
            FlyoutGroup(
                key: "system",
                title: "النظام والإعدادات",
                icon: PackIconKind.CogOutline,
                accent: "#455A64",
                accentLight: "#ECEFF1",
                [
                    ("المستخدمون", PackIconKind.AccountMultiple, typeof(UsersViewModel), HotelPermissionRegistry.Users),
                    ("الصلاحيات", PackIconKind.ShieldKey, typeof(PermissionsViewModel), HotelPermissionRegistry.Permissions),
                    ("إعدادات الطباعة", PackIconKind.PrinterSettings, typeof(PrintLayoutSettingsViewModel), HotelPermissionRegistry.PrintSettings),
                    ("النسخ الاحتياطي", PackIconKind.DatabaseCog, typeof(BackupRestoreViewModel), HotelPermissionRegistry.Backup),
                    ("ربط الحاسبات", PackIconKind.LanConnect, typeof(NetworkConnectionSettingsViewModel), ScreenPermissionRegistry.NetworkConnection),
                    ("المزامنة السحابية", PackIconKind.CloudSync, typeof(CloudSyncSettingsViewModel), HotelPermissionRegistry.CloudSync),
                    ("تحديث النظام", PackIconKind.CloudDownload, typeof(SystemUpdateViewModel), ScreenPermissionRegistry.SystemUpdate),
                    ("تبديل النظام (مطور)", PackIconKind.DeveloperBoard, typeof(DeveloperSystemSwitchViewModel), ScreenPermissionRegistry.DeveloperSystem),
                ])
        };

        if (items.Count > 0)
            items[0].IsSelected = true;

        return items;
    }

    public string GetScreenName(Type viewModelType) =>
        HotelPermissionRegistry.GetScreenName(viewModelType);

    public string GetLabel(string screenName) =>
        HotelPermissionRegistry.GetLabel(screenName);

    public Type? GetDefaultViewModelType(string screenName) =>
        HotelPermissionRegistry.GetDefaultViewModelType(screenName);

    private static NavigationMenuItem Item(string title, PackIconKind icon, Type viewModelType, string screenName) =>
        new()
        {
            Title = title,
            Icon = icon,
            ViewModelType = viewModelType,
            ScreenName = screenName
        };

    private static NavigationMenuItem FlyoutGroup(
        string key,
        string title,
        PackIconKind icon,
        string accent,
        string accentLight,
        (string Title, PackIconKind Icon, Type Vm, string Screen)[] children)
    {
        var group = new NavigationMenuItem
        {
            Title = title,
            Icon = icon,
            IsReportCategory = true,
            CategoryKey = key,
            ScreenName = $"MenuGroup:{key}",
            CategoryAccentColor = accent,
            CategoryAccentLightColor = accentLight,
            FlyoutItemLabel = "شاشة"
        };

        foreach (var child in children)
        {
            group.Children.Add(new NavigationMenuItem
            {
                Title = child.Title,
                Icon = child.Icon,
                ViewModelType = child.Vm,
                ScreenName = child.Screen,
                IsSubItem = true
            });
        }

        return group;
    }
}
