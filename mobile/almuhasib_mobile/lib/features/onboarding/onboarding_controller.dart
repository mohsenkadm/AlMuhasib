import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../core/getx/app_services.dart';
import '../../core/router/app_routes.dart';

class OnboardingController extends GetxController {
  final pageController = PageController();
  final currentPage = 0.obs;

  static const slideCount = 3;

  Future<void> complete() async {
    await AppServices.prefs.setOnboardingCompleted(true);
    Get.offAllNamed(AppRoutes.login);
  }

  void next() {
    if (currentPage.value < slideCount - 1) {
      pageController.nextPage(
        duration: const Duration(milliseconds: 400),
        curve: Curves.easeOutCubic,
      );
    } else {
      complete();
    }
  }

  void onPageChanged(int index) => currentPage.value = index;

  @override
  void onClose() {
    pageController.dispose();
    super.onClose();
  }
}
