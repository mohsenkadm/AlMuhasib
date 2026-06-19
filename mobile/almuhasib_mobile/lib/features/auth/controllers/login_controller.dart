import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../core/network/api_exception.dart';

class LoginController extends GetxController {
  final formKey = GlobalKey<FormState>();
  final usernameController = TextEditingController(text: 'demo');
  final passwordController = TextEditingController(text: 'demo123');

  final isLoading = false.obs;
  final obscurePassword = true.obs;
  final errorMessage = RxnString();

  Future<void> login() async {
    if (!formKey.currentState!.validate()) return;

    isLoading.value = true;
    errorMessage.value = null;

    try {
      await AppServices.auth.login(
        usernameController.text.trim(),
        passwordController.text,
      );
      Get.offAllNamed(AppServices.prefs.launchRoute);
    } on ApiException catch (e) {
      errorMessage.value = mapApiErrorCode(e.code).tr();
    } catch (_) {
      errorMessage.value = 'login_error'.tr();
    } finally {
      isLoading.value = false;
    }
  }

  void togglePasswordVisibility() => obscurePassword.toggle();

  @override
  void onClose() {
    usernameController.dispose();
    passwordController.dispose();
    super.onClose();
  }
}
