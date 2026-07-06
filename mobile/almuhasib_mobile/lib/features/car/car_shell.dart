import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../core/config/system_profile.dart';
import '../../core/getx/app_services.dart';
import '../../features/car/contracts/car_contracts_screen.dart';
import '../../features/car/dashboard/car_dashboard_screen.dart';
import '../../features/car/payments/car_payments_screen.dart';
import '../../features/car/reports/car_report_screen.dart';
import '../../features/settings/settings_screen.dart';
import '../../shared/widgets/animated_bottom_nav.dart';
import 'controllers/car_shell_controller.dart';

class CarShellPage extends GetView<CarShellController> {
  const CarShellPage({super.key, this.initialTab = 0});

  final int initialTab;

  @override
  Widget build(BuildContext context) {
    controller.syncTab(initialTab);
    final profile = SystemProfile.ofInt(AppServices.prefs.systemType);

    return Obx(
      () => Scaffold(
        extendBody: true,
        body: IndexedStack(
          index: controller.currentIndex.value,
          children: const [
            CarDashboardScreen(),
            CarContractsScreen(),
            CarPaymentsScreen(),
            CarReportScreen(),
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
              icon: Icons.dashboard_outlined,
              activeIcon: Icons.dashboard_rounded,
              labelKey: 'car_nav_home',
            ),
            BottomNavItem(
              icon: Icons.description_outlined,
              activeIcon: Icons.description_rounded,
              labelKey: 'car_nav_contracts',
            ),
            BottomNavItem(
              icon: Icons.payments_outlined,
              activeIcon: Icons.payments_rounded,
              labelKey: 'car_nav_payments',
            ),
            BottomNavItem(
              icon: Icons.bar_chart_outlined,
              activeIcon: Icons.bar_chart_rounded,
              labelKey: 'car_nav_reports',
            ),
            BottomNavItem(
              icon: Icons.settings_outlined,
              activeIcon: Icons.settings_rounded,
              labelKey: 'car_nav_settings',
            ),
          ],
        ),
      ),
    );
  }
}
