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

    public IReadOnlyList<NavigationMenuItem> BuildMenuItems() =>
    [
        new NavigationMenuItem
        {
            Title = "لوحة التحكم",
            Icon = PackIconKind.ViewDashboard,
            ViewModelType = typeof(HotelDashboardViewModel),
            ScreenName = HotelPermissionRegistry.Dashboard
        },
        new NavigationMenuItem
        {
            Title = "حجز جديد",
            Icon = PackIconKind.CalendarPlus,
            ViewModelType = typeof(HotelReservationFormViewModel),
            ScreenName = HotelPermissionRegistry.ReservationForm
        },
        new NavigationMenuItem
        {
            Title = "الحجوزات",
            Icon = PackIconKind.CalendarClock,
            ViewModelType = typeof(HotelReservationsViewModel),
            ScreenName = HotelPermissionRegistry.Reservations
        },
        new NavigationMenuItem
        {
            Title = "تقويم الحجوزات",
            Icon = PackIconKind.CalendarMonth,
            ViewModelType = typeof(HotelReservationsCalendarViewModel),
            ScreenName = HotelPermissionRegistry.ReservationsCalendar
        },
        new NavigationMenuItem
        {
            Title = "تسجيل دخول/خروج",
            Icon = PackIconKind.Login,
            ViewModelType = typeof(HotelCheckInOutViewModel),
            ScreenName = HotelPermissionRegistry.CheckInOut
        },
        new NavigationMenuItem
        {
            Title = "الغرف",
            Icon = PackIconKind.Door,
            ViewModelType = typeof(HotelRoomsViewModel),
            ScreenName = HotelPermissionRegistry.Rooms
        },
        new NavigationMenuItem
        {
            Title = "أنواع الغرف",
            Icon = PackIconKind.Bed,
            ViewModelType = typeof(HotelRoomTypesViewModel),
            ScreenName = HotelPermissionRegistry.RoomTypes
        },
        new NavigationMenuItem
        {
            Title = "الطوابق",
            Icon = PackIconKind.Stairs,
            ViewModelType = typeof(HotelFloorsViewModel),
            ScreenName = HotelPermissionRegistry.Floors
        },
        new NavigationMenuItem
        {
            Title = "النزلاء",
            Icon = PackIconKind.AccountGroup,
            ViewModelType = typeof(HotelGuestsViewModel),
            ScreenName = HotelPermissionRegistry.Guests
        },
        new NavigationMenuItem
        {
            Title = "خطط الأسعار",
            Icon = PackIconKind.CurrencyUsd,
            ViewModelType = typeof(HotelRatePlansViewModel),
            ScreenName = HotelPermissionRegistry.RatePlans
        },
        new NavigationMenuItem
        {
            Title = "النظافة",
            Icon = PackIconKind.Broom,
            ViewModelType = typeof(HotelHousekeepingViewModel),
            ScreenName = HotelPermissionRegistry.Housekeeping
        },
        new NavigationMenuItem
        {
            Title = "كاشير المطعم",
            Icon = PackIconKind.SilverwareForkKnife,
            ViewModelType = typeof(RestaurantPosViewModel),
            ScreenName = HotelPermissionRegistry.RestaurantPos
        },
        new NavigationMenuItem
        {
            Title = "قائمة المطعم",
            Icon = PackIconKind.Food,
            ViewModelType = typeof(RestaurantMenuViewModel),
            ScreenName = HotelPermissionRegistry.RestaurantMenu
        },
        new NavigationMenuItem
        {
            Title = "مخزون المطبخ",
            Icon = PackIconKind.PackageVariant,
            ViewModelType = typeof(RestaurantInventoryViewModel),
            ScreenName = HotelPermissionRegistry.RestaurantInventory
        },
        new NavigationMenuItem
        {
            Title = "طاولات الصالة",
            Icon = PackIconKind.TableChair,
            ViewModelType = typeof(RestaurantTablesViewModel),
            ScreenName = HotelPermissionRegistry.RestaurantTables
        },
        new NavigationMenuItem
        {
            Title = "تقارير المطعم",
            Icon = PackIconKind.ChartPie,
            ViewModelType = typeof(RestaurantReportsViewModel),
            ScreenName = HotelPermissionRegistry.RestaurantReports
        },
        new NavigationMenuItem
        {
            Title = "شاشة المطبخ",
            Icon = PackIconKind.Stove,
            ViewModelType = typeof(RestaurantKitchenViewModel),
            ScreenName = HotelPermissionRegistry.RestaurantKitchen
        },
        new NavigationMenuItem
        {
            Title = "الصندوق",
            Icon = PackIconKind.CashRegister,
            ViewModelType = typeof(HotelCashViewModel),
            ScreenName = HotelPermissionRegistry.HotelCash
        },
        new NavigationMenuItem
        {
            Title = "المصاريف",
            Icon = PackIconKind.CashMinus,
            ViewModelType = typeof(HotelExpensesViewModel),
            ScreenName = HotelPermissionRegistry.HotelExpenses
        },
        new NavigationMenuItem
        {
            Title = "التقارير",
            Icon = PackIconKind.ChartBar,
            ViewModelType = typeof(HotelReportsViewModel),
            ScreenName = HotelPermissionRegistry.HotelReports
        },
        new NavigationMenuItem
        {
            Title = "المستخدمون",
            Icon = PackIconKind.AccountMultiple,
            ViewModelType = typeof(UsersViewModel),
            ScreenName = HotelPermissionRegistry.Users
        },
        new NavigationMenuItem
        {
            Title = "الصلاحيات",
            Icon = PackIconKind.ShieldKey,
            ViewModelType = typeof(PermissionsViewModel),
            ScreenName = HotelPermissionRegistry.Permissions
        },
        new NavigationMenuItem
        {
            Title = "إعدادات الطباعة",
            Icon = PackIconKind.PrinterSettings,
            ViewModelType = typeof(PrintLayoutSettingsViewModel),
            ScreenName = HotelPermissionRegistry.PrintSettings
        },
        new NavigationMenuItem
        {
            Title = "النسخ الاحتياطي",
            Icon = PackIconKind.DatabaseCog,
            ViewModelType = typeof(BackupRestoreViewModel),
            ScreenName = HotelPermissionRegistry.Backup
        },
        new NavigationMenuItem
        {
            Title = "المزامنة السحابية",
            Icon = PackIconKind.CloudSync,
            ViewModelType = typeof(CloudSyncSettingsViewModel),
            ScreenName = HotelPermissionRegistry.CloudSync
        },
        new NavigationMenuItem
        {
            Title = "تبديل النظام (مطور)",
            Icon = PackIconKind.DeveloperBoard,
            ViewModelType = typeof(DeveloperSystemSwitchViewModel),
            ScreenName = ScreenPermissionRegistry.DeveloperSystem
        }
    ];

    public string GetScreenName(Type viewModelType) =>
        HotelPermissionRegistry.GetScreenName(viewModelType);

    public string GetLabel(string screenName) =>
        HotelPermissionRegistry.GetLabel(screenName);

    public Type? GetDefaultViewModelType(string screenName) =>
        HotelPermissionRegistry.GetDefaultViewModelType(screenName);
}
