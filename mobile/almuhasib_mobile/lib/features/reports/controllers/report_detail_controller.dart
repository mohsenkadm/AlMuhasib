import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../shared/models/master_data_models.dart';
import '../../../shared/models/report_models.dart';
import '../../../shared/widgets/lookup_picker_sheet.dart';

class ReportDetailController extends GetxController {
  ReportDetailController({required this.reportType});

  final String reportType;

  final from = DateTime.now().subtract(const Duration(days: 30)).obs;
  final to = DateTime.now().obs;
  final Rxn<LookupItem> selectedCustomer = Rxn<LookupItem>();
  final Rxn<LookupItem> selectedInvestor = Rxn<LookupItem>();
  final Rxn<LookupItem> selectedSupplier = Rxn<LookupItem>();
  final Rxn<LookupItem> selectedWarehouse = Rxn<LookupItem>();
  final Rxn<LookupItem> selectedCashBox = Rxn<LookupItem>();
  final Rxn<LookupItem> selectedBankAccount = Rxn<LookupItem>();
  final isLoading = true.obs;
  final Rxn<Object> error = Rxn<Object>();
  final Rxn<dynamic> result = Rxn<dynamic>();
  final profitInvoices = <ProfitInvoiceDetailRow>[].obs;

  static const _noDateTypes = {
    'overdue',
    'warehouse',
    'installments_unpaid',
    'installments_aging',
    'stock_health',
    'cash_balances_summary',
    'inventory_valuation',
    'stock_taking',
  };

  static const _singleDateTypes = {
    'balance_sheet',
    'receivables_aging',
    'payables_aging',
    'overdue_customers',
    'financial_position_summary',
    'statement_of_financial_position',
  };

  static const _customerFilterTypes = {
    'statement',
    'opening_installment_balances',
    'company_fees',
    'installment_schedule',
    'receivables_aging',
    'customer_collections',
    'overdue_customers',
  };

  static const _supplierFilterTypes = {
    'supplier_statement',
    'payables_aging',
    'supplier_payments',
  };

  static const _investorFilterTypes = {
    'investor_statement',
    'investor_profit_distributions',
  };

  static const _warehouseFilterTypes = {
    'sales_by_payment_method',
    'daily_sales',
    'sales_by_warehouse_user',
    'inventory_valuation',
    'stock_taking',
    'cogs',
  };

  static const _cashBoxFilterTypes = {
    'customer_collections',
    'cash_box_movement',
  };

  static const _bankFilterTypes = {
    'bank_account_statement',
  };

  static const _requiredEntityTypes = {
    'statement',
    'investor_statement',
    'supplier_statement',
    'bank_account_statement',
    'cash_box_movement',
  };

  @override
  void onInit() {
    super.onInit();
    _applyRouteArguments();
    reload();
  }

  void _applyRouteArguments() {
    final args = Get.arguments;
    if (args is Map) {
      final syncId = args['customerSyncId']?.toString();
      final id = args['customerId'];
      final name = args['customerName']?.toString();
      if (syncId != null && syncId.isNotEmpty) {
        selectedCustomer.value = LookupItem(
          id: id is int ? id : int.tryParse(id?.toString() ?? '') ?? 0,
          syncId: syncId,
          name: name ?? '',
        );
      }
    } else if (args is LookupItem && reportType == 'statement') {
      selectedCustomer.value = args;
    }
  }

  Future<void> reload() async {
    isLoading.value = true;
    error.value = null;
    try {
      final repo = AppServices.reports;
      dynamic loaded;
      switch (reportType) {
        case 'sales':
          loaded = await repo.getSalesReport(from.value, to.value);
        case 'purchases':
          loaded = await repo.getPurchasesReport(from.value, to.value);
        case 'profit':
          loaded = await repo.getProfitReport(from.value, to.value);
          profitInvoices.value =
              await repo.getProfitInvoiceDetails(from.value, to.value);
        case 'balance_sheet':
          loaded = await repo.getBalanceSheet(to.value);
        case 'overdue':
          loaded = await repo.getOverdueReport();
        case 'warehouse':
          loaded = await repo.getWarehouseReport();
        case 'top_products':
          loaded = await repo.getTopProductsReport(from.value, to.value);
        case 'statement':
          await _ensureCustomer();
          if (selectedCustomer.value != null) {
            loaded = await repo.getCustomerStatement(
              selectedCustomer.value!.syncId,
              from: from.value,
              to: to.value,
            );
          }
        case 'investor_statement':
          await _ensureInvestor();
          if (selectedInvestor.value != null) {
            loaded = await repo.getInvestorStatement(
              selectedInvestor.value!.syncId,
              from: from.value,
              to: to.value,
            );
          }
        case 'supplier_statement':
          await _ensureSupplier();
          if (selectedSupplier.value != null) {
            loaded = await repo.getSupplierStatement(
              selectedSupplier.value!.syncId,
              from: from.value,
              to: to.value,
            );
          }
        case 'expenses':
          loaded = await repo.getExpensesReport(from.value, to.value);
        case 'income_expense':
          loaded = await repo.getIncomeExpenseReport(from.value, to.value);
        case 'cash_flow':
          loaded = await repo.getCashFlowReport(from.value, to.value);
        case 'installments_summary':
          loaded = await repo.getInstallmentsSummary(from.value, to.value);
        case 'installments_detail':
          loaded = await repo.getInstallmentsDetail(from.value, to.value);
        case 'installments_paid':
          loaded = await repo.getInstallmentsPaid(from.value, to.value);
        case 'installments_unpaid':
          loaded = await repo.getInstallmentsUnpaid();
        case 'installments_aging':
          loaded = await repo.getInstallmentsAging();
        case 'product_margin':
          loaded = await repo.getProductMargin(from.value, to.value);
        case 'product_movement':
          loaded = await repo.getProductMovement(from.value, to.value);
        case 'stock_health':
          loaded = await repo.getStockHealth();
        case 'inventory_replenishment':
          loaded = await repo.getInventoryReplenishment(from.value, to.value);
        case 'customers_overview':
          loaded = await repo.getCustomersOverview(from.value, to.value);
        case 'suppliers_overview':
          loaded = await repo.getSuppliersOverview(from.value, to.value);
        case 'profit_comparison':
          loaded = await repo.getProfitComparison(from.value, to.value);
        case 'investor_profit_distributions':
          loaded = await repo.getInvestorProfitDistributions(
            from: from.value,
            to: to.value,
            investorSyncId: selectedInvestor.value?.syncId,
          );
        case 'capital_movement':
          loaded = await repo.getCapitalMovement(
            from: from.value,
            to: to.value,
          );
        case 'opening_installment_balances':
          loaded = await repo.getOpeningInstallmentBalances(
            from: from.value,
            to: to.value,
            customerSyncId: selectedCustomer.value?.syncId,
          );
        case 'company_fees':
          loaded = await repo.getCompanyFees(
            from: from.value,
            to: to.value,
            customerSyncId: selectedCustomer.value?.syncId,
          );
        case 'installment_schedule':
          loaded = await repo.getInstallmentSchedule(
            from: from.value,
            to: to.value,
            customerSyncId: selectedCustomer.value?.syncId,
          );
        case 'sales_by_payment_method':
          loaded = await repo.getSalesByPaymentMethod(
            from: from.value,
            to: to.value,
            warehouseSyncId: selectedWarehouse.value?.syncId,
          );
        case 'daily_sales':
          loaded = await repo.getDailySales(
            from: from.value,
            to: to.value,
            warehouseSyncId: selectedWarehouse.value?.syncId,
          );
        case 'sales_by_warehouse_user':
          loaded = await repo.getSalesByWarehouseUser(
            from: from.value,
            to: to.value,
            warehouseSyncId: selectedWarehouse.value?.syncId,
          );
        case 'gross_profit_margin':
          loaded = await repo.getGrossProfitMargin(
            from: from.value,
            to: to.value,
          );
        case 'operating_profit':
          loaded = await repo.getOperatingProfit(
            from: from.value,
            to: to.value,
          );
        case 'receivables_aging':
          loaded = await repo.getReceivablesAging(
            asOf: to.value,
            customerSyncId: selectedCustomer.value?.syncId,
          );
        case 'payables_aging':
          loaded = await repo.getPayablesAging(
            asOf: to.value,
            supplierSyncId: selectedSupplier.value?.syncId,
          );
        case 'customer_collections':
          loaded = await repo.getCustomerCollections(
            from: from.value,
            to: to.value,
            customerSyncId: selectedCustomer.value?.syncId,
            cashBoxSyncId: selectedCashBox.value?.syncId,
          );
        case 'overdue_customers':
          loaded = await repo.getOverdueCustomers(
            asOf: to.value,
            customerSyncId: selectedCustomer.value?.syncId,
          );
        case 'supplier_payments':
          loaded = await repo.getSupplierPayments(
            from: from.value,
            to: to.value,
            supplierSyncId: selectedSupplier.value?.syncId,
          );
        case 'bank_account_statement':
          await _ensureBankAccount();
          if (selectedBankAccount.value != null) {
            loaded = await repo.getBankAccountStatement(
              from: from.value,
              to: to.value,
              bankAccountSyncId: selectedBankAccount.value!.syncId,
            );
          }
        case 'cash_box_movement':
          await _ensureCashBox();
          if (selectedCashBox.value != null) {
            loaded = await repo.getCashBoxMovement(
              from: from.value,
              to: to.value,
              cashBoxSyncId: selectedCashBox.value!.syncId,
            );
          }
        case 'cash_balances_summary':
          loaded = await repo.getCashBalancesSummary();
        case 'transfers':
          loaded = await repo.getTransfersReport(
            from: from.value,
            to: to.value,
          );
        case 'inventory_valuation':
          loaded = await repo.getInventoryValuation(
            warehouseSyncId: selectedWarehouse.value?.syncId,
          );
        case 'stock_taking':
          loaded = await repo.getStockTaking(
            warehouseSyncId: selectedWarehouse.value?.syncId,
          );
        case 'cogs':
          loaded = await repo.getCogsReport(
            from: from.value,
            to: to.value,
            warehouseSyncId: selectedWarehouse.value?.syncId,
          );
        case 'financial_position_summary':
          loaded = await repo.getFinancialPositionSummary(asOf: to.value);
        case 'profit_and_loss':
          loaded = await repo.getProfitAndLoss(
            from: from.value,
            to: to.value,
          );
        case 'statement_of_financial_position':
          loaded = await repo.getStatementOfFinancialPosition(asOf: to.value);
        default:
          loaded = null;
      }
      result.value = loaded;
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }

  Future<void> _ensureCustomer() async {
    if (selectedCustomer.value == null) {
      final customers = await AppServices.data.getCustomers();
      if (customers.isNotEmpty) {
        selectedCustomer.value = customers.first;
      }
    } else {
      final customers = await AppServices.data.getCustomers();
      final syncId = selectedCustomer.value!.syncId;
      final match = customers.where((c) => c.syncId == syncId);
      if (match.isNotEmpty) {
        selectedCustomer.value = match.first;
      }
    }
  }

  Future<void> _ensureInvestor() async {
    if (selectedInvestor.value == null) {
      final investors = await AppServices.data.getInvestors();
      if (investors.isNotEmpty) {
        selectedInvestor.value = investors.first;
      }
    }
  }

  Future<void> _ensureSupplier() async {
    if (selectedSupplier.value == null) {
      final suppliers = await AppServices.data.getSuppliers();
      if (suppliers.isNotEmpty) {
        selectedSupplier.value = suppliers.first;
      }
    }
  }

  Future<void> _ensureCashBox() async {
    if (selectedCashBox.value == null) {
      final boxes = await AppServices.data.getCashBoxes();
      if (boxes.isNotEmpty) {
        selectedCashBox.value = boxes.first;
      }
    }
  }

  Future<void> _ensureBankAccount() async {
    if (selectedBankAccount.value == null) {
      final accounts = await AppServices.data.getBankAccounts();
      if (accounts.isNotEmpty) {
        selectedBankAccount.value = accounts.first;
      }
    }
  }

  Future<void> pickFromDate(BuildContext context) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: from.value,
      firstDate: DateTime(2020),
      lastDate: DateTime.now(),
    );
    if (picked != null) {
      from.value = picked;
      await reload();
    }
  }

  Future<void> pickToDate(BuildContext context) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: to.value,
      firstDate: DateTime(2020),
      lastDate: DateTime.now(),
    );
    if (picked != null) {
      to.value = picked;
      await reload();
    }
  }

  Future<void> pickCustomer(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_customer'.tr(),
      loadItems: (search) => AppServices.data.getCustomers(search: search),
    );
    if (selected != null) {
      selectedCustomer.value = selected;
      await reload();
    }
  }

  Future<void> pickInvestor(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_investor'.tr(),
      loadItems: (search) => AppServices.data.getInvestors(search: search),
    );
    if (selected != null) {
      selectedInvestor.value = selected;
      await reload();
    }
  }

  Future<void> pickSupplier(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_supplier'.tr(),
      loadItems: (search) => AppServices.data.getSuppliers(search: search),
    );
    if (selected != null) {
      selectedSupplier.value = selected;
      await reload();
    }
  }

  Future<void> pickWarehouse(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_warehouse'.tr(),
      loadItems: (search) => AppServices.data.getWarehouses(search: search),
    );
    if (selected != null) {
      selectedWarehouse.value = selected;
      await reload();
    }
  }

  Future<void> pickCashBox(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_cashbox'.tr(),
      loadItems: (search) => AppServices.data.getCashBoxes(search: search),
    );
    if (selected != null) {
      selectedCashBox.value = selected;
      await reload();
    }
  }

  Future<void> pickBankAccount(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_bank_account'.tr(),
      loadItems: (search) => AppServices.data.getBankAccounts(search: search),
    );
    if (selected != null) {
      selectedBankAccount.value = selected;
      await reload();
    }
  }

  void clearCustomer() {
    selectedCustomer.value = null;
    reload();
  }

  void clearSupplier() {
    selectedSupplier.value = null;
    reload();
  }

  void clearInvestor() {
    selectedInvestor.value = null;
    reload();
  }

  void clearWarehouse() {
    selectedWarehouse.value = null;
    reload();
  }

  void clearCashBox() {
    if (_requiredEntityTypes.contains(reportType)) return;
    selectedCashBox.value = null;
    reload();
  }

  String get title => 'report_$reportType'.tr();

  bool get showDateFilter => !_noDateTypes.contains(reportType);

  bool get singleDate => _singleDateTypes.contains(reportType);

  bool get showCustomerPicker => _customerFilterTypes.contains(reportType);

  bool get showSupplierPicker => _supplierFilterTypes.contains(reportType);

  bool get showInvestorPicker => _investorFilterTypes.contains(reportType);

  bool get showWarehousePicker => _warehouseFilterTypes.contains(reportType);

  bool get showCashBoxPicker => _cashBoxFilterTypes.contains(reportType);

  bool get showBankPicker => _bankFilterTypes.contains(reportType);
}
