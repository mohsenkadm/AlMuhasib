import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../core/config/system_profile.dart';
import '../../core/getx/app_services.dart';
import '../../features/dashboard/presentation/dashboard_screen.dart';
import '../../features/data_tab/presentation/data_screen.dart';
import '../../features/reports/presentation/reports_screen.dart';
import '../../features/settings/settings_screen.dart';
import '../../shared/widgets/animated_bottom_nav.dart';
import 'main_shell_controller.dart';

class MainShellPage extends GetView<MainShellController> {
  const MainShellPage({super.key, this.initialTab = 0});

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
            DashboardScreen(),
            ReportsScreen(),
            DataScreen(),
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
              icon: Icons.home_outlined,
              activeIcon: Icons.home_rounded,
              labelKey: 'nav_home',
            ),
            BottomNavItem(
              icon: Icons.bar_chart_outlined,
              activeIcon: Icons.bar_chart_rounded,
              labelKey: 'nav_reports',
            ),
            BottomNavItem(
              icon: Icons.grid_view_rounded,
              activeIcon: Icons.grid_view_rounded,
              labelKey: 'nav_data',
            ),
            BottomNavItem(
              icon: Icons.settings_outlined,
              activeIcon: Icons.settings_rounded,
              labelKey: 'nav_settings',
            ),
          ],
        ),
      ),
    );
  }
}
