import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/pricing_type_form_controller.dart';

class PricingTypeFormScreen extends GetView<PricingTypeFormController> {
  const PricingTypeFormScreen({super.key, this.syncId});

  final String? syncId;

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => AppFormPage(
        title: controller.isEdit
            ? 'edit_pricing_type'.tr()
            : 'add_pricing_type'.tr(),
        formKey: controller.formKey,
        saveLabel: 'save'.tr(),
        onSave: controller.save,
        isSaving: controller.saving,
        extraActions: [
          if (controller.isEdit)
            IconButton(
              tooltip: 'delete'.tr(),
              onPressed: controller.deleting.value ? null : controller.delete,
              icon: controller.deleting.value
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.delete_outline),
            ),
        ],
        sections: [
          AppFormSection(
            title: 'pricing_type_info'.tr(),
            children: [
              AppTextField(
                controller: controller.nameController,
                label: 'name'.tr(),
                prefixIcon: Icons.label_outline,
                validator: (v) =>
                    v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
              ),
              const SizedBox(height: AppSpacing.md),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                title: Text('pricing_type_default'.tr()),
                value: controller.isDefault.value,
                onChanged: (v) => controller.isDefault.value = v,
              ),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                title: Text('pricing_type_active'.tr()),
                value: controller.isActive.value,
                onChanged: (v) => controller.isActive.value = v,
              ),
            ],
          ),
        ],
      ),
    );
  }
}
