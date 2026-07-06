import 'package:get/get.dart';

import '../../../core/router/app_routes.dart';

class HotelShellController extends GetxController {
  final currentIndex = 0.obs;

  static const _routes = [
    AppRoutes.hotelHome,
    AppRoutes.hotelReservations,
    AppRoutes.hotelRooms,
    AppRoutes.hotelOperations,
    AppRoutes.hotelSettings,
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
