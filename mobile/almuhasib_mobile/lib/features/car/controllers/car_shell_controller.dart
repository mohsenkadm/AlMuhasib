import 'package:flutter/scheduler.dart';
import 'package:get/get.dart';

import '../../../core/router/app_routes.dart';

class CarShellController extends GetxController {
  final currentIndex = 0.obs;

  static const _routes = [
    AppRoutes.carHome,
    AppRoutes.carContracts,
    AppRoutes.carPayments,
    AppRoutes.carReports,
    AppRoutes.carSettings,
  ];

  /// Called from shell [build]; defer Rx writes so Obx is not rebuilt mid-frame.
  void syncTab(int index) {
    if (currentIndex.value == index) return;
    SchedulerBinding.instance.addPostFrameCallback((_) {
      if (isClosed) return;
      if (currentIndex.value != index) {
        currentIndex.value = index;
      }
    });
  }

  void onTabTap(int index) {
    if (index == currentIndex.value) return;
    currentIndex.value = index;
    Get.offNamed(_routes[index]);
  }
}
