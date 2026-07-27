import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/real_estate_models.dart';

class RealEstateReportController extends GetxController {
  final from = DateTime.now().subtract(const Duration(days: 30)).obs;
  final to = DateTime.now().obs;
  final statusFilter = RxnString();
  final isLoading = false.obs;
  final error = Rxn<Object>();
  final report = Rxn<RealEstateReportDto>();
  final profit = Rxn<RealEstateProfitReportDto>();
  final mode = 'contracts'.obs; // contracts | profit

  List<RealEstateContractListItem> get rows => report.value?.rows ?? const [];

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      if (mode.value == 'profit') {
        profit.value = await AppServices.realEstate.getProfitReport(
          from: from.value,
          to: to.value,
        );
      } else {
        report.value = await AppServices.realEstate.getReport(
          from: from.value,
          to: to.value,
          status: statusFilter.value,
        );
      }
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }

  void setMode(String value) {
    mode.value = value;
    load();
  }

  Future<void> pickFromDate() async {
    final ctx = Get.context;
    if (ctx == null) return;
    final d = await showDatePicker(
      context: ctx,
      initialDate: from.value,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (d != null) setFrom(d);
  }

  Future<void> pickToDate() async {
    final ctx = Get.context;
    if (ctx == null) return;
    final d = await showDatePicker(
      context: ctx,
      initialDate: to.value,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (d != null) setTo(d);
  }

  void setFrom(DateTime date) {
    from.value = date;
    load();
  }

  void setTo(DateTime date) {
    to.value = date;
    load();
  }

  void updateStatusFilter(String? status) {
    statusFilter.value = status;
    load();
  }

  void clearFilters() {
    statusFilter.value = null;
    from.value = DateTime.now().subtract(const Duration(days: 30));
    to.value = DateTime.now();
    load();
  }
}
