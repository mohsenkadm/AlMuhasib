import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../core/getx/app_services.dart';

class SettingsController extends GetxController {
  late final TextEditingController apiUrlController;
  final licenseText = RxnString();

  @override
  void onInit() {
    super.onInit();
    apiUrlController = TextEditingController(
      text: AppServices.prefs.apiBaseUrl,
    );
    loadLicense();
  }

  Future<void> loadLicense() async {
    try {
      final status = await AppServices.authRepository.getLicenseStatus();
      licenseText.value = status.isActive && status.isMobileEnabled
          ? 'license_active'.tr()
          : status.message ?? status.statusCode ?? 'license_inactive'.tr();
    } catch (_) {
      licenseText.value = '—';
    }
  }

  Future<void> saveApiUrl() async {
    await AppServices.prefs.setApiBaseUrl(apiUrlController.text.trim());
    AppServices.api.updateBaseUrl();
    Get.snackbar('', 'settings_saved'.tr());
  }

  @override
  void onClose() {
    apiUrlController.dispose();
    super.onClose();
  }
}
