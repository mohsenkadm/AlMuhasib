import '../../../core/network/api_client.dart';
import '../../../shared/models/report_models.dart';

class ReportsRepository {
  ReportsRepository(this._apiClient);

  final ApiClient _apiClient;

  Map<String, dynamic> _dateParams(DateTime? from, DateTime? to) {
    return {
      if (from != null) 'from': from.toIso8601String(),
      if (to != null) 'to': to.toIso8601String(),
    };
  }

  Future<SalesReportResult> getSalesReport(DateTime? from, DateTime? to) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/reports/sales',
      queryParameters: _dateParams(from, to),
      parser: (data) =>
          SalesReportResult.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<PurchasesReportResult> getPurchasesReport(DateTime? from, DateTime? to) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/reports/purchases',
      queryParameters: _dateParams(from, to),
      parser: (data) =>
          PurchasesReportResult.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<ProfitReportResult> getProfitReport(DateTime? from, DateTime? to) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/reports/profit',
      queryParameters: _dateParams(from, to),
      parser: (data) =>
          ProfitReportResult.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<OverdueResult> getOverdueReport() {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/reports/installments/overdue',
      queryParameters: {'asOfDate': DateTime.now().toIso8601String()},
      parser: (data) => OverdueResult.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<CustomerStatementResult> getCustomerStatement(
    String customerSyncId, {
    DateTime? from,
    DateTime? to,
  }) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/reports/statements/customer',
      queryParameters: {
        'customerSyncId': customerSyncId,
        ..._dateParams(from, to),
      },
      parser: (data) =>
          CustomerStatementResult.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<InvestorStatementResult> getInvestorStatement(
    String investorSyncId, {
    DateTime? from,
    DateTime? to,
  }) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/reports/statements/investor',
      queryParameters: {
        'investorSyncId': investorSyncId,
        ..._dateParams(from, to),
      },
      parser: (data) =>
          InvestorStatementResult.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<List<WarehouseStockRow>> getWarehouseReport() {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/reports/warehouse',
      parser: (data) => (data as List<dynamic>)
          .map((e) => WarehouseStockRow.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<TopProductsReportResult> getTopProductsReport(
    DateTime? from,
    DateTime? to,
  ) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/reports/products/top',
      queryParameters: _dateParams(from, to),
      parser: (data) =>
          TopProductsReportResult.fromJson(data as Map<String, dynamic>),
    );
  }
}
