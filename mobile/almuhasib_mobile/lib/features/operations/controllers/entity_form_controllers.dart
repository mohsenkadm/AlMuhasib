import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../shared/models/master_data_models.dart';
import '../../../shared/models/mobile_models.dart';
import '../../../shared/widgets/lookup_picker_sheet.dart';
import '../../../shared/widgets/sticky_summary_bar.dart';

class SupplierFormController extends GetxController {
  SupplierFormController({this.syncId});

  final String? syncId;

  final formKey = GlobalKey<FormState>();
  final nameController = TextEditingController();
  final phoneController = TextEditingController();
  final addressController = TextEditingController();
  final notesController = TextEditingController();

  final saving = false.obs;

  Future<void> save() async {
    if (!formKey.currentState!.validate()) return;
    saving.value = true;
    try {
      final response = await AppServices.operations.createSupplier(
        CreateSupplierRequest(
          syncId: syncId,
          name: nameController.text.trim(),
          phone: phoneController.text.trim().isEmpty
              ? null
              : phoneController.text.trim(),
          address: addressController.text.trim().isEmpty
              ? null
              : addressController.text.trim(),
          notes: notesController.text.trim().isEmpty
              ? null
              : notesController.text.trim(),
        ),
      );
      final ctx = Get.context;
      if (ctx == null) return;
      if (response.conflicts.isNotEmpty) {
        showErrorSnackbar(ctx, response.message);
        return;
      }
      showSuccessSnackbar(ctx, response.message);
      Get.back(result: true);
    } catch (e) {
      final ctx = Get.context;
      if (ctx != null) showErrorSnackbar(ctx, e.toString());
    } finally {
      saving.value = false;
    }
  }

  @override
  void onClose() {
    nameController.dispose();
    phoneController.dispose();
    addressController.dispose();
    notesController.dispose();
    super.onClose();
  }
}

class InvestorFormController extends GetxController {
  InvestorFormController({this.syncId});

  final String? syncId;

  final formKey = GlobalKey<FormState>();
  final nameController = TextEditingController();
  final phoneController = TextEditingController();
  final profitPctController = TextEditingController(text: '0');
  final openingBalanceController = TextEditingController(text: '0');

  final saving = false.obs;

  Future<void> save() async {
    if (!formKey.currentState!.validate()) return;
    saving.value = true;
    try {
      final response = await AppServices.operations.createInvestor(
        CreateInvestorRequest(
          syncId: syncId,
          name: nameController.text.trim(),
          phone: phoneController.text.trim().isEmpty
              ? null
              : phoneController.text.trim(),
          profitPercentage: double.tryParse(profitPctController.text) ?? 0,
          openingBalance: double.tryParse(openingBalanceController.text) ?? 0,
        ),
      );
      final ctx = Get.context;
      if (ctx != null) {
        showSuccessSnackbar(ctx, response.message);
        Get.back(result: true);
      }
    } catch (e) {
      final ctx = Get.context;
      if (ctx != null) showErrorSnackbar(ctx, e.toString());
    } finally {
      saving.value = false;
    }
  }

  @override
  void onClose() {
    nameController.dispose();
    phoneController.dispose();
    profitPctController.dispose();
    openingBalanceController.dispose();
    super.onClose();
  }
}

class ProductFormController extends GetxController {
  ProductFormController({this.syncId});

  final String? syncId;

  final formKey = GlobalKey<FormState>();
  final nameController = TextEditingController();
  final barcodeController = TextEditingController();
  final descriptionController = TextEditingController();

  final category = Rxn<LookupItem>();
  final saving = false.obs;

  Future<void> pickCategory() async {
    final ctx = Get.context;
    if (ctx == null) return;
    final selected = await showLookupPickerSheet<LookupItem>(
      context: ctx,
      title: 'select_category'.tr(),
      loadItems: (search) => AppServices.data.getCategories(search: search),
    );
    if (selected != null) category.value = selected;
  }

  Future<void> save() async {
    if (!formKey.currentState!.validate()) return;
    final ctx = Get.context;
    if (ctx == null) return;
    if (category.value == null) {
      showErrorSnackbar(ctx, 'select_category'.tr());
      return;
    }
    saving.value = true;
    try {
      final response = await AppServices.operations.createProduct(
        CreateProductRequest(
          syncId: syncId,
          name: nameController.text.trim(),
          categorySyncId: category.value!.syncId,
          barcode: barcodeController.text.trim().isEmpty
              ? null
              : barcodeController.text.trim(),
          description: descriptionController.text.trim().isEmpty
              ? null
              : descriptionController.text.trim(),
        ),
      );
      showSuccessSnackbar(ctx, response.message);
      Get.back(result: true);
    } catch (e) {
      showErrorSnackbar(ctx, e.toString());
    } finally {
      saving.value = false;
    }
  }

  @override
  void onClose() {
    nameController.dispose();
    barcodeController.dispose();
    descriptionController.dispose();
    super.onClose();
  }
}
