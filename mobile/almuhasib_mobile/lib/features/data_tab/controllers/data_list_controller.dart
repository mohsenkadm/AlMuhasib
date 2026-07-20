import 'dart:async';

import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/models/master_data_models.dart';

class DataListController extends GetxController {
  DataListController({required this.listType});

  final String listType;

  final isLoading = true.obs;
  final Rxn<Object> error = Rxn<Object>();
  final items = <dynamic>[].obs;
  final search = ''.obs;
  final RxnInt invoiceTypeFilter = RxnInt();
  final RxnInt paymentFilter = RxnInt();
  final from = DateTime(DateTime.now().year - 2).obs;
  final to = DateTime.now().obs;

  Timer? _searchDebounce;

  @override
  void onInit() {
    super.onInit();
    reload();
  }

  @override
  void onClose() {
    _searchDebounce?.cancel();
    super.onClose();
  }

  Future<void> reload() async {
    isLoading.value = true;
    error.value = null;
    try {
      final repo = AppServices.data;
      final loaded = switch (listType) {
        'customers' => await repo.getCustomers(search: search.value),
        'products' => await repo.getProducts(search: search.value),
        'suppliers' => await repo.getSuppliers(search: search.value),
        'investors' => await repo.getInvestors(search: search.value),
        'warehouses' => await repo.getWarehouses(search: search.value),
        'cash-boxes' => await repo.getCashBoxes(search: search.value),
        'bank-accounts' => await repo.getBankAccounts(search: search.value),
        'invoices' => (await repo.getInvoices(
            from: from.value,
            to: to.value,
            search: search.value,
            invoiceType: invoiceTypeFilter.value,
            paymentMethod: paymentFilter.value,
          ))
            .items,
        _ => <dynamic>[],
      };
      items.value = loaded;
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }

  void updateSearch(String value) {
    search.value = value;
    _searchDebounce?.cancel();
    _searchDebounce = Timer(const Duration(milliseconds: 350), reload);
  }

  void updateInvoiceTypeFilter(String? id) {
    invoiceTypeFilter.value = id == null ? null : int.tryParse(id);
    reload();
  }

  void updatePaymentFilter(String? id) {
    paymentFilter.value = id == null ? null : int.tryParse(id);
    reload();
  }

  void clearFilters() {
    search.value = '';
    invoiceTypeFilter.value = null;
    paymentFilter.value = null;
    from.value = DateTime(DateTime.now().year - 2);
    to.value = DateTime.now();
    reload();
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

  String get title => switch (listType) {
        'customers' => 'customers'.tr(),
        'products' => 'products'.tr(),
        'suppliers' => 'suppliers'.tr(),
        'investors' => 'investors'.tr(),
        'warehouses' => 'warehouses'.tr(),
        'cash-boxes' => 'cash_boxes'.tr(),
        'bank-accounts' => 'bank_accounts'.tr(),
        'invoices' => 'invoices'.tr(),
        _ => 'data_title'.tr(),
      };

  String? get fabRoute => switch (listType) {
        'customers' => AppRoutes.customerNew,
        'products' => AppRoutes.productNew,
        'suppliers' => AppRoutes.supplierNew,
        'investors' => AppRoutes.investorNew,
        'warehouses' => AppRoutes.warehouseNew,
        'cash-boxes' => AppRoutes.cashBoxNew,
        'bank-accounts' => AppRoutes.bankAccountNew,
        'invoices' => AppRoutes.invoiceNew,
        _ => null,
      };

  String? detailRouteFor(LookupItem item) => switch (listType) {
        'customers' =>
          AppRoutes.customerDetailPath(item.syncId, name: item.name),
        'products' => AppRoutes.productDetailPath(item.syncId, name: item.name),
        'suppliers' =>
          AppRoutes.supplierDetailPath(item.syncId, name: item.name),
        'investors' =>
          AppRoutes.investorDetailPath(item.syncId, name: item.name),
        _ => null,
      };
}
