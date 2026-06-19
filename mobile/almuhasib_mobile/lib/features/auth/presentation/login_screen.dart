import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../controllers/login_controller.dart';

class LoginScreen extends StatelessWidget {
  const LoginScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final controller = Get.put(LoginController(), tag: 'login');

    return Scaffold(
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: controller.formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SizedBox(height: 32),
                const Center(child: AppLogoMark()).scaleIn(),
                const SizedBox(height: 24),
                Text(
                  'login_title'.tr(),
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.displaySmall,
                ).fadeSlideIn(delayMs: 100),
                const SizedBox(height: 8),
                Text(
                  'login_subtitle'.tr(),
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.bodyMedium,
                ).fadeSlideIn(delayMs: 160),
                const SizedBox(height: 32),
                GradientCard(
                  child: Column(
                    children: [
                      TextFormField(
                        controller: controller.usernameController,
                        decoration: InputDecoration(
                          labelText: 'username'.tr(),
                          prefixIcon: const Icon(Icons.person_outline),
                        ),
                        validator: (v) =>
                            v == null || v.isEmpty ? 'username'.tr() : null,
                      ),
                      const SizedBox(height: 16),
                      Obx(
                        () => TextFormField(
                          controller: controller.passwordController,
                          obscureText: controller.obscurePassword.value,
                          decoration: InputDecoration(
                            labelText: 'password'.tr(),
                            prefixIcon: const Icon(Icons.lock_outline),
                            suffixIcon: IconButton(
                              icon: Icon(
                                controller.obscurePassword.value
                                    ? Icons.visibility_outlined
                                    : Icons.visibility_off_outlined,
                              ),
                              onPressed: controller.togglePasswordVisibility,
                            ),
                          ),
                          validator: (v) => v == null || v.isEmpty
                              ? 'password'.tr()
                              : null,
                        ),
                      ),
                      Obx(() {
                        final errorMessage = controller.errorMessage.value;
                        if (errorMessage == null) return const SizedBox.shrink();
                        return Column(
                          children: [
                            const SizedBox(height: 16),
                            Container(
                              width: double.infinity,
                              padding: const EdgeInsets.all(12),
                              decoration: BoxDecoration(
                                color: AppColors.error.withValues(alpha: 0.12),
                                borderRadius: BorderRadius.circular(12),
                              ),
                              child: Text(
                                errorMessage,
                                style: const TextStyle(color: AppColors.error),
                              ),
                            ),
                          ],
                        );
                      }),
                      const SizedBox(height: 24),
                      Obx(
                        () => SizedBox(
                          width: double.infinity,
                          child: FilledButton(
                            onPressed: controller.isLoading.value
                                ? null
                                : controller.login,
                            style: FilledButton.styleFrom(
                              padding: const EdgeInsets.symmetric(vertical: 16),
                            ),
                            child: controller.isLoading.value
                                ? const SizedBox(
                                    width: 22,
                                    height: 22,
                                    child: CircularProgressIndicator(
                                      strokeWidth: 2,
                                      color: Colors.white,
                                    ),
                                  )
                                : Text('login_button'.tr()),
                          ),
                        ),
                      ),
                    ],
                  ),
                ).fadeSlideIn(delayMs: 220),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
