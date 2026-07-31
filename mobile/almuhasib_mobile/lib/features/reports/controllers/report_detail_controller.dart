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
  final isLoading = true.obs;
  final Rxn<Object> error = Rxn<Object>();
  final Rxn<dynamic> result = Rxn<dynamic>();
  final profitInvoices = <ProfitInvoiceDetailRow>[].obs;

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
          if (selectedCustomer.value == null) {
            final customers = await AppServices.data.getCustomers();
            if (customers.isNotEmpty) {
              selectedCustomer.value = customers.first;
            }
          } else {
            // حدّث الرصيد/الاسم من القائمة إن وُجد
            final customers = await AppServices.data.getCustomers();
            final syncId = selectedCustomer.value!.syncId;
            final match = customers.where((c) => c.syncId == syncId);
            if (match.isNotEmpty) {
              selectedCustomer.value = match.first;
            }
          }
          if (selectedCustomer.value != null) {
            loaded = await repo.getCustomerStatement(
              selectedCustomer.value!.syncId,
              from: from.value,
              to: to.value,
            );
          }
        case 'investor_statement':
          if (selectedInvestor.value == null) {
            final investors = await AppServices.data.getInvestors();
            if (investors.isNotEmpty) {
              selectedInvestor.value = investors.first;
            }
          }
          if (selectedInvestor.value != null) {
            loaded = await repo.getInvestorStatement(
              selectedInvestor.value!.syncId,
              from: from.value,
              to: to.value,
            );
          }
        case 'supplier_statement':
          if (selectedSupplier.value == null) {
            final suppliers = await AppServices.data.getSuppliers();
            if (suppliers.isNotEmpty) {
              selectedSupplier.value = suppliers.first;
            }
          }
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

  String get title {
    switch (reportType) {
      case 'sales':
        return 'report_sales'.tr();
      case 'purchases':
        return 'report_purchases'.tr();
      case 'profit':
        return 'report_profit'.tr();
      case 'balance_sheet':
        return 'report_balance_sheet'.tr();
      case 'overdue':
        return 'report_overdue'.tr();
      case 'statement':
        return 'report_statement'.tr();
      case 'investor_statement':
        return 'report_investor_statement'.tr();
      case 'supplier_statement':
        return 'report_supplier_statement'.tr();
      case 'warehouse':
        return 'report_warehouse'.tr();
      case 'top_products':
        return 'report_top_products'.tr();
      case 'expenses':
        return 'report_expenses'.tr();
      case 'income_expense':
        return 'report_income_expense'.tr();
      case 'cash_flow':
        return 'report_cash_flow'.tr();
      case 'installments_summary':
        return 'report_installments_summary'.tr();
      case 'installments_detail':
        return 'report_installments_detail'.tr();
      case 'installments_paid':
        return 'report_installments_paid'.tr();
      case 'installments_unpaid':
        return 'report_installments_unpaid'.tr();
      case 'installments_aging':
        return 'report_installments_aging'.tr();
      case 'product_margin':
        return 'report_product_margin'.tr();
      case 'product_movement':
        return 'report_product_movement'.tr();
      case 'stock_health':
        return 'report_stock_health'.tr();
      case 'inventory_replenishment':
        return 'report_inventory_replenishment'.tr();
      case 'customers_overview':
        return 'report_customers_overview'.tr();
      case 'suppliers_overview':
        return 'report_suppliers_overview'.tr();
      case 'profit_comparison':
        return 'report_profit_comparison'.tr();
      default:
        return 'reports_title'.tr();
    }
  }

  bool get showDateFilter => !{
        'overdue',
        'warehouse',
        'installments_unpaid',
        'installments_aging',
        'stock_health',
      }.contains(reportType);

  bool get singleDate => reportType == 'balance_sheet';
}
