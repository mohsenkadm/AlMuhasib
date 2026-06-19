import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../controllers/customer_form_controller.dart';
import '../../../../shared/widgets/form_section_card.dart';

class CustomerFormScreen extends StatelessWidget {
  const CustomerFormScreen({super.key, this.syncId});

  final String? syncId;

  @override
  Widget build(BuildContext context) {
    final controller = Get.put(CustomerFormController(syncId: syncId));
    return _CustomerFormView(controller: controller);
  }
}

class _CustomerFormView extends StatelessWidget {
  const _CustomerFormView({required this.controller});

  final CustomerFormController controller;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(
          controller.isEdit ? 'edit_customer'.tr() : 'add_customer'.tr(),
        ),
      ),
      body: Form(
        key: controller.formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            FormSectionCard(
              title: 'customer_info'.tr(),
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
                  keyboardType: TextInputType.phone,
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
