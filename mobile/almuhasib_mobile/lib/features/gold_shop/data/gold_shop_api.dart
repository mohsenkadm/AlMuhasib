import '../../../core/network/api_client.dart';
import '../models/gold_shop_models.dart';

/// HTTP client for gold-shop mobile endpoints (mirrors [CarRepository] style).
class GoldShopApi {
  GoldShopApi(this._api);

  final ApiClient _api;

  static const _base = '/api/gold-shop/mobile';
  static const _reports = '/api/gold-shop/reports';
  static const _statements = '/api/gold-shop/statements';
  static const _vouchers = '/api/gold-shop/vouchers';
  static const _finance = '/api/gold-shop';

  Map<String, dynamic> _dateParams(DateTime? from, DateTime? to) => {
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
      };

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

  Future<GoldInvoiceDetail> createPurchase(CreateGoldSaleRequest request) {
    return _api.post(
      '$_base/invoices/purchase',
      data: request.toJson(),
      parser: (data) =>
          GoldInvoiceDetail.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<GoldInvoiceDetail> createSaleReturn(CreateGoldSaleRequest request) {
    return _api.post(
      '$_base/invoices/sale-return',
      data: request.toJson(),
      parser: (data) =>
          GoldInvoiceDetail.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<GoldInvoiceDetail> createExchange(CreateGoldExchangeRequest request) {
    return _api.post(
      '$_base/invoices/exchange',
      data: request.toJson(),
      parser: (data) =>
          GoldInvoiceDetail.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<Map<String, dynamic>> collect(CreateGoldCollectionRequest request) {
    return _api.post(
      '$_base/invoices/collection',
      data: request.toJson(),
      parser: (data) => data as Map<String, dynamic>,
    );
  }

  Future<List<GoldCashBoxItem>> getCashBoxes() {
    return _api.get(
      '$_finance/cash-boxes',
      parser: (data) => (data as List<dynamic>)
          .map((e) => GoldCashBoxItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<GoldExpenseItem>> getExpenses({
    DateTime? from,
    DateTime? to,
    int page = 1,
    int pageSize = 50,
  }) {
    return _api.get(
      '$_finance/expenses',
      queryParameters: {
        ..._dateParams(from, to),
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) {
        final map = data as Map<String, dynamic>;
        return (map['items'] as List<dynamic>? ?? [])
            .map((e) => GoldExpenseItem.fromJson(e as Map<String, dynamic>))
            .toList();
      },
    );
  }

  Future<List<GoldExpenseTypeItem>> getExpenseTypes() {
    return _api.get(
      '$_finance/expense-types',
      parser: (data) => (data as List<dynamic>)
          .map((e) => GoldExpenseTypeItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<GoldExpenseItem> createExpense({
    required int expenseTypeId,
    required double amount,
    required int cashBoxId,
    DateTime? expenseDate,
    String currency = 'IQD',
    int? warehouseId,
    String notes = '',
  }) {
    return _api.post(
      '$_finance/expenses',
      data: {
        'expenseDate': (expenseDate ?? DateTime.now()).toIso8601String(),
        'expenseTypeId': expenseTypeId,
        'amount': amount,
        'currency': currency,
        'cashBoxId': cashBoxId,
        if (warehouseId != null) 'warehouseId': warehouseId,
        'notes': notes,
      },
      parser: (data) =>
          GoldExpenseItem.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<List<GoldVoucherItem>> getVouchers({
    DateTime? from,
    DateTime? to,
    String? type,
    int? customerId,
    int? supplierId,
    int? cashBoxId,
    int page = 1,
    int pageSize = 50,
  }) {
    return _api.get(
      _vouchers,
      queryParameters: {
        ..._dateParams(from, to),
        if (type != null && type.isNotEmpty) 'type': type,
        if (customerId != null) 'customerId': customerId,
        if (supplierId != null) 'supplierId': supplierId,
        if (cashBoxId != null) 'cashBoxId': cashBoxId,
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) {
        final map = data as Map<String, dynamic>;
        return (map['items'] as List<dynamic>? ?? [])
            .map((e) => GoldVoucherItem.fromJson(e as Map<String, dynamic>))
            .toList();
      },
    );
  }

  Future<GoldVoucherItem> getVoucher(int id) {
    return _api.get(
      '$_vouchers/$id',
      parser: (data) =>
          GoldVoucherItem.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<GoldVoucherItem> createVoucher(CreateGoldVoucherRequest request) {
    return _api.post(
      _vouchers,
      data: request.toJson(),
      parser: (data) =>
          GoldVoucherItem.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<GoldStatementDto> getCustomerStatement(
    int id, {
    DateTime? from,
    DateTime? to,
  }) {
    return _api.get(
      '$_statements/customer/$id',
      queryParameters: _dateParams(from, to),
      parser: (data) =>
          GoldStatementDto.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<GoldStatementDto> getSupplierStatement(
    int id, {
    DateTime? from,
    DateTime? to,
  }) {
    return _api.get(
      '$_statements/supplier/$id',
      queryParameters: _dateParams(from, to),
      parser: (data) =>
          GoldStatementDto.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<Map<String, dynamic>> getReportRaw(
    String path, {
    DateTime? from,
    DateTime? to,
    Map<String, dynamic>? extra,
  }) {
    return _api.get(
      '$_reports/$path',
      queryParameters: {
        ..._dateParams(from, to),
        ...?extra,
      },
      parser: (data) => data as Map<String, dynamic>,
    );
  }
}
