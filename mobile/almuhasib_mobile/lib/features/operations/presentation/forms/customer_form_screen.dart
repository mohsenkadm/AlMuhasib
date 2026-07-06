import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../../core/theme/app_spacing.dart';
import '../../controllers/customer_form_controller.dart';
import '../../../../shared/widgets/design_system/design_system.dart';

class CustomerFormScreen extends GetView<CustomerFormController> {
  CustomerFormScreen({super.key, this.syncId});

  final String? syncId;

  @override
  Widget build(BuildContext context) {
    return AppFormPage(
      title: controller.isEdit ? 'edit_customer'.tr() : 'add_customer'.tr(),
      formKey: controller.formKey,
      saveLabel: 'save'.tr(),
      onSave: controller.save,
      isSaving: controller.saving,
      sections: [
        AppFormSection(
          title: 'customer_info'.tr(),
          children: [
            AppTextField(
              controller: controller.nameController,
              label: 'name'.tr(),
              prefixIcon: Icons.person_outline_rounded,
              validator: (v) =>
                  v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.phoneController,
              label: 'phone'.tr(),
              prefixIcon: Icons.phone_outlined,
              keyboardType: TextInputType.phone,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.addressController,
              label: 'address'.tr(),
              prefixIcon: Icons.location_on_outlined,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.notesController,
              label: 'notes'.tr(),
              prefixIcon: Icons.notes_outlined,
              maxLines: 3,
            ),
          ],
        ),
      ],
    );
  }
}
