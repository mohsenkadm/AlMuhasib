import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../controllers/entity_form_controllers.dart';
import '../../../../shared/widgets/form_section_card.dart';

class SupplierFormScreen extends GetView<SupplierFormController> {
  const SupplierFormScreen({super.key, this.syncId});

  final String? syncId;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('add_supplier'.tr())),
      body: Form(
        key: controller.formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            FormSectionCard(
              title: 'supplier_info'.tr(),
              children: [
                TextFormField(
                  controller: controller.nameController,
                  decoration: InputDecoration(labelText: 'name'.tr()),
                  validator: (v) =>
                      v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: controller.phoneController,
                  decoration: InputDecoration(labelText: 'phone'.tr()),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: controller.addressController,
                  decoration: InputDecoration(labelText: 'address'.tr()),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: controller.notesController,
                  decoration: InputDecoration(labelText: 'notes'.tr()),
                  maxLines: 3,
                ),
              ],
            ),
            Obx(
              () => FilledButton(
                onPressed: controller.saving.value ? null : controller.save,
                child: controller.saving.value
                    ? const SizedBox(
                        height: 20,
                        width: 20,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : Text('save'.tr()),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class InvestorFormScreen extends GetView<InvestorFormController> {
  const InvestorFormScreen({super.key, this.syncId});

  final String? syncId;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('add_investor'.tr())),
      body: Form(
        key: controller.formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            FormSectionCard(
              title: 'investor_info'.tr(),
              children: [
                TextFormField(
                  controller: controller.nameController,
                  decoration: InputDecoration(labelText: 'name'.tr()),
                  validator: (v) =>
                      v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: controller.phoneController,
                  decoration: InputDecoration(labelText: 'phone'.tr()),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: controller.profitPctController,
                  decoration: InputDecoration(labelText: 'profit_percentage'.tr()),
                  keyboardType: TextInputType.number,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: controller.openingBalanceController,
                  decoration: InputDecoration(labelText: 'opening_balance'.tr()),
                  keyboardType: TextInputType.number,
                ),
              ],
            ),
            Obx(
              () => FilledButton(
                onPressed: controller.saving.value ? null : controller.save,
                child: controller.saving.value
                    ? const SizedBox(
                        height: 20,
                        width: 20,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : Text('save'.tr()),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class ProductFormScreen extends GetView<ProductFormController> {
  const ProductFormScreen({super.key, this.syncId});

  final String? syncId;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(
          controller.isEdit ? 'edit_product'.tr() : 'add_product'.tr(),
        ),
      ),
      body: Form(
        key: controller.formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            FormSectionCard(
              title: 'product_info'.tr(),
              children: [
                TextFormField(
                  controller: controller.nameController,
                  decoration: InputDecoration(labelText: 'name'.tr()),
                  validator: (v) =>
                      v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
                ),
                const SizedBox(height: 12),
                Obx(
                  () => OutlinedButton.icon(
                    onPressed: controller.pickCategory,
                    icon: const Icon(Icons.category_outlined),
                    label: Text(
                      controller.category.value?.name ?? 'select_category'.tr(),
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: controller.barcodeController,
                  decoration: InputDecoration(labelText: 'barcode'.tr()),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: controller.descriptionController,
                  decoration: InputDecoration(labelText: 'description'.tr()),
                  maxLines: 3,
                ),
              ],
            ),
            if (controller.isEdit)
              Obx(
                () => FormSectionCard(
                  title: 'product_prices'.tr(),
                  children: [
                    if (controller.prices.isEmpty)
                      Text('no_product_prices'.tr())
                    else
                      ...controller.prices.map(
                        (price) => ListTile(
                          contentPadding: EdgeInsets.zero,
                          title: Text(price.pricingTypeName),
                          subtitle: Text(
                            '${'sale_price'.tr()}: ${price.salePrice} • ${'purchase_price'.tr()}: ${price.purchasePrice}',
                          ),
                          trailing: IconButton(
                            icon: const Icon(Icons.edit_outlined),
                            onPressed: () => controller.editPrice(price),
                          ),
                        ),
                      ),
                    const SizedBox(height: 8),
                    OutlinedButton.icon(
                      onPressed: controller.addPrice,
                      icon: const Icon(Icons.add),
                      label: Text('add_product_price'.tr()),
                    ),
                  ],
                ),
              ),
            Obx(
              () => FilledButton(
                onPressed: controller.saving.value ? null : controller.save,
                child: controller.saving.value
                    ? const SizedBox(
                        height: 20,
                        width: 20,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : Text('save'.tr()),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
