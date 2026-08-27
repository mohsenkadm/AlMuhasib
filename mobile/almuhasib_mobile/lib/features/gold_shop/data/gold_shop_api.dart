import '../../../core/network/api_client.dart';
import '../models/gold_shop_models.dart';

/// HTTP client for gold-shop mobile endpoints (mirrors [CarRepository] style).
class GoldShopApi {
  GoldShopApi(this._api);

  final ApiClient _api;

  static const _base = '/api/gold-shop/mobile';

  Future<GoldDashboardDto> getDashboard() {
    return _api.get(
      '$_base/dashboard',
      parser: (data) =>
          GoldDashboardDto.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<List<GoldMithqalPriceRow>> getPrices() {
    return _api.get(
      '$_base/prices',
      parser: (data) => (data as List<dynamic>)
          .map((e) => GoldMithqalPriceRow.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<GoldInvoiceListItem>> getInvoices({
    String? search,
    int? status,
    int? invoiceType,
    DateTime? from,
    DateTime? to,
    int page = 1,
    int pageSize = 50,
  }) {
    return _api.get(
      '$_base/invoices',
      queryParameters: {
        if (search != null && search.isNotEmpty) 'search': search,
        if (status != null) 'status': status,
        if (invoiceType != null) 'invoiceType': invoiceType,
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => (data as List<dynamic>)
          .map(
            (e) => GoldInvoiceListItem.fromJson(e as Map<String, dynamic>),
          )
          .toList(),
    );
  }

  Future<GoldInvoiceDetail> getInvoice(int id) {
    return _api.get(
      '$_base/invoices/$id',
      parser: (data) =>
          GoldInvoiceDetail.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<List<GoldCustomerListItem>> getCustomers({
    String? search,
    int page = 1,
    int pageSize = 50,
  }) {
    return _api.get(
      '$_base/customers',
      queryParameters: {
        if (search != null && search.isNotEmpty) 'search': search,
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => (data as List<dynamic>)
          .map(
            (e) => GoldCustomerListItem.fromJson(e as Map<String, dynamic>),
          )
          .toList(),
    );
  }

  Future<List<GoldNotificationItem>> getNotifications({
    bool? unreadOnly,
    int page = 1,
    int pageSize = 50,
  }) {
    return _api.get(
      '$_base/notifications',
      queryParameters: {
        if (unreadOnly == true) 'unreadOnly': true,
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => (data as List<dynamic>)
          .map(
            (e) => GoldNotificationItem.fromJson(e as Map<String, dynamic>),
          )
          .toList(),
    );
  }

  Future<List<GoldWarehouseItem>> getWarehouses() {
    return _api.get(
      '$_base/warehouses',
      parser: (data) => (data as List<dynamic>)
          .map((e) => GoldWarehouseItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<GoldSupplierItem>> getSuppliers({String? search}) {
    return _api.get(
      '$_base/suppliers',
      queryParameters: {
        if (search != null && search.isNotEmpty) 'search': search,
      },
      parser: (data) => (data as List<dynamic>)
          .map((e) => GoldSupplierItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<GoldStockRow>> getStock() {
    return _api.get(
      '/api/gold-shop/master/stock',
      parser: (data) {
        if (data is Map<String, dynamic> && data['rows'] is List) {
          return (data['rows'] as List<dynamic>)
              .map((e) => GoldStockRow.fromJson(e as Map<String, dynamic>))
              .toList();
        }
        return (data as List<dynamic>)
            .map((e) => GoldStockRow.fromJson(e as Map<String, dynamic>))
            .toList();
      },
    );
  }

  Future<GoldInvoiceDetail> createSale(CreateGoldSaleRequest request) {
    return _api.post(
      '$_base/invoices/sale',
      data: request.toJson(),
      parser: (data) =>
          GoldInvoiceDetail.fromJson(data as Map<String, dynamic>),
    );
  }
}
