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
}
