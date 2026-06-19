import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../features/hotel/check_in_out/hotel_check_in_out_screen.dart';
import '../../features/hotel/dashboard/hotel_dashboard_screen.dart';
import '../../features/hotel/guests/hotel_guests_screen.dart';
import '../../features/hotel/reservations/hotel_reservations_screen.dart';
import '../../features/hotel/restaurant/pos/restaurant_hub_screen.dart';
import '../../features/hotel/rooms/hotel_rooms_screen.dart';
import '../../features/settings/settings_screen.dart';
import '../../shared/widgets/animated_bottom_nav.dart';
import '../../core/router/app_routes.dart';

class HotelShellController extends GetxController {
  HotelShellController({int initialTab = 0}) : currentIndex = initialTab.obs;

  final RxInt currentIndex;

  static final _routes = [
    AppRoutes.hotelHome,
    AppRoutes.hotelReservations,
    AppRoutes.hotelRooms,
    AppRoutes.hotelOperations,
    AppRoutes.hotelGuests,
    AppRoutes.hotelRestaurant,
    AppRoutes.hotelSettings,
  ];

  void onTabTap(int index) {
    if (index == currentIndex.value) return;
    currentIndex.value = index;
    Get.offNamed(_routes[index]);
  }
}

class HotelShellPage extends StatelessWidget {
  const HotelShellPage({super.key, this.initialTab = 0});

  final int initialTab;

  @override
  Widget build(BuildContext context) {
    final tag = 'hotel_shell_$initialTab';
    if (!Get.isRegistered<HotelShellController>(tag: tag)) {
      Get.put(HotelShellController(initialTab: initialTab), tag: tag);
    }
    final controller = Get.find<HotelShellController>(tag: tag);

    return Obx(
      () => Scaffold(
        extendBody: true,
        body: IndexedStack(
          index: controller.currentIndex.value,
          children: const [
            HotelDashboardScreen(),
            HotelReservationsScreen(),
            HotelRoomsScreen(),
            HotelCheckInOutScreen(),
            HotelGuestsScreen(),
            RestaurantHubScreen(),
            SettingsScreen(),
          ],
        ),
        bottomNavigationBar: AnimatedBottomNavBar(
          selectedIndex: controller.currentIndex.value,
          onTap: controller.onTabTap,
          items: const [
            BottomNavItem(
              icon: Icons.hotel_outlined,
              activeIcon: Icons.hotel_rounded,
              labelKey: 'hotel_nav_home',
            ),
            BottomNavItem(
              icon: Icons.event_note_outlined,
              activeIcon: Icons.event_note_rounded,
              labelKey: 'hotel_nav_reservations',
            ),
            BottomNavItem(
              icon: Icons.meeting_room_outlined,
              activeIcon: Icons.meeting_room_rounded,
              labelKey: 'hotel_nav_rooms',
            ),
            BottomNavItem(
              icon: Icons.login_outlined,
              activeIcon: Icons.login_rounded,
              labelKey: 'hotel_nav_operations',
            ),
            BottomNavItem(
              icon: Icons.people_outline_rounded,
              activeIcon: Icons.people_rounded,
              labelKey: 'hotel_nav_guests',
            ),
            BottomNavItem(
              icon: Icons.restaurant_outlined,
              activeIcon: Icons.restaurant_rounded,
              labelKey: 'hotel_nav_restaurant',
            ),
            BottomNavItem(
              icon: Icons.settings_outlined,
              activeIcon: Icons.settings_rounded,
              labelKey: 'hotel_nav_settings',
            ),
          ],
          accentColor: Color(0xFFFFB74D),
          primaryColor: Color(0xFF00897B),
        ),
      ),
    );
  }
}
