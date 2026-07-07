import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/car_trade_models.dart';

class CarTradePartyStatementController extends GetxController {
  final partyNameController = TextEditingController();
  final partyPhoneController = TextEditingController();
  final from = Rxn<DateTime>();
  final to = Rxn<DateTime>();
  final isLoading = false.obs;
  final error = Rxn<Object>();
  final statement = Rxn<CarTradePartyStatementDto>();

  @override
  void onClose() {
    partyNameController.dispose();
    partyPhoneController.dispose();
    super.onClose();
  }

  Future<void> load() async {
    final name = partyNameController.text.trim();
    if (name.isEmpty) {
      error.value = 'car_trade_party_name_required';
      return;
    }

    isLoading.value = true;
    error.value = null;
    try {
      statement.value = await AppServices.carTrade.getPartyStatement(
        partyName: name,
        partyPhone: partyPhoneController.text.trim().isEmpty
            ? null
            : partyPhoneController.text.trim(),
        from: from.value,
        to: to.value,
      );
    } catch (e) {
      error.value = e;
      statement.value = null;
    } finally {
      isLoading.value = false;
    }
  }

  Future<void> pickFromDate() async {
    final ctx = Get.context;
    if (ctx == null) return;
    final d = await showDatePicker(
      context: ctx,
      initialDate: from.value ?? DateTime.now(),
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (d != null) from.value = d;
  }

  Future<void> pickToDate() async {
    final ctx = Get.context;
    if (ctx == null) return;
    final d = await showDatePicker(
      context: ctx,
      initialDate: to.value ?? DateTime.now(),
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (d != null) to.value = d;
  }

  void clearFilters() {
    from.value = null;
    to.value = null;
  }
}
