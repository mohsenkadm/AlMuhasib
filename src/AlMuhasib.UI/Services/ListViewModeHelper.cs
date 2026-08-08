using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.UI.Services;

public static class ListViewModeKeys
{
    public const string Products = "Products";
    public const string Categories = "Categories";
    public const string Customers = "Customers";
    public const string Drivers = "Drivers";
    public const string SalesRepresentatives = "SalesRepresentatives";
    public const string Suppliers = "Suppliers";
    public const string Investors = "Investors";
    public const string HotelFloors = "HotelFloors";
    public const string HotelRoomTypes = "HotelRoomTypes";
    public const string HotelRooms = "HotelRooms";
    public const string HotelGuests = "HotelGuests";
    public const string HotelRatePlans = "HotelRatePlans";
    public const string HotelHousekeeping = "HotelHousekeeping";
    public const string CarTradeTransactions = "CarTradeTransactions";
}

public static class ListViewModeHelper
{
    public static bool LoadIsCardView(IUserPreferencesService preferences, string screenKey) =>
        preferences.Current.ListViewModes.TryGetValue(screenKey, out var mode)
        && mode == MasterDataListViewMode.Cards;

    public static void SaveIsCardView(IUserPreferencesService preferences, string screenKey, bool isCardView) =>
        preferences.Update(p =>
            p.ListViewModes[screenKey] = isCardView
                ? MasterDataListViewMode.Cards
                : MasterDataListViewMode.Table);
}
