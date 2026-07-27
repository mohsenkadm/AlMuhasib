import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../shared/models/master_data_models.dart';
import '../../../shared/models/mobile_models.dart';
import '../../../shared/widgets/design_system/design_system.dart';

class PricingTypeFormController extends GetxController {
  PricingTypeFormController({this.syncId});

  final String? syncId;

  final formKey = GlobalKey<FormState>();
  final nameController = TextEditingController();
  final isDefault = false.obs;
  final isActive = true.obs;
  final saving = false.obs;
  final deleting = false.obs;
  final loading = false.obs;

  bool get isEdit => syncId != null && syncId!.isNotEmpty;

  @override
  void onInit() {
    super.onInit();
    if (isEdit) _loadExisting();
  }

  Future<void> _loadExisting() async {
    loading.value = true;
    try {
      final args = Get.arguments;
      if (args is PricingTypeLookupItem) {
        _apply(args);
        return;
      }
      final items = await AppServices.data.getPricingTypes();
      for (final item in items) {
        if (item.syncId == syncId) {
          _apply(item);
          break;
        }
      }
    } catch (e) {
      AppExceptionHandler.showError(e);
    } finally {
      loading.value = false;
    }
  }

  void _apply(PricingTypeLookupItem item) {
    nameController.text = item.name;
    isDefault.value = item.isDefault;
    isActive.value = item.isActive;
  }

  Future<void> save() async {
    if (!(formKey.currentState?.validate() ?? false)) return;
    saving.value = true;
    try {
      final response = await AppServices.operations.upsertPricingType(
        UpsertPricingTypeRequest(
          syncId: syncId,
          name: nameController.text.trim(),
          isDefault: isDefault.value,
          isActive: isActive.value,
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

  Future<void> delete() async {
    if (!isEdit) return;
    final confirmed = await Get.dialog<bool>(
      AlertDialog(
        title: Text('delete'.tr()),
        content: Text('confirm_delete_pricing_type'.tr()),
        actions: [
          TextButton(onPressed: () => Get.back(result: false), child: Text('cancel'.tr())),
          FilledButton(onPressed: () => Get.back(result: true), child: Text('delete'.tr())),
        ],
      ),
    );
    if (confirmed != true) return;
    deleting.value = true;
    try {
      final response = await AppServices.operations.deletePricingType(syncId!);
      if (response.conflicts.isNotEmpty) {
        AppExceptionHandler.showConflicts(response.conflicts);
        return;
      }
      AppExceptionHandler.showSuccess(response.message);
      Get.back(result: true);
    } catch (e) {
      AppExceptionHandler.showError(e);
    } finally {
      deleting.value = false;
    }
  }

  @override
  void onClose() {
    nameController.dispose();
    super.onClose();
  }
}
