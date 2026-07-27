import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/car_trade_models.dart';

class CarTradeTransactionsController extends GetxController {
  final search = ''.obs;
  final statusFilter = RxnString();
  final tradeTypeFilter = RxnString();
  final from = DateTime.now().subtract(const Duration(days: 365)).obs;
  final to = DateTime.now().obs;
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final items = <CarTradeTransactionListItem>[].obs;

  @override
  void onInit() {
    super.onInit();
    ever(search, (_) => load());
    load();
  }

  void updateSearch(String value) => search.value = value;

  void updateStatusFilter(String? status) {
    statusFilter.value = status;
    load();
  }

  void updateTradeTypeFilter(String? tradeType) {
    tradeTypeFilter.value = tradeType;
    load();
  }

  Future<void> pickFromDate() async {
    final ctx = Get.context;
    if (ctx == null) return;
    final picked = await showDatePicker(
      context: ctx,
      initialDate: from.value,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (picked != null) {
      from.value = picked;
      load();
    }
  }

  Future<void> pickToDate() async {
    final ctx = Get.context;
    if (ctx == null) return;
    final picked = await showDatePicker(
      context: ctx,
      initialDate: to.value,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (picked != null) {
      to.value = picked;
      load();
    }
  }

  void clearFilters() {
    search.value = '';
    statusFilter.value = null;
    tradeTypeFilter.value = null;
    from.value = DateTime.now().subtract(const Duration(days: 365));
    to.value = DateTime.now();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      items.value = await AppServices.carTrade.getTransactions(
        search: search.value.trim(),
        status: statusFilter.value,
        tradeType: tradeTypeFilter.value,
        from: from.value,
        to: to.value,
      );
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
