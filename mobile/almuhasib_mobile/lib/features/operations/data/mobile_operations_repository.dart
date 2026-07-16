import '../../../core/network/api_client.dart';
import '../../../shared/models/mobile_models.dart';

class MobileOperationsRepository {
  MobileOperationsRepository(this._apiClient);

  final ApiClient _apiClient;

  Future<MobileWriteResponse> createCustomer(CreateCustomerRequest request) {
    _apiClient.updateBaseUrl();
    return _apiClient.post(
      '/api/mobile/customers',
      data: request.toJson(),
      parser: (data) =>
          MobileWriteResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<MobileWriteResponse> createSupplier(CreateSupplierRequest request) {
    _apiClient.updateBaseUrl();
    return _apiClient.post(
      '/api/mobile/suppliers',
      data: request.toJson(),
      parser: (data) =>
          MobileWriteResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<MobileWriteResponse> createProduct(CreateProductRequest request) {
    _apiClient.updateBaseUrl();
    return _apiClient.post(
      '/api/mobile/products',
      data: request.toJson(),
      parser: (data) =>
          MobileWriteResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<MobileWriteResponse> createInvestor(CreateInvestorRequest request) {
    _apiClient.updateBaseUrl();
    return _apiClient.post(
      '/api/mobile/investors',
      data: request.toJson(),
      parser: (data) =>
          MobileWriteResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<MobileWriteResponse> createInvoice(CreateInvoiceRequest request) {
    _apiClient.updateBaseUrl();
    return _apiClient.post(
      '/api/mobile/invoices',
      data: request.toJson(),
      parser: (data) =>
          MobileWriteResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<MobileWriteResponse> upsertPricingType(UpsertPricingTypeRequest request) {
    _apiClient.updateBaseUrl();
    final syncId = request.syncId;
    if (syncId != null && syncId.isNotEmpty) {
      return _apiClient.put(
        '/api/mobile/pricing-types/$syncId',
        data: request.toJson(),
        parser: (data) =>
            MobileWriteResponse.fromJson(data as Map<String, dynamic>),
      );
    }
    return _apiClient.post(
      '/api/mobile/pricing-types',
      data: request.toJson(),
      parser: (data) =>
          MobileWriteResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<MobileWriteResponse> deletePricingType(String syncId) {
    _apiClient.updateBaseUrl();
    return _apiClient.delete(
      '/api/mobile/pricing-types/$syncId',
      parser: (data) =>
          MobileWriteResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<MobileWriteResponse> upsertProductPrice(UpsertProductPriceRequest request) {
    _apiClient.updateBaseUrl();
    final syncId = request.syncId;
    if (syncId != null && syncId.isNotEmpty) {
      return _apiClient.put(
        '/api/mobile/product-prices/$syncId',
        data: request.toJson(),
        parser: (data) =>
            MobileWriteResponse.fromJson(data as Map<String, dynamic>),
      );
    }
    return _apiClient.post(
      '/api/mobile/product-prices',
      data: request.toJson(),
      parser: (data) =>
          MobileWriteResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<MobileWriteResponse> deleteProductPrice(String syncId) {
    _apiClient.updateBaseUrl();
    return _apiClient.delete(
      '/api/mobile/product-prices/$syncId',
      parser: (data) =>
          MobileWriteResponse.fromJson(data as Map<String, dynamic>),
    );
  }
}
