import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../shared/widgets/sticky_summary_bar.dart'
    show showErrorSnackbar, showSuccessSnackbar;
import '../models/hotel_models.dart';

class HotelGuestFormController extends GetxController {
  HotelGuestFormController({this.guest});

  final HotelGuest? guest;

  final formKey = GlobalKey<FormState>();
  late final TextEditingController nameController;
  late final TextEditingController idNumberController;
  late final TextEditingController phoneController;
  late final TextEditingController emailController;
  late final TextEditingController notesController;

  final saving = false.obs;

  bool get isEdit => guest != null;

  @override
  void onInit() {
    super.onInit();
    nameController = TextEditingController(text: guest?.fullName ?? '');
    idNumberController = TextEditingController(text: guest?.idNumber ?? '');
    phoneController = TextEditingController(text: guest?.phone ?? '');
    emailController = TextEditingController(text: guest?.email ?? '');
    notesController = TextEditingController(text: guest?.notes ?? '');
  }

  Future<void> save() async {
    if (!formKey.currentState!.validate()) return;
    saving.value = true;
    try {
      final request = HotelGuestUpsertRequest(
        fullName: nameController.text.trim(),
        idNumber: idNumberController.text.trim(),
        phone: phoneController.text.trim(),
        email: emailController.text.trim(),
        notes: notesController.text.trim(),
      );
      if (guest != null) {
        await AppServices.hotel.updateGuest(guest!.syncId, request);
      } else {
        await AppServices.hotel.createGuest(request);
      }
      final ctx = Get.context;
      if (ctx != null) showSuccessSnackbar(ctx, 'settings_saved'.tr());
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
    idNumberController.dispose();
    phoneController.dispose();
    emailController.dispose();
    notesController.dispose();
    super.onClose();
  }
}
