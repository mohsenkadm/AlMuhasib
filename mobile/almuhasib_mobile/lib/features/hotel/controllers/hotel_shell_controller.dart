import 'package:flutter/scheduler.dart';
import 'package:get/get.dart';

import '../../../core/router/app_routes.dart';

class HotelShellController extends GetxController {
  final currentIndex = 0.obs;

  /// Non-reactive: tracks which tabs have been opened at least once.
  final builtTabs = <int>{0};

  static const _routes = [
    AppRoutes.hotelHome,
    AppRoutes.hotelReservations,
    AppRoutes.hotelRooms,
    AppRoutes.hotelOperations,
    AppRoutes.hotelSettings,
  ];

  /// Called from shell [build]; defer Rx writes so Obx is not rebuilt mid-frame.
  void syncTab(int index) {
    builtTabs.add(index);
    if (currentIndex.value == index) return;
    SchedulerBinding.instance.addPostFrameCallback((_) {
      if (isClosed) return;
      if (currentIndex.value != index) {
        currentIndex.value = index;
        builtTabs.add(index);
      }
    });
  }

  void onTabTap(int index) {
    builtTabs.add(index);
    if (index == currentIndex.value) return;
    currentIndex.value = index;
    Get.offNamed(_routes[index]);
  }

  bool isTabBuilt(int index) => builtTabs.contains(index);
}
