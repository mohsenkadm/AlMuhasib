import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../shared/widgets/form_section_card.dart';
import '../controllers/hotel_guest_form_controller.dart';
import '../models/hotel_models.dart';

class HotelGuestFormScreen extends GetView<HotelGuestFormController> {
  const HotelGuestFormScreen({super.key, this.guest})
      : super(tag: 'hotel_guest_form');

  final HotelGuest? guest;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(
          controller.isEdit ? 'hotel_edit_guest'.tr() : 'hotel_add_guest'.tr(),
        ),
      ),
      body: Form(
        key: controller.formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            FormSectionCard(
              title: 'hotel_guest_info'.tr(),
              children: [
                TextFormField(
                  controller: controller.nameController,
                  decoration: InputDecoration(labelText: 'name'.tr()),
                  validator: (v) =>
                      v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: controller.idNumberController,
                  decoration: InputDecoration(
                    labelText: 'hotel_id_number'.tr(),
                  ),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: controller.phoneController,
                  decoration: InputDecoration(labelText: 'phone'.tr()),
                  keyboardType: TextInputType.phone,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: controller.emailController,
                  decoration: InputDecoration(labelText: 'hotel_email'.tr()),
                  keyboardType: TextInputType.emailAddress,
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
