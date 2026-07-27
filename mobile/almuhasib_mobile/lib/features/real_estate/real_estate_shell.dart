import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../core/getx/app_services.dart';
import '../../core/router/app_routes.dart';
import '../../features/real_estate/contracts/real_estate_contracts_screen.dart';
import '../../features/real_estate/dashboard/real_estate_dashboard_screen.dart';
import '../../features/real_estate/payments/real_estate_payments_screen.dart';
import '../../features/real_estate/reports/real_estate_report_screen.dart';
import '../../features/settings/settings_screen.dart';
import '../../shared/widgets/animated_bottom_nav.dart';
import 'controllers/real_estate_shell_controller.dart';

class RealEstateShellPage extends GetView<RealEstateShellController> {
  const RealEstateShellPage({super.key, this.initialTab = 0});

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
            RealEstateDashboardScreen(),
            RealEstateContractsScreen(),
            RealEstatePaymentsScreen(),
            RealEstateReportScreen(),
            SettingsScreen(),
          ],
        ),
        bottomNavigationBar: AnimatedBottomNavBar(
          selectedIndex: controller.currentIndex.value,
          onTap: controller.onTabTap,
          onFabTap: () => Get.toNamed(AppRoutes.realEstateContractNew),
          accentColor: profile.accent,
          primaryColor: profile.primary,
          items: const [
            BottomNavItem(
              icon: Icons.dashboard_outlined,
              activeIcon: Icons.dashboard_rounded,
              labelKey: 'real_estate_nav_home',
            ),
            BottomNavItem(
              icon: Icons.home_work_outlined,
              activeIcon: Icons.home_work_rounded,
              labelKey: 'real_estate_nav_contracts',
            ),
            BottomNavItem(
              icon: Icons.payments_outlined,
              activeIcon: Icons.payments_rounded,
              labelKey: 'real_estate_nav_payments',
            ),
            BottomNavItem(
              icon: Icons.bar_chart_outlined,
              activeIcon: Icons.bar_chart_rounded,
              labelKey: 'real_estate_nav_reports',
            ),
            BottomNavItem(
              icon: Icons.settings_outlined,
              activeIcon: Icons.settings_rounded,
              labelKey: 'real_estate_nav_settings',
            ),
          ],
        ),
      ),
    );
  }
}
