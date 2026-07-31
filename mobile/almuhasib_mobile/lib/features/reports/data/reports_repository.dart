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

  Future<List<ProfitInvoiceDetailRow>> getProfitInvoiceDetails(
    DateTime? from,
    DateTime? to,
  ) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/reports/profit/invoices',
      queryParameters: _dateParams(from, to),
      parser: (data) => (data as List<dynamic>)
          .map((e) => ProfitInvoiceDetailRow.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<BalanceSheetResult> getBalanceSheet(DateTime date) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/reports/balance-sheet',
      queryParameters: {'asOfDate': date.toIso8601String()},
      parser: (data) =>
          BalanceSheetResult.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<dynamic> getGenericReport(
    String path, {
    Map<String, dynamic>? query,
  }) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      path,
      queryParameters: query,
      parser: (data) => data,
    );
  }

  Future<dynamic> getSupplierStatement(
    String supplierSyncId, {
    DateTime? from,
    DateTime? to,
  }) {
    return getGenericReport(
      '/api/reports/statements/supplier',
      query: {
        'supplierSyncId': supplierSyncId,
        ..._dateParams(from, to),
      },
    );
  }

  Future<dynamic> getExpensesReport(DateTime? from, DateTime? to) {
    return getGenericReport(
      '/api/reports/expenses',
      query: _dateParams(from, to),
    );
  }

  Future<dynamic> getIncomeExpenseReport(DateTime? from, DateTime? to) {
    return getGenericReport(
      '/api/reports/income-expense',
      query: _dateParams(from, to),
    );
  }

  Future<dynamic> getCashFlowReport(DateTime? from, DateTime? to) {
    return getGenericReport(
      '/api/reports/cash-flow',
      query: _dateParams(from, to),
    );
  }

  Future<dynamic> getInstallmentsSummary(DateTime? from, DateTime? to) {
    return getGenericReport(
      '/api/reports/installments/summary',
      query: _dateParams(from, to),
    );
  }

  Future<dynamic> getInstallmentsDetail(DateTime? from, DateTime? to) {
    return getGenericReport(
      '/api/reports/installments/detail',
      query: _dateParams(from, to),
    );
  }

  Future<dynamic> getInstallmentsPaid(DateTime? from, DateTime? to) {
    return getGenericReport(
      '/api/reports/installments/paid',
      query: _dateParams(from, to),
    );
  }

  Future<dynamic> getInstallmentsUnpaid() {
    return getGenericReport('/api/reports/installments/unpaid');
  }

  Future<dynamic> getInstallmentsAging() {
    return getGenericReport(
      '/api/reports/installments/aging',
      query: {'asOfDate': DateTime.now().toIso8601String()},
    );
  }

  Future<dynamic> getProductMargin(DateTime? from, DateTime? to) {
    return getGenericReport(
      '/api/reports/products/margin',
      query: _dateParams(from, to),
    );
  }

  Future<dynamic> getProductMovement(DateTime? from, DateTime? to) {
    return getGenericReport(
      '/api/reports/products/movement',
      query: _dateParams(from, to),
    );
  }

  Future<dynamic> getStockHealth() {
    return getGenericReport('/api/reports/stock-health');
  }

  Future<dynamic> getInventoryReplenishment(DateTime? from, DateTime? to) {
    return getGenericReport(
      '/api/reports/inventory-replenishment',
      query: _dateParams(from, to),
    );
  }

  Future<dynamic> getCustomersOverview(DateTime? from, DateTime? to) {
    return getGenericReport(
      '/api/reports/customers/overview',
      query: _dateParams(from, to),
    );
  }

  Future<dynamic> getSuppliersOverview(DateTime? from, DateTime? to) {
    return getGenericReport(
      '/api/reports/suppliers/overview',
      query: _dateParams(from, to),
    );
  }

  Future<dynamic> getProfitComparison(DateTime? from, DateTime? to) {
    return getGenericReport(
      '/api/reports/profit/comparison',
      query: _dateParams(from, to),
    );
  }

  // ── New extended reports ──────────────────────────────────────

  Map<String, dynamic> _filterParams({
    DateTime? from,
    DateTime? to,
    DateTime? asOfDate,
    String? customerSyncId,
    String? supplierSyncId,
    String? warehouseSyncId,
    String? cashBoxSyncId,
    String? bankAccountSyncId,
    String? investorSyncId,
    String? status,
    int? minDaysOverdue,
    bool? includeZero,
    String? paymentMethod,
  }) {
    return {
      ..._dateParams(from, to),
      if (asOfDate != null) 'asOfDate': asOfDate.toIso8601String(),
      if (customerSyncId != null && customerSyncId.isNotEmpty)
        'customerSyncId': customerSyncId,
      if (supplierSyncId != null && supplierSyncId.isNotEmpty)
        'supplierSyncId': supplierSyncId,
      if (warehouseSyncId != null && warehouseSyncId.isNotEmpty)
        'warehouseSyncId': warehouseSyncId,
      if (cashBoxSyncId != null && cashBoxSyncId.isNotEmpty)
        'cashBoxSyncId': cashBoxSyncId,
      if (bankAccountSyncId != null && bankAccountSyncId.isNotEmpty)
        'bankAccountSyncId': bankAccountSyncId,
      if (investorSyncId != null && investorSyncId.isNotEmpty)
        'investorSyncId': investorSyncId,
      if (status != null && status.isNotEmpty) 'status': status,
      if (minDaysOverdue != null) 'minDaysOverdue': minDaysOverdue,
      if (includeZero != null) 'includeZero': includeZero,
      if (paymentMethod != null && paymentMethod.isNotEmpty)
        'paymentMethod': paymentMethod,
    };
  }

  Future<dynamic> getInvestorProfitDistributions({
    DateTime? from,
    DateTime? to,
    String? investorSyncId,
  }) =>
      getGenericReport(
        '/api/reports/investor-profit-distributions',
        query: _filterParams(
          from: from,
          to: to,
          investorSyncId: investorSyncId,
        ),
      );

  Future<dynamic> getCapitalMovement({DateTime? from, DateTime? to}) =>
      getGenericReport(
        '/api/reports/capital-movement',
        query: _filterParams(from: from, to: to),
      );

  Future<dynamic> getOpeningInstallmentBalances({
    DateTime? from,
    DateTime? to,
    String? customerSyncId,
  }) =>
      getGenericReport(
        '/api/reports/opening-installment-balances',
        query: _filterParams(
          from: from,
          to: to,
          customerSyncId: customerSyncId,
        ),
      );

  Future<dynamic> getCompanyFees({
    DateTime? from,
    DateTime? to,
    String? customerSyncId,
  }) =>
      getGenericReport(
        '/api/reports/company-fees',
        query: _filterParams(
          from: from,
          to: to,
          customerSyncId: customerSyncId,
        ),
      );

  Future<dynamic> getInstallmentSchedule({
    DateTime? from,
    DateTime? to,
    String? customerSyncId,
    String? status,
  }) =>
      getGenericReport(
        '/api/reports/installment-schedule',
        query: _filterParams(
          from: from,
          to: to,
          customerSyncId: customerSyncId,
          status: status,
        ),
      );

  Future<dynamic> getSalesByPaymentMethod({
    DateTime? from,
    DateTime? to,
    String? warehouseSyncId,
  }) =>
      getGenericReport(
        '/api/reports/sales-by-payment-method',
        query: _filterParams(
          from: from,
          to: to,
          warehouseSyncId: warehouseSyncId,
        ),
      );

  Future<dynamic> getDailySales({
    DateTime? from,
    DateTime? to,
    String? warehouseSyncId,
    String? paymentMethod,
  }) =>
      getGenericReport(
        '/api/reports/daily-sales',
        query: _filterParams(
          from: from,
          to: to,
          warehouseSyncId: warehouseSyncId,
          paymentMethod: paymentMethod,
        ),
      );

  Future<dynamic> getSalesByWarehouseUser({
    DateTime? from,
    DateTime? to,
    String? warehouseSyncId,
  }) =>
      getGenericReport(
        '/api/reports/sales-by-warehouse-user',
        query: _filterParams(
          from: from,
          to: to,
          warehouseSyncId: warehouseSyncId,
        ),
      );

  Future<dynamic> getGrossProfitMargin({DateTime? from, DateTime? to}) =>
      getGenericReport(
        '/api/reports/gross-profit-margin',
        query: _filterParams(from: from, to: to),
      );

  Future<dynamic> getOperatingProfit({DateTime? from, DateTime? to}) =>
      getGenericReport(
        '/api/reports/operating-profit',
        query: _filterParams(from: from, to: to),
      );

  Future<dynamic> getReceivablesAging({
    DateTime? asOf,
    String? customerSyncId,
  }) =>
      getGenericReport(
        '/api/reports/receivables-aging',
        query: _filterParams(
          to: asOf ?? DateTime.now(),
          customerSyncId: customerSyncId,
        ),
      );

  Future<dynamic> getPayablesAging({
    DateTime? asOf,
    String? supplierSyncId,
  }) =>
      getGenericReport(
        '/api/reports/payables-aging',
        query: _filterParams(
          to: asOf ?? DateTime.now(),
          supplierSyncId: supplierSyncId,
        ),
      );

  Future<dynamic> getCustomerCollections({
    DateTime? from,
    DateTime? to,
    String? customerSyncId,
    String? cashBoxSyncId,
  }) =>
      getGenericReport(
        '/api/reports/customer-collections',
        query: _filterParams(
          from: from,
          to: to,
          customerSyncId: customerSyncId,
          cashBoxSyncId: cashBoxSyncId,
        ),
      );

  Future<dynamic> getOverdueCustomers({
    DateTime? asOf,
    int? minDaysOverdue,
    String? customerSyncId,
  }) =>
      getGenericReport(
        '/api/reports/overdue-customers',
        query: _filterParams(
          to: asOf ?? DateTime.now(),
          minDaysOverdue: minDaysOverdue ?? 1,
          customerSyncId: customerSyncId,
        ),
      );

  Future<dynamic> getSupplierPayments({
    DateTime? from,
    DateTime? to,
    String? supplierSyncId,
  }) =>
      getGenericReport(
        '/api/reports/supplier-payments',
        query: _filterParams(
          from: from,
          to: to,
          supplierSyncId: supplierSyncId,
        ),
      );

  Future<dynamic> getBankAccountStatement({
    DateTime? from,
    DateTime? to,
    String? bankAccountSyncId,
  }) =>
      getGenericReport(
        '/api/reports/bank-account-statement',
        query: _filterParams(
          from: from,
          to: to,
          bankAccountSyncId: bankAccountSyncId,
        ),
      );

  Future<dynamic> getCashBoxMovement({
    DateTime? from,
    DateTime? to,
    String? cashBoxSyncId,
  }) =>
      getGenericReport(
        '/api/reports/cash-box-movement',
        query: _filterParams(
          from: from,
          to: to,
          cashBoxSyncId: cashBoxSyncId,
        ),
      );

  Future<dynamic> getCashBalancesSummary() =>
      getGenericReport('/api/reports/cash-balances-summary');

  Future<dynamic> getTransfersReport({DateTime? from, DateTime? to}) =>
      getGenericReport(
        '/api/reports/transfers',
        query: _filterParams(from: from, to: to),
      );

  Future<dynamic> getInventoryValuation({
    String? warehouseSyncId,
    bool includeZero = false,
  }) =>
      getGenericReport(
        '/api/reports/inventory-valuation',
        query: _filterParams(
          warehouseSyncId: warehouseSyncId,
          includeZero: includeZero,
        ),
      );

  Future<dynamic> getStockTaking({
    String? warehouseSyncId,
    bool includeZero = true,
  }) =>
      getGenericReport(
        '/api/reports/stock-taking',
        query: _filterParams(
          warehouseSyncId: warehouseSyncId,
          includeZero: includeZero,
        ),
      );

  Future<dynamic> getCogsReport({
    DateTime? from,
    DateTime? to,
    String? warehouseSyncId,
  }) =>
      getGenericReport(
        '/api/reports/cogs',
        query: _filterParams(
          from: from,
          to: to,
          warehouseSyncId: warehouseSyncId,
        ),
      );

  Future<dynamic> getFinancialPositionSummary({DateTime? asOf}) =>
      getGenericReport(
        '/api/reports/financial-position-summary',
        query: _filterParams(to: asOf ?? DateTime.now()),
      );

  Future<dynamic> getProfitAndLoss({DateTime? from, DateTime? to}) =>
      getGenericReport(
        '/api/reports/profit-and-loss',
        query: _filterParams(from: from, to: to),
      );

  Future<dynamic> getStatementOfFinancialPosition({DateTime? asOf}) =>
      getGenericReport(
        '/api/reports/statement-of-financial-position',
        query: _filterParams(to: asOf ?? DateTime.now()),
      );
}
