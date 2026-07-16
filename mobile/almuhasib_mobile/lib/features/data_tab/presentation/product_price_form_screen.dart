import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/product_price_form_controller.dart';

class ProductPriceFormScreen extends GetView<ProductPriceFormController> {
  const ProductPriceFormScreen({super.key, this.syncId});

  final String? syncId;

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => AppFormPage(
        title: controller.isEdit
            ? 'edit_product_price'.tr()
            : 'add_product_price'.tr(),
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
            title: 'product_price_info'.tr(),
            children: [
              OutlinedButton.icon(
                onPressed: controller.isEdit ? null : controller.pickProduct,
                icon: const Icon(Icons.inventory_2_outlined),
                label: Text(
                  controller.product.value?.name ?? 'select_product'.tr(),
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              OutlinedButton.icon(
                onPressed: controller.pickPricingType,
                icon: const Icon(Icons.sell_outlined),
                label: Text(
                  controller.pricingType.value?.name ??
                      'select_pricing_type'.tr(),
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              AppTextField(
                controller: controller.salePriceController,
                label: 'sale_price'.tr(),
                prefixIcon: Icons.trending_up,
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
                validator: (v) =>
                    v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
              ),
              const SizedBox(height: AppSpacing.md),
              AppTextField(
                controller: controller.purchasePriceController,
                label: 'purchase_price'.tr(),
                prefixIcon: Icons.trending_down,
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
                validator: (v) =>
                    v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
              ),
            ],
          ),
        ],
      ),
    );
  }
}
