import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;
import 'package:intl/intl.dart';

import '../../../../core/constants/app_colors.dart';
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
    final fmt = NumberFormat('#,###');

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

      final overview = controller.overview.value;
      final channels = controller.channels.value;
      final topItems = controller.topItems.value;

      return ListView(
        padding: const EdgeInsets.all(16),
        children: [
          _StatCard(
            title: 'restaurant_revenue'.tr(),
            value: fmt.format(summary.revenue),
            color: profile.primary,
          ),
          const SizedBox(height: 12),
          _StatCard(
            title: 'restaurant_cogs'.tr(),
            value: fmt.format(summary.cogs),
            color: AppColors.warning,
          ),
          const SizedBox(height: 12),
          _StatCard(
            title: 'restaurant_profit'.tr(),
            value: fmt.format(summary.grossProfit),
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
          if (summary.roomServiceRevenue > 0) ...[
            const SizedBox(height: 12),
            _StatCard(
              title: 'restaurant_room_revenue'.tr(),
              value: fmt.format(summary.roomServiceRevenue),
              color: Colors.deepPurple,
            ),
          ],
          if (overview != null) ...[
            const SizedBox(height: 20),
            Text(
              'restaurant_financial_overview'.tr(),
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
            const SizedBox(height: 8),
            _StatCard(
              title: 'restaurant_kitchen_purchases'.tr(),
              value: fmt.format(overview.kitchenPurchases),
              color: Colors.blueGrey,
            ),
            const SizedBox(height: 12),
            _StatCard(
              title: 'restaurant_net_operating'.tr(),
              value: fmt.format(overview.netOperating),
              color: AppColors.success,
            ),
          ],
          if (channels.isNotEmpty) ...[
            const SizedBox(height: 20),
            Text(
              'restaurant_channels'.tr(),
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
            const SizedBox(height: 8),
            ...channels.map(
              (c) => Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: TweenAnimationBuilder<double>(
                  tween: Tween(begin: 0.92, end: 1),
                  duration: const Duration(milliseconds: 280),
                  builder: (context, scale, child) =>
                      Transform.scale(scale: scale, child: child),
                  child: AppEntityCard(
                    title: c.label,
                    subtitle: '${c.orderCount} ${'restaurant_orders_count'.tr()}',
                    trailing: Text(
                      fmt.format(c.revenue),
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                  ),
                ),
              ),
            ),
          ],
          if (topItems.isNotEmpty) ...[
            const SizedBox(height: 12),
            Text(
              'restaurant_top_items'.tr(),
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
            const SizedBox(height: 8),
            ...topItems.map(
              (t) => Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: AppEntityCard(
                  title: t.itemName,
                  subtitle:
                      '${'restaurant_qty'.tr()}: ${t.quantitySold.toStringAsFixed(0)}',
                  trailing: Text(
                    fmt.format(t.revenue),
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                ),
              ),
            ),
          ],
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
    return TweenAnimationBuilder<double>(
      tween: Tween(begin: 0.0, end: 1.0),
      duration: const Duration(milliseconds: 350),
      curve: Curves.easeOutCubic,
      builder: (context, opacity, child) => Opacity(opacity: opacity, child: child),
      child: AppEntityCard(
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
      ),
    );
  }
}
