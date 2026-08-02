import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../core/getx/app_services.dart';
import '../../core/router/app_routes.dart';
import '../../shared/widgets/animated_bottom_nav.dart';
import 'controllers/gold_shell_controller.dart';
import 'customers/gold_customers_screen.dart';
import 'dashboard/gold_dashboard_screen.dart';
import 'sales/gold_sales_screen.dart';
import 'settings/gold_settings_screen.dart';

class GoldShopShellPage extends GetView<GoldShellController> {
  const GoldShopShellPage({super.key, this.initialTab = 0});

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
            GoldDashboardScreen(),
            GoldSalesScreen(),
            GoldCustomersScreen(),
            GoldSettingsScreen(),
          ],
        ),
        bottomNavigationBar: AnimatedBottomNavBar(
          selectedIndex: controller.currentIndex.value,
          onTap: controller.onTabTap,
          onFabTap: () => Get.toNamed(AppRoutes.goldShopSaleNew),
          accentColor: profile.accent,
          primaryColor: profile.primary,
          items: const [
            BottomNavItem(
              icon: Icons.home_outlined,
              activeIcon: Icons.home_rounded,
              labelKey: 'gold_nav_home',
            ),
            BottomNavItem(
              icon: Icons.point_of_sale_outlined,
              activeIcon: Icons.point_of_sale_rounded,
              labelKey: 'gold_nav_sales',
            ),
            BottomNavItem(
              icon: Icons.people_outline_rounded,
              activeIcon: Icons.people_rounded,
              labelKey: 'gold_nav_customers',
            ),
            BottomNavItem(
              icon: Icons.more_horiz_rounded,
              activeIcon: Icons.more_horiz_rounded,
              labelKey: 'gold_nav_more',
            ),
          ],
        ),
      ),
    );
  }
}
