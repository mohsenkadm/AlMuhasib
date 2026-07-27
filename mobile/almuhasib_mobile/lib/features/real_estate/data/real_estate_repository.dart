import '../../../core/network/api_client.dart';
import '../models/real_estate_models.dart';

class RealEstateRepository {
  RealEstateRepository(this._api);

  final ApiClient _api;

  Future<RealEstateDashboardDto> getDashboard() {
    return _api.get(
      '/api/real-estate/dashboard',
      parser: (data) =>
          RealEstateDashboardDto.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<List<RealEstateContractListItem>> getContracts({
    String? search,
    String? status,
    DateTime? from,
    DateTime? to,
    bool? hasRemaining,
    int page = 1,
    int pageSize = 50,
  }) {
    return _api.get(
      '/api/real-estate/contracts',
      queryParameters: {
        if (search != null && search.isNotEmpty) 'search': search,
        if (status != null && status.isNotEmpty) 'status': status,
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
        if (hasRemaining == true) 'hasRemaining': true,
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => (data as List<dynamic>)
          .map(
            (e) => RealEstateContractListItem.fromJson(
              e as Map<String, dynamic>,
            ),
          )
          .toList(),
    );
  }

  Future<RealEstateContractDetail> getContract(String syncId) {
    return _api.get(
      '/api/real-estate/contracts/$syncId',
      parser: (data) =>
          RealEstateContractDetail.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<String> createContract(CreateRealEstateContractRequest request) {
    return _api.post(
      '/api/real-estate/contracts',
      data: request.toJson(),
      parser: (data) => (data as Map<String, dynamic>)['syncId'].toString(),
    );
  }

  Future<void> updateContract(
    String syncId,
    CreateRealEstateContractRequest request,
  ) async {
    await _api.put(
      '/api/real-estate/contracts/$syncId',
      data: request.toJson(),
      parser: (_) => null,
    );
  }

  Future<void> deleteContract(String syncId) async {
    await _api.delete(
      '/api/real-estate/contracts/$syncId',
      parser: (_) => null,
    );
  }

  Future<void> recordPayment({
    required String contractSyncId,
    required double amount,
    required DateTime paymentDate,
    String? notes,
  }) {
    return _api.postVoid(
      '/api/real-estate/contracts/$contractSyncId/payments',
      data: {
        'amount': amount,
        'paymentDate': paymentDate.toIso8601String(),
        if (notes != null) 'notes': notes,
      },
    );
  }

  Future<List<RealEstateDebtItem>> getDebts({bool overdueOnly = false}) {
    return _api.get(
      '/api/real-estate/debts',
      queryParameters: {
        if (overdueOnly) 'overdueOnly': true,
      },
      parser: (data) => (data as List<dynamic>)
          .map((e) => RealEstateDebtItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<RealEstateReportDto> getReport({
    DateTime? from,
    DateTime? to,
    String? status,
  }) {
    return _api.get(
      '/api/real-estate/reports/contracts',
      queryParameters: {
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
        if (status != null) 'status': status,
      },
      parser: (data) {
        if (data is List) {
          final rows = data
              .map(
                (e) => RealEstateContractListItem.fromJson(
                  e as Map<String, dynamic>,
                ),
              )
              .toList();
          return RealEstateReportDto(
            rows: rows,
            contractCount: rows.length,
            totalValue: rows.fold<double>(0, (s, e) => s + e.totalPrice),
            totalReceived: rows.fold<double>(0, (s, e) => s + e.amountPaid),
            totalRemaining:
                rows.fold<double>(0, (s, e) => s + e.remainingAmount),
          );
        }
        return RealEstateReportDto.fromJson(data as Map<String, dynamic>);
      },
    );
  }

  Future<RealEstateProfitReportDto> getProfitReport({
    DateTime? from,
    DateTime? to,
  }) {
    return _api.get(
      '/api/real-estate/reports/profit',
      queryParameters: {
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
      },
      parser: (data) =>
          RealEstateProfitReportDto.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<List<RealEstateExpenseTypeDto>> getExpenseTypes() {
    return _api.get(
      '/api/real-estate/expenses/types',
      parser: (data) => (data as List<dynamic>)
          .map(
            (e) => RealEstateExpenseTypeDto.fromJson(
              e as Map<String, dynamic>,
            ),
          )
          .toList(),
    );
  }

  Future<RealEstateExpensesPage> getExpenses({
    DateTime? from,
    DateTime? to,
    String? search,
    String? typeSyncId,
    int page = 1,
    int pageSize = 50,
  }) {
    return _api.get(
      '/api/real-estate/expenses',
      queryParameters: {
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
        if (search != null && search.isNotEmpty) 'search': search,
        if (typeSyncId != null && typeSyncId.isNotEmpty)
          'typeSyncId': typeSyncId,
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) =>
          RealEstateExpensesPage.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<String> createExpense({
    required String expenseTypeSyncId,
    required DateTime expenseDate,
    required double amount,
    String? description,
    String? notes,
    String? relatedContractSyncId,
  }) {
    return _api.post(
      '/api/real-estate/expenses',
      data: {
        'expenseTypeSyncId': expenseTypeSyncId,
        'expenseDate': expenseDate.toIso8601String(),
        'amount': amount,
        if (description != null) 'description': description,
        if (notes != null) 'notes': notes,
        if (relatedContractSyncId != null)
          'relatedContractSyncId': relatedContractSyncId,
      },
      parser: (data) => (data as Map<String, dynamic>)['syncId'].toString(),
    );
  }

  Future<void> deleteExpense(String syncId) {
    return _api.delete(
      '/api/real-estate/expenses/$syncId',
      parser: (_) => null,
    );
  }
}
