using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.ViewModels.Hotel;

namespace AlMuhasib.UI.Services;

public static class HotelPermissionRegistry
{
    public const string Dashboard = "HotelDashboard";
    public const string Reservations = "HotelReservations";
    public const string ReservationsCalendar = "HotelReservationsCalendar";
    public const string ReservationForm = "HotelReservationForm";
    public const string CheckInOut = "HotelCheckInOut";
    public const string Rooms = "HotelRooms";
    public const string RoomTypes = "HotelRoomTypes";
    public const string Floors = "HotelFloors";
    public const string Guests = "HotelGuests";
    public const string RatePlans = "HotelRatePlans";
    public const string Housekeeping = "HotelHousekeeping";
    public const string HotelCash = "HotelCash";
    public const string HotelExpenses = "HotelExpenses";
    public const string HotelReports = "HotelReports";
    public const string RestaurantPos = "RestaurantPos";
    public const string RestaurantMenu = "RestaurantMenu";
    public const string RestaurantInventory = "RestaurantInventory";
    public const string RestaurantTables = "RestaurantTables";
    public const string RestaurantReports = "RestaurantReports";
    public const string RestaurantKitchen = "RestaurantKitchen";
    public const string Users = "Users";
    public const string Permissions = "Permissions";
    public const string PrintSettings = "PrintSettings";
    public const string Backup = "Backup";
    public const string CloudSync = "CloudSync";

    private static readonly Dictionary<Type, string> ViewModelToScreen = new()
    {
        [typeof(HotelDashboardViewModel)] = Dashboard,
        [typeof(HotelReservationsViewModel)] = Reservations,
        [typeof(HotelReservationsCalendarViewModel)] = ReservationsCalendar,
        [typeof(HotelReservationFormViewModel)] = ReservationForm,
        [typeof(HotelCheckInOutViewModel)] = CheckInOut,
        [typeof(HotelRoomsViewModel)] = Rooms,
        [typeof(HotelRoomTypesViewModel)] = RoomTypes,
        [typeof(HotelFloorsViewModel)] = Floors,
        [typeof(HotelGuestsViewModel)] = Guests,
        [typeof(HotelRatePlansViewModel)] = RatePlans,
        [typeof(HotelHousekeepingViewModel)] = Housekeeping,
        [typeof(HotelCashViewModel)] = HotelCash,
        [typeof(HotelExpensesViewModel)] = HotelExpenses,
        [typeof(HotelReportsViewModel)] = HotelReports,
        [typeof(RestaurantPosViewModel)] = RestaurantPos,
        [typeof(RestaurantMenuViewModel)] = RestaurantMenu,
        [typeof(RestaurantInventoryViewModel)] = RestaurantInventory,
        [typeof(RestaurantTablesViewModel)] = RestaurantTables,
        [typeof(RestaurantReportsViewModel)] = RestaurantReports,
        [typeof(RestaurantKitchenViewModel)] = RestaurantKitchen,
        [typeof(UsersViewModel)] = Users,
        [typeof(PermissionsViewModel)] = Permissions,
        [typeof(PrintLayoutSettingsViewModel)] = PrintSettings,
        [typeof(BackupRestoreViewModel)] = Backup,
        [typeof(CloudSyncSettingsViewModel)] = CloudSync
    };

    private static readonly Dictionary<string, Type> ScreenToDefaultViewModel = new()
    {
        [Dashboard] = typeof(HotelDashboardViewModel),
        [Reservations] = typeof(HotelReservationsViewModel),
        [ReservationsCalendar] = typeof(HotelReservationsCalendarViewModel),
        [ReservationForm] = typeof(HotelReservationFormViewModel),
        [CheckInOut] = typeof(HotelCheckInOutViewModel),
        [Rooms] = typeof(HotelRoomsViewModel),
        [RoomTypes] = typeof(HotelRoomTypesViewModel),
        [Floors] = typeof(HotelFloorsViewModel),
        [Guests] = typeof(HotelGuestsViewModel),
        [RatePlans] = typeof(HotelRatePlansViewModel),
        [Housekeeping] = typeof(HotelHousekeepingViewModel),
        [HotelCash] = typeof(HotelCashViewModel),
        [HotelExpenses] = typeof(HotelExpensesViewModel),
        [HotelReports] = typeof(HotelReportsViewModel),
        [RestaurantPos] = typeof(RestaurantPosViewModel),
        [RestaurantMenu] = typeof(RestaurantMenuViewModel),
        [RestaurantInventory] = typeof(RestaurantInventoryViewModel),
        [RestaurantTables] = typeof(RestaurantTablesViewModel),
        [RestaurantReports] = typeof(RestaurantReportsViewModel),
        [RestaurantKitchen] = typeof(RestaurantKitchenViewModel),
        [Users] = typeof(UsersViewModel),
        [Permissions] = typeof(PermissionsViewModel),
        [PrintSettings] = typeof(PrintLayoutSettingsViewModel),
        [Backup] = typeof(BackupRestoreViewModel),
        [CloudSync] = typeof(CloudSyncSettingsViewModel)
    };

    private static readonly Dictionary<string, string> Labels = new()
    {
        [Dashboard] = "لوحة التحكم",
        [Reservations] = "الحجوزات",
        [ReservationsCalendar] = "تقويم الحجوزات",
        [ReservationForm] = "حجز جديد",
        [CheckInOut] = "تسجيل دخول/خروج",
        [Rooms] = "الغرف",
        [RoomTypes] = "أنواع الغرف",
        [Floors] = "الطوابق",
        [Guests] = "النزلاء",
        [RatePlans] = "خطط الأسعار",
        [Housekeeping] = "النظافة",
        [HotelCash] = "الصندوق",
        [HotelExpenses] = "المصاريف",
        [HotelReports] = "التقارير",
        [RestaurantPos] = "كاشير المطعم",
        [RestaurantMenu] = "قائمة المطعم",
        [RestaurantInventory] = "مخزون المطبخ",
        [RestaurantTables] = "طاولات الصالة",
        [RestaurantReports] = "تقارير المطعم",
        [RestaurantKitchen] = "شاشة المطبخ",
        [Users] = "المستخدمون",
        [Permissions] = "الصلاحيات",
        [PrintSettings] = "إعدادات الطباعة",
        [Backup] = "النسخ الاحتياطي",
        [CloudSync] = "المزامنة السحابية"
    };

    public static string GetScreenName(Type viewModelType) =>
        ViewModelToScreen.TryGetValue(viewModelType, out var name) ? name : viewModelType.Name;

    public static Type? GetDefaultViewModelType(string screenName) =>
        ScreenToDefaultViewModel.TryGetValue(screenName, out var type) ? type : null;

    public static string GetLabel(string screenName) =>
        Labels.TryGetValue(screenName, out var label) ? label : screenName;
}
