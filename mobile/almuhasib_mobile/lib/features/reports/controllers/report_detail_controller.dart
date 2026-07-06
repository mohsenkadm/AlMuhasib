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
  final isLoading = true.obs;
  final Rxn<Object> error = Rxn<Object>();
  final Rxn<dynamic> result = Rxn<dynamic>();
  final profitInvoices = <ProfitInvoiceDetailRow>[].obs;

  @override
  void onInit() {
    super.onInit();
    reload();
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
      case 'warehouse':
        return 'report_warehouse'.tr();
      case 'top_products':
        return 'report_top_products'.tr();
      default:
        return 'reports_title'.tr();
    }
  }

  bool get showDateFilter =>
      !{'overdue', 'warehouse', 'balance_sheet'}.contains(reportType);

  bool get singleDate => reportType == 'balance_sheet';
}
