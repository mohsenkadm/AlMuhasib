import '../../../core/network/api_client.dart';
import '../../../shared/models/master_data_models.dart';
import '../../../shared/models/paged_result.dart';

class DataRepository {
  DataRepository(this._apiClient);

  final ApiClient _apiClient;

  Future<List<LookupItem>> getCustomers({String? search}) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/master-data/customers',
      queryParameters: {if (search != null && search.isNotEmpty) 'search': search},
      parser: (data) => (data as List<dynamic>)
          .map((e) => LookupItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<ProductLookupItem>> getProducts({String? search, String? categorySyncId}) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/master-data/products',
      queryParameters: {
        if (search != null && search.isNotEmpty) 'search': search,
        if (categorySyncId != null) 'categorySyncId': categorySyncId,
      },
      parser: (data) => (data as List<dynamic>)
          .map((e) => ProductLookupItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<LookupItem>> getSuppliers({String? search}) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/master-data/suppliers',
      queryParameters: {if (search != null && search.isNotEmpty) 'search': search},
      parser: (data) => (data as List<dynamic>)
          .map((e) => LookupItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<LookupItem>> getInvestors({String? search}) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/master-data/investors',
      queryParameters: {if (search != null && search.isNotEmpty) 'search': search},
      parser: (data) => (data as List<dynamic>)
          .map((e) => LookupItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<LookupItem>> getWarehouses({String? search}) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/master-data/warehouses',
      queryParameters: {if (search != null && search.isNotEmpty) 'search': search},
      parser: (data) => (data as List<dynamic>)
          .map((e) => LookupItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<LookupItem>> getCashBoxes({String? search}) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/master-data/cash-boxes',
      queryParameters: {if (search != null && search.isNotEmpty) 'search': search},
      parser: (data) => (data as List<dynamic>)
          .map((e) => LookupItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<LookupItem>> getBankAccounts({String? search}) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/master-data/bank-accounts',
      queryParameters: {if (search != null && search.isNotEmpty) 'search': search},
      parser: (data) => (data as List<dynamic>)
          .map((e) => LookupItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<LookupItem>> getExpenseTypes({String? search}) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/master-data/expense-types',
      queryParameters: {if (search != null && search.isNotEmpty) 'search': search},
      parser: (data) => (data as List<dynamic>)
          .map((e) => LookupItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<LookupItem>> getCategories({String? search}) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/master-data/categories',
      queryParameters: {if (search != null && search.isNotEmpty) 'search': search},
      parser: (data) => (data as List<dynamic>)
          .map((e) => LookupItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<PagedResult<InvoiceDetailResponse>> getInvoices({
    DateTime? from,
    DateTime? to,
    String? search,
    int? invoiceType,
    int? paymentMethod,
    String? customerSyncId,
    String? supplierSyncId,
    int page = 1,
    int pageSize = 50,
  }) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/invoices',
      queryParameters: {
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
        if (search != null && search.isNotEmpty) 'search': search,
        if (invoiceType != null) 'invoiceType': invoiceType,
        if (paymentMethod != null) 'paymentMethod': paymentMethod,
        if (customerSyncId != null) 'customerSyncId': customerSyncId,
        if (supplierSyncId != null) 'supplierSyncId': supplierSyncId,
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => PagedResult.fromJson(
        data as Map<String, dynamic>,
        (json) => InvoiceDetailResponse.fromJson(json),
      ),
    );
  }

  Future<InvoiceDetailResponse> getInvoiceDetail(String syncId) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/invoices/$syncId',
      parser: (data) =>
          InvoiceDetailResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<BusinessSettings> getBusinessSettings() {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/business-settings',
      parser: (data) =>
          BusinessSettings.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<List<PricingTypeLookupItem>> getPricingTypes({String? search}) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/master-data/pricing-types',
      queryParameters: {
        if (search != null && search.isNotEmpty) 'search': search,
      },
      parser: (data) => (data as List<dynamic>)
          .map((e) =>
              PricingTypeLookupItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<ProductPriceLookupItem>> getProductPrices({
    String? productSyncId,
    String? pricingTypeSyncId,
  }) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/master-data/product-prices',
      queryParameters: {
        if (productSyncId != null) 'productSyncId': productSyncId,
        if (pricingTypeSyncId != null) 'pricingTypeSyncId': pricingTypeSyncId,
      },
      parser: (data) => (data as List<dynamic>)
          .map((e) =>
              ProductPriceLookupItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
