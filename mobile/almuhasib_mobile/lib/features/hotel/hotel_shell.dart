import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../core/config/system_profile.dart';
import '../../core/getx/app_services.dart';
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

    return Obx(
      () => Scaffold(
        extendBody: true,
        body: IndexedStack(
          index: controller.currentIndex.value,
          children: const [
            HotelDashboardScreen(),
            HotelReservationsScreen(),
            HotelRoomsScreen(),
            HotelOperationsHubScreen(),
            SettingsScreen(),
          ],
        ),
        bottomNavigationBar: AnimatedBottomNavBar(
          selectedIndex: controller.currentIndex.value,
          onTap: controller.onTabTap,
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
