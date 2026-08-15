import 'package:almuhasib_mobile/core/getx/app_services.dart';
import 'package:almuhasib_mobile/features/real_estate/models/real_estate_models.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

class RealEstateExpensesController extends GetxController {
  final from = DateTime.now().subtract(const Duration(days: 30)).obs;
  final to = DateTime.now().obs;
  final search = ''.obs;
  final isLoading = false.obs;
  final error = Rxn<Object>();
  final page = Rxn<RealEstateExpensesPage>();
  final types = <RealEstateExpenseTypeDto>[].obs;
  final selectedTypeSyncId = RxnString();

  final amountCtrl = TextEditingController();
  final descriptionCtrl = TextEditingController();
  final selectedTypeForForm = Rxn<RealEstateExpenseTypeDto>();
  final expenseDate = DateTime.now().obs;

  @override
  void onInit() {
    super.onInit();
    load();
  }

  @override
  void onClose() {
    amountCtrl.dispose();
    descriptionCtrl.dispose();
    super.onClose();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      types.assignAll(await AppServices.realEstate.getExpenseTypes());
      page.value = await AppServices.realEstate.getExpenses(
        from: from.value,
        to: to.value,
        search: search.value,
        typeSyncId: selectedTypeSyncId.value,
      );
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }

  Future<void> pickFrom() async {
    final ctx = Get.context;
    if (ctx == null) return;
    final d = await showDatePicker(
      context: ctx,
      initialDate: from.value,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (d != null) {
      from.value = d;
      load();
    }
  }

  Future<void> pickTo() async {
    final ctx = Get.context;
    if (ctx == null) return;
    final d = await showDatePicker(
      context: ctx,
      initialDate: to.value,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (d != null) {
      to.value = d;
      load();
    }
  }

  void setSearch(String value) {
    search.value = value;
    load();
  }

  void setTypeFilter(String? syncId) {
    selectedTypeSyncId.value = syncId;
    load();
  }

  Future<void> createExpense() async {
    final type = selectedTypeForForm.value;
    final amount = double.tryParse(amountCtrl.text.trim()) ?? 0;
    if (type == null || amount <= 0) {
      Get.snackbar('error'.tr(), 'real_estate_expense_validation'.tr());
      return;
    }
    try {
      await AppServices.realEstate.createExpense(
        expenseTypeSyncId: type.syncId,
        expenseDate: expenseDate.value,
        amount: amount,
        description: descriptionCtrl.text.trim(),
      );
      Get.back();
      amountCtrl.clear();
      descriptionCtrl.clear();
      await load();
    } catch (e) {
      Get.snackbar('error'.tr(), e.toString());
    }
  }

  Future<void> deleteExpense(RealEstateExpenseItem item) async {
    try {
      await AppServices.realEstate.deleteExpense(item.syncId);
      await load();
    } catch (e) {
      Get.snackbar('error'.tr(), e.toString());
    }
  }
}
