import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/config/system_profile.dart';
import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/shimmer_widgets.dart';
import '../controllers/car_trade_dashboard_controller.dart';

class CarTradeDashboardScreen extends GetView<CarTradeDashboardController> {
  const CarTradeDashboardScreen({super.key});

  @override
  final String? tag = 'car_trade_dashboard';

  @override
  Widget build(BuildContext context) {
    final profile = SystemProfile.of(AppServices.prefs.systemType);

    return AppPageScaffold(
      useSliver: true,
      title: 'car_trade_dashboard_title'.tr(),
      subtitle: AppServices.prefs.companyName,
      actions: [
        IconButton(
          onPressed: () => Get.toNamed(AppRoutes.profile),
          icon: const Icon(Icons.person_outline_rounded, color: Colors.white),
        ),
      ],
      slivers: [
        SliverToBoxAdapter(
          child: RefreshIndicator(
            onRefresh: controller.load,
            child: Obx(() {
              return AppAsyncBody(
                isLoading: controller.isLoading.value,
                error: controller.error.value,
                data: controller.data.value,
                onRetry: controller.load,
                loadingWidget: const DashboardShimmer(),
                builder: (context, data) {
                  return Padding(
                    padding: const EdgeInsets.all(20),
                    child: AppKpiGrid(
                      items: [
                        AppKpiItem(
                          title: 'car_trade_kpi_today'.tr(),
                          value: '${data.todayTransactions}',
                          icon: Icons.today_rounded,
                          color: profile.accent,
                        ),
                        AppKpiItem(
                          title: 'car_trade_kpi_month'.tr(),
                          value: '${data.monthTransactions}',
                          icon: Icons.calendar_month_rounded,
                          color: profile.secondary,
                        ),
                        AppKpiItem(
                          title: 'car_trade_kpi_unpaid'.tr(),
                          value: '${data.unpaidTransactions}',
                          icon: Icons.warning_amber_rounded,
                          color: AppColors.warning,
                        ),
                        AppKpiItem(
                          title: 'car_trade_kpi_paid'.tr(),
                          value: _formatCompactCurrency(data.totalPaid),
                          icon: Icons.payments_rounded,
                          color: AppColors.success,
                        ),
                      ],
                    ).fadeSlideIn(),
                  );
                },
              );
            }),
          ),
        ),
      ],
    );
  }
}

String _formatCompactCurrency(double v) {
  if (v >= 1000000) return '${(v / 1000000).toStringAsFixed(1)}M';
  if (v >= 1000) return '${(v / 1000).toStringAsFixed(1)}K';
  return v.toStringAsFixed(0);
}
