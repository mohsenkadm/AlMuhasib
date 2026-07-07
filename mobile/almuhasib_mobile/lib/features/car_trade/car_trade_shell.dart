import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../core/getx/app_services.dart';
import '../../features/car_trade/dashboard/car_trade_dashboard_screen.dart';
import '../../features/car_trade/payments/car_trade_payments_screen.dart';
import '../../features/car_trade/reports/car_trade_report_screen.dart';
import '../../features/car_trade/transactions/car_trade_transactions_screen.dart';
import '../../features/settings/settings_screen.dart';
import '../../shared/widgets/animated_bottom_nav.dart';
import 'controllers/car_trade_shell_controller.dart';

class CarTradeShellPage extends GetView<CarTradeShellController> {
  const CarTradeShellPage({super.key, this.initialTab = 0});

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
            CarTradeDashboardScreen(),
            CarTradeTransactionsScreen(),
            CarTradePaymentsScreen(),
            CarTradeReportScreen(),
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
              labelKey: 'car_trade_nav_home',
            ),
            BottomNavItem(
              icon: Icons.swap_horiz_outlined,
              activeIcon: Icons.swap_horiz_rounded,
              labelKey: 'car_trade_nav_transactions',
            ),
            BottomNavItem(
              icon: Icons.payments_outlined,
              activeIcon: Icons.payments_rounded,
              labelKey: 'car_trade_nav_payments',
            ),
            BottomNavItem(
              icon: Icons.bar_chart_outlined,
              activeIcon: Icons.bar_chart_rounded,
              labelKey: 'car_trade_nav_reports',
            ),
            BottomNavItem(
              icon: Icons.settings_outlined,
              activeIcon: Icons.settings_rounded,
              labelKey: 'car_trade_nav_settings',
            ),
          ],
        ),
      ),
    );
  }
}
