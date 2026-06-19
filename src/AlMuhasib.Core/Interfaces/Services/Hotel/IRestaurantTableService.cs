using AlMuhasib.Core.Entities.Hotel.Restaurant;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Hotel;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IRestaurantTableService
{
    Task<IReadOnlyList<RestaurantTable>> GetTablesAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<RestaurantTable> SaveTableAsync(RestaurantTable table, CancellationToken ct = default);
    Task DeleteTableAsync(int id, string deletedBy, CancellationToken ct = default);
    Task SetTableStatusAsync(int tableId, RestaurantTableStatus status, CancellationToken ct = default);
}

public interface IRestaurantReportService
{
    Task<RestaurantProfitSummary> GetProfitSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<RestaurantChannelSales>> GetSalesByChannelAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<RestaurantTopItem>> GetTopSellingItemsAsync(DateTime from, DateTime to, int limit = 10, CancellationToken ct = default);
    Task<RestaurantFinancialOverview> GetFinancialOverviewAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
