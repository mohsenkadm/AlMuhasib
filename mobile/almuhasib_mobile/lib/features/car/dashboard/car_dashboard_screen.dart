import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/config/system_profile.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/modern_scaffold.dart';
import '../controllers/car_dashboard_controller.dart';

class CarDashboardScreen extends StatelessWidget {
  const CarDashboardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final controller =
        Get.put(CarDashboardController(), tag: 'car_dashboard');
    final profile = SystemProfile.of(AppServices.prefs.systemType);

    return ModernScaffold(
      gradientColors: [profile.primary, profile.secondary],
      appBar: AppBar(
        title: Text('car_dashboard_title'.tr()),
        actions: [
          IconButton(
            onPressed: () => Get.toNamed(AppRoutes.profile),
            icon: const Icon(Icons.person_outline),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: controller.load,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 120),
          children: [
            Obx(
              () => AppServices.connectivity.isOffline.value
                  ? const ConnectivityBanner(isOffline: true)
                  : const SizedBox.shrink(),
            ),
            Obx(() {
              if (controller.isLoading.value) {
                return const Center(
                  child: Padding(
                    padding: EdgeInsets.all(48),
                    child: CircularProgressIndicator(),
                  ),
                );
              }
              if (controller.error.value != null) {
                return ErrorStateWidget(
                  message: 'error_load'.tr(),
                  onRetry: controller.load,
                );
              }
              final data = controller.data.value;
              if (data == null) return const SizedBox.shrink();
              return Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    AppServices.prefs.companyName ?? '',
                    style: Theme.of(context).textTheme.titleLarge,
                  ).fadeSlideIn(),
                  const SizedBox(height: 16),
                  GridView.count(
                    crossAxisCount: 2,
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    mainAxisSpacing: 12,
                    crossAxisSpacing: 12,
                    childAspectRatio: 1.35,
                    children: [
                      KpiCard(
                        title: 'car_kpi_today'.tr(),
                        value: '${data.todayContracts}',
                        icon: Icons.today_rounded,
                        color: profile.accent,
                      ).fadeSlideInList(index: 0),
                      KpiCard(
                        title: 'car_kpi_month'.tr(),
                        value: '${data.monthContracts}',
                        icon: Icons.calendar_month_rounded,
                        color: profile.secondary,
                      ).fadeSlideInList(index: 1),
                      KpiCard(
                        title: 'car_kpi_unpaid'.tr(),
                        value: '${data.unpaidContracts}',
                        icon: Icons.warning_amber_rounded,
                        color: Colors.orange,
                      ).fadeSlideInList(index: 2),
                      KpiCard(
                        title: 'car_kpi_received'.tr(),
                        value: formatCompactCurrency(data.totalReceived),
                        icon: Icons.payments_rounded,
                        color: Colors.green,
                      ).fadeSlideInList(index: 3),
                    ],
                  ),
                ],
              );
            }),
          ],
        ),
      ),
    );
  }
}

String formatCompactCurrency(double v) {
  if (v >= 1000000) return '${(v / 1000000).toStringAsFixed(1)}M';
  if (v >= 1000) return '${(v / 1000).toStringAsFixed(1)}K';
  return v.toStringAsFixed(0);
}
