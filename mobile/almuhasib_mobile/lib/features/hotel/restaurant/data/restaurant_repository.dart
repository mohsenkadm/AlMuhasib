import '../../../../core/network/api_client.dart';
import '../models/restaurant_models.dart';

class RestaurantRepository {
  RestaurantRepository(this._apiClient);

  final ApiClient _apiClient;

  Future<RestaurantMenuData> getMenu() {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/hotel/restaurant/menu',
      parser: (data) => RestaurantMenuData.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<List<RestaurantTable>> getTables() {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/hotel/restaurant/tables',
      parser: (data) => (data as List<dynamic>? ?? [])
          .map((e) => RestaurantTable.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<RestaurantOrder> createOrder({
    required int orderType,
    String? tableSyncId,
    String? reservationSyncId,
  }) {
    _apiClient.updateBaseUrl();
    return _apiClient.post(
      '/api/hotel/restaurant/orders',
      data: {
        'orderType': orderType,
        if (tableSyncId != null) 'tableSyncId': tableSyncId,
        if (reservationSyncId != null) 'reservationSyncId': reservationSyncId,
      },
      parser: (data) => RestaurantOrder.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<RestaurantOrder> addLine(String orderSyncId, String menuItemSyncId, double quantity) {
    _apiClient.updateBaseUrl();
    return _apiClient.post(
      '/api/hotel/restaurant/orders/$orderSyncId/lines',
      data: {'menuItemSyncId': menuItemSyncId, 'quantity': quantity},
      parser: (data) => RestaurantOrder.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<RestaurantOrder> payOrder(String orderSyncId, double amount, {int paymentMethod = 0}) {
    _apiClient.updateBaseUrl();
    return _apiClient.post(
      '/api/hotel/restaurant/orders/$orderSyncId/pay',
      data: {'amount': amount, 'paymentMethod': paymentMethod},
      parser: (data) => RestaurantOrder.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<RestaurantProfitSummary> getProfitSummary({DateTime? from, DateTime? to}) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/hotel/restaurant/reports/summary',
      queryParameters: {
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
      },
      parser: (data) => RestaurantProfitSummary.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<List<RestaurantStockAlert>> getStockAlerts() {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/hotel/restaurant/inventory/alerts',
      parser: (data) => (data as List<dynamic>? ?? [])
          .map((e) => RestaurantStockAlert.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
