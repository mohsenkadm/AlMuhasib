import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;
import 'package:intl/intl.dart';

import '../../../../core/constants/app_colors.dart';
import '../../../../core/config/system_profile.dart';
import '../../../../core/getx/app_services.dart';
import '../../../../shared/widgets/common_widgets.dart';
import '../../../../shared/widgets/design_system/design_system.dart';
import '../../../../shared/widgets/shimmer_widgets.dart';
import '../data/restaurant_controller.dart';

class RestaurantReportsScreen extends StatelessWidget {
  const RestaurantReportsScreen({super.key, required this.controller});

  final RestaurantController controller;

  @override
  Widget build(BuildContext context) {
    final profile = AppServices.prefs.systemProfile;

    return Obx(() {
      if (controller.isProfitLoading.value) {
        return const ListShimmer(itemCount: 5);
      }
      final error = controller.profitError.value;
      if (error != null) {
        return ErrorStateWidget(
          message: AppExceptionHandler.messageFor(error),
          onRetry: controller.loadProfit,
        );
      }
      final summary = controller.profit.value;
      if (summary == null) {
        return const SizedBox.shrink();
      }
      return ListView(
        padding: const EdgeInsets.all(16),
        children: [
          _StatCard(
            title: 'restaurant_revenue'.tr(),
            value: NumberFormat('#,###').format(summary.revenue),
            color: profile.primary,
          ),
          const SizedBox(height: 12),
          _StatCard(
            title: 'restaurant_cogs'.tr(),
            value: NumberFormat('#,###').format(summary.cogs),
            color: AppColors.warning,
          ),
          const SizedBox(height: 12),
          _StatCard(
            title: 'restaurant_profit'.tr(),
            value: NumberFormat('#,###').format(summary.grossProfit),
            color: AppColors.success,
          ),
          const SizedBox(height: 12),
          _StatCard(
            title: 'restaurant_margin'.tr(),
            value: '${summary.marginPercent.toStringAsFixed(1)}%',
            color: profile.accent,
          ),
          const SizedBox(height: 12),
          _StatCard(
            title: 'restaurant_orders_count'.tr(),
            value: '${summary.orderCount}',
            color: profile.secondary,
          ),
        ],
      );
    });
  }
}

class _StatCard extends StatelessWidget {
  const _StatCard({
    required this.title,
    required this.value,
    required this.color,
  });

  final String title;
  final String value;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return AppEntityCard(
      title: title,
      trailing: Text(
        value,
        style: Theme.of(context).textTheme.titleLarge?.copyWith(
              fontWeight: FontWeight.w800,
            ),
      ),
      leading: Container(
        width: 4,
        height: 48,
        decoration: BoxDecoration(
          color: color,
          borderRadius: BorderRadius.circular(4),
        ),
      ),
    );
  }
}
