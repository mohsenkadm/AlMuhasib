import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../shared/models/mobile_models.dart';
import '../../../shared/widgets/design_system/design_system.dart';

class CustomerFormController extends GetxController {
  CustomerFormController({this.syncId});

  final String? syncId;

  final formKey = GlobalKey<FormState>();
  final nameController = TextEditingController();
  final phoneController = TextEditingController();
  final addressController = TextEditingController();
  final notesController = TextEditingController();

  final saving = false.obs;

  bool get isEdit => syncId != null;

  Future<void> save() async {
    if (!formKey.currentState!.validate()) return;
    saving.value = true;
    try {
      final response = await AppServices.operations.createCustomer(
        CreateCustomerRequest(
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
      if (response.conflicts.isNotEmpty) {
        AppExceptionHandler.showConflicts(response.conflicts);
        return;
      }
      AppExceptionHandler.showSuccess(response.message);
      Get.back(result: true);
    } catch (e) {
      AppExceptionHandler.showError(e);
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
