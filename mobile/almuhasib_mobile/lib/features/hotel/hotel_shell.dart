import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../core/getx/app_services.dart';
import '../../core/router/app_routes.dart';
import '../../features/hotel/dashboard/hotel_dashboard_screen.dart';
import '../../features/hotel/operations/hotel_operations_hub_screen.dart';
import '../../features/hotel/reservations/hotel_reservations_screen.dart';
import '../../features/hotel/rooms/hotel_rooms_screen.dart';
import '../../features/settings/settings_screen.dart';
import '../../shared/widgets/animated_bottom_nav.dart';
import 'controllers/hotel_shell_controller.dart';

class HotelShellPage extends GetView<HotelShellController> {
  const HotelShellPage({super.key, this.initialTab = 0});

  final int initialTab;

  @override
  Widget build(BuildContext context) {
    controller.syncTab(initialTab);
    final profile = AppServices.prefs.systemProfile;

    return Scaffold(
      extendBody: true,
      body: Obx(() {
        final index = controller.currentIndex.value;
        controller.builtTabs.add(index);
        return IndexedStack(
          index: index,
          children: [
            const HotelDashboardScreen(),
            controller.isTabBuilt(1)
                ? const HotelReservationsScreen()
                : const SizedBox.shrink(),
            controller.isTabBuilt(2)
                ? const HotelRoomsScreen()
                : const SizedBox.shrink(),
            controller.isTabBuilt(3)
                ? const HotelOperationsHubScreen()
                : const SizedBox.shrink(),
            controller.isTabBuilt(4)
                ? const SettingsScreen()
                : const SizedBox.shrink(),
          ],
        );
      }),
      bottomNavigationBar: Obx(
        () => AnimatedBottomNavBar(
          selectedIndex: controller.currentIndex.value,
          onTap: controller.onTabTap,
          onFabTap: () => Get.toNamed(AppRoutes.hotelReservationNew),
          accentColor: profile.accent,
          primaryColor: profile.primary,
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
              icon: Icons.swap_horiz_rounded,
              activeIcon: Icons.swap_horiz_rounded,
              labelKey: 'hotel_nav_operations',
            ),
            BottomNavItem(
              icon: Icons.settings_outlined,
              activeIcon: Icons.settings_rounded,
              labelKey: 'hotel_nav_settings',
            ),
          ],
        ),
      ),
    );
  }
}
