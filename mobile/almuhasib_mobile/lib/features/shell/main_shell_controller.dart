import 'package:get/get.dart';

import '../../core/router/app_routes.dart';

class MainShellController extends GetxController {
  final currentIndex = 0.obs;

  static const _routes = [
    AppRoutes.home,
    AppRoutes.reports,
    AppRoutes.data,
    AppRoutes.settings,
  ];

  void syncTab(int index) {
    if (currentIndex.value != index) {
      currentIndex.value = index;
    }
  }

  void onTabTap(int index) {
    if (index == currentIndex.value) return;
    currentIndex.value = index;
    Get.offNamed(_routes[index]);
  }
}
