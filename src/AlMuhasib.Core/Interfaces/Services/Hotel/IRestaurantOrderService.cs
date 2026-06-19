using AlMuhasib.Core.Entities.Hotel.Restaurant;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Hotel;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IRestaurantOrderService
{
    Task<string> GenerateOrderNumberAsync(CancellationToken ct = default);
    Task<RestaurantOrder> CreateOrderAsync(RestaurantOrderType orderType, int? tableId, int? reservationId, int? roomId, CancellationToken ct = default);
    Task<RestaurantOrder?> GetOrderByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<RestaurantOrder>> GetOpenOrdersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RestaurantOrder>> GetKitchenOrdersAsync(CancellationToken ct = default);
    Task AddLineAsync(int orderId, int menuItemId, decimal quantity, CancellationToken ct = default);
    Task UpdateLineQuantityAsync(int lineId, decimal quantity, CancellationToken ct = default);
    Task RemoveLineAsync(int lineId, CancellationToken ct = default);
    Task SetOrderDiscountAsync(int orderId, decimal discountAmount, CancellationToken ct = default);
    Task<RestaurantOrder> CompleteAndPayAsync(int orderId, IReadOnlyList<RestaurantPaymentRequest> payments, bool overrideStock = false, CancellationToken ct = default);
    Task CancelOrderAsync(int orderId, CancellationToken ct = default);
    Task UpdateKitchenStatusAsync(int orderId, RestaurantKitchenStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<ActiveRoomForService>> GetActiveRoomsForServiceAsync(CancellationToken ct = default);
}

public sealed class RestaurantPaymentRequest
{
    public decimal Amount { get; set; }
    public RestaurantPaymentMethod PaymentMethod { get; set; }
    public int? HotelCashBoxId { get; set; }
}
