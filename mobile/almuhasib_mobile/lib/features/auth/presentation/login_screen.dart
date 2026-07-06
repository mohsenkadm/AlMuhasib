import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/config/system_profile.dart';
import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/login_controller.dart';

class LoginScreen extends GetView<LoginController> {
  const LoginScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final profile = SystemProfile.ofInt(AppServices.prefs.systemType);

    return Scaffold(
      body: Container(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [
              profile.primary.withValues(alpha: 0.14),
              Theme.of(context).scaffoldBackgroundColor,
            ],
          ),
        ),
        child: SafeArea(
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
                        AppTextField(
                          controller: controller.usernameController,
                          label: 'username'.tr(),
                          prefixIcon: Icons.person_outline_rounded,
                          validator: (v) =>
                              v == null || v.isEmpty ? 'required'.tr() : null,
                        ),
                        const SizedBox(height: 16),
                        Obx(
                          () => AppTextField(
                            controller: controller.passwordController,
                            label: 'password'.tr(),
                            prefixIcon: Icons.lock_outline_rounded,
                            obscureText: controller.obscurePassword.value,
                            suffixIcon: IconButton(
                              icon: Icon(
                                controller.obscurePassword.value
                                    ? Icons.visibility_outlined
                                    : Icons.visibility_off_outlined,
                              ),
                              onPressed: controller.togglePasswordVisibility,
                            ),
                            validator: (v) => v == null || v.isEmpty
                                ? 'required'.tr()
                                : null,
                          ),
                        ),
                        Obx(() {
                          final errorMessage = controller.errorMessage.value;
                          if (errorMessage == null) {
                            return const SizedBox.shrink();
                          }
                          return Column(
                            children: [
                              const SizedBox(height: 16),
                              Container(
                                width: double.infinity,
                                padding: const EdgeInsets.all(12),
                                decoration: BoxDecoration(
                                  color:
                                      AppColors.error.withValues(alpha: 0.12),
                                  borderRadius: BorderRadius.circular(12),
                                ),
                                child: Text(
                                  errorMessage,
                                  style:
                                      const TextStyle(color: AppColors.error),
                                ),
                              ),
                            ],
                          );
                        }),
                        const SizedBox(height: 24),
                        Obx(
                          () => AppProgressButton(
                            label: 'login_button'.tr(),
                            isLoading: controller.isLoading.value,
                            onPressed: controller.login,
                            icon: Icons.login_rounded,
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
      ),
    );
  }
}
