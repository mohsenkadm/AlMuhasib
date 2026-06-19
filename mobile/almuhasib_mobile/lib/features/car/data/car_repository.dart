import '../../../core/network/api_client.dart';
import '../models/car_models.dart';

class CarRepository {
  CarRepository(this._api);

  final ApiClient _api;

  Future<CarDashboardDto> getDashboard() {
    return _api.get(
      '/api/car/dashboard',
      parser: (data) => CarDashboardDto.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<List<CarContractListItem>> getContracts({
    String? search,
    String? status,
    int page = 1,
    int pageSize = 50,
  }) {
    return _api.get(
      '/api/car/contracts',
      queryParameters: {
        if (search != null && search.isNotEmpty) 'search': search,
        if (status != null && status.isNotEmpty) 'status': status,
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => (data as List<dynamic>)
          .map((e) => CarContractListItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<CarContractDetail> getContract(String syncId) {
    return _api.get(
      '/api/car/contracts/$syncId',
      parser: (data) => CarContractDetail.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<String> createContract(CreateCarContractRequest request) {
    return _api.post(
      '/api/car/contracts',
      data: request.toJson(),
      parser: (data) => (data as Map<String, dynamic>)['syncId'] as String,
    );
  }

  Future<void> recordPayment({
    required String contractSyncId,
    required double amount,
    required DateTime paymentDate,
    String? notes,
  }) {
    return _api.postVoid(
      '/api/car/contracts/$contractSyncId/payments',
      data: {
        'amount': amount,
        'paymentDate': paymentDate.toIso8601String(),
        if (notes != null) 'notes': notes,
      },
    );
  }

  Future<List<CarContractListItem>> getReport({
    DateTime? from,
    DateTime? to,
    String? status,
  }) {
    return _api.get(
      '/api/car/reports/contracts',
      queryParameters: {
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
        if (status != null) 'status': status,
      },
      parser: (data) => (data as List<dynamic>)
          .map((e) => CarContractListItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
