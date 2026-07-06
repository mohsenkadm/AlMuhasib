import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../core/config/system_profile.dart';
import '../../core/getx/app_services.dart';
import '../../core/router/app_routes.dart';
import '../../features/dashboard/presentation/dashboard_screen.dart';
import '../../features/data_tab/presentation/data_screen.dart';
import '../../features/reports/presentation/reports_screen.dart';
import '../../features/settings/settings_screen.dart';
import '../../shared/widgets/animated_bottom_nav.dart';

class MainShellController extends GetxController {
  MainShellController({int initialTab = 0}) : currentIndex = initialTab.obs;

  final RxInt currentIndex;

  static final _routes = [
    AppRoutes.home,
    AppRoutes.reports,
    AppRoutes.data,
    AppRoutes.settings,
  ];

  void onTabTap(int index) {
    if (index == currentIndex.value) return;
    currentIndex.value = index;
    Get.offNamed(_routes[index], id: null);
  }
}

class MainShellPage extends StatelessWidget {
  const MainShellPage({super.key, this.initialTab = 0});

  final int initialTab;

  @override
  Widget build(BuildContext context) {
    final tag = 'main_shell_$initialTab';
    if (!Get.isRegistered<MainShellController>(tag: tag)) {
      Get.put(MainShellController(initialTab: initialTab), tag: tag);
    }
    final controller = Get.find<MainShellController>(tag: tag);
    final profile = SystemProfile.ofInt(AppServices.prefs.systemType);

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
