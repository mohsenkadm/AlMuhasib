import '../../../core/network/api_client.dart';
import '../../../shared/models/mobile_models.dart';
import '../../../shared/models/paged_result.dart';

class FinanceRepository {
  FinanceRepository(this._apiClient);

  final ApiClient _apiClient;

  Future<PagedResult<VoucherListItem>> getVouchers({
    DateTime? from,
    DateTime? to,
    int? voucherType,
    String? search,
    int page = 1,
    int pageSize = 50,
  }) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/mobile/vouchers',
      queryParameters: {
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
        if (voucherType != null) 'voucherType': voucherType,
        if (search != null && search.isNotEmpty) 'search': search,
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => PagedResult.fromJson(
        data as Map<String, dynamic>,
        VoucherListItem.fromJson,
      ),
    );
  }

  Future<PagedResult<ExpenseListItem>> getExpenses({
    DateTime? from,
    DateTime? to,
    String? search,
    int page = 1,
    int pageSize = 50,
  }) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/mobile/expenses',
      queryParameters: {
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
        if (search != null && search.isNotEmpty) 'search': search,
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => PagedResult.fromJson(
        data as Map<String, dynamic>,
        ExpenseListItem.fromJson,
      ),
    );
  }

  Future<PagedResult<TransferListItem>> getTransfers({
    DateTime? from,
    DateTime? to,
    int page = 1,
    int pageSize = 50,
  }) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/mobile/transfers',
      queryParameters: {
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => PagedResult.fromJson(
        data as Map<String, dynamic>,
        TransferListItem.fromJson,
      ),
    );
  }

  Future<PagedResult<WarehouseStockListItem>> getWarehouseStocks({
    String? warehouseSyncId,
    String? productSyncId,
    String? search,
    int page = 1,
    int pageSize = 100,
  }) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/mobile/warehouse-stocks',
      queryParameters: {
        if (warehouseSyncId != null) 'warehouseSyncId': warehouseSyncId,
        if (productSyncId != null) 'productSyncId': productSyncId,
        if (search != null && search.isNotEmpty) 'search': search,
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => PagedResult.fromJson(
        data as Map<String, dynamic>,
        WarehouseStockListItem.fromJson,
      ),
    );
  }

  Future<PagedResult<WarehouseTransferListItem>> getWarehouseTransfers({
    DateTime? from,
    DateTime? to,
    int page = 1,
    int pageSize = 50,
  }) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/mobile/warehouse-transfers',
      queryParameters: {
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => PagedResult.fromJson(
        data as Map<String, dynamic>,
        WarehouseTransferListItem.fromJson,
      ),
    );
  }

  Future<PagedResult<InstallmentListItem>> getInstallments({
    String? status,
    String? customerSyncId,
    String? planSyncId,
    String? search,
    int page = 1,
    int pageSize = 50,
  }) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/mobile/installments',
      queryParameters: {
        if (status != null && status.isNotEmpty) 'status': status,
        if (customerSyncId != null) 'customerSyncId': customerSyncId,
        if (planSyncId != null) 'planSyncId': planSyncId,
        if (search != null && search.isNotEmpty) 'search': search,
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => PagedResult.fromJson(
        data as Map<String, dynamic>,
        InstallmentListItem.fromJson,
      ),
    );
  }

  Future<InstallmentPlanDetail> getInstallmentPlan(String syncId) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/mobile/installment-plans/$syncId',
      parser: (data) =>
          InstallmentPlanDetail.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<Map<String, dynamic>> notifyOverdueInstallments() {
    _apiClient.updateBaseUrl();
    return _apiClient.post(
      '/api/mobile/installments/notify-overdue',
      parser: (data) => Map<String, dynamic>.from(data as Map),
    );
  }
}
