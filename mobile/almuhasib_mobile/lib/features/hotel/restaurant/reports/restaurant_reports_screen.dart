import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../data/restaurant_controller.dart';

class RestaurantReportsScreen extends StatelessWidget {
  const RestaurantReportsScreen({super.key, required this.controller});

  final RestaurantController controller;

  @override
  Widget build(BuildContext context) {
    const accent = Color(0xFF00897B);

    return Obx(() {
      if (controller.isProfitLoading.value) {
        return const Center(child: CircularProgressIndicator());
      }
      final error = controller.profitError.value;
      if (error != null) {
        return Center(child: Text('$error'));
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
            color: accent,
          ),
          const SizedBox(height: 12),
          _StatCard(
            title: 'restaurant_cogs'.tr(),
            value: NumberFormat('#,###').format(summary.cogs),
            color: Colors.orange,
          ),
          const SizedBox(height: 12),
          _StatCard(
            title: 'restaurant_profit'.tr(),
            value: NumberFormat('#,###').format(summary.grossProfit),
            color: Colors.green,
          ),
          const SizedBox(height: 12),
          _StatCard(
            title: 'restaurant_margin'.tr(),
            value: '${summary.marginPercent.toStringAsFixed(1)}%',
            color: Colors.blue,
          ),
          const SizedBox(height: 12),
          _StatCard(
            title: 'restaurant_orders_count'.tr(),
            value: '${summary.orderCount}',
            color: Colors.purple,
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
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Row(
          children: [
            Container(
              width: 4,
              height: 48,
              decoration: BoxDecoration(
                color: color,
                borderRadius: BorderRadius.circular(4),
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(title, style: Theme.of(context).textTheme.bodyMedium),
                  const SizedBox(height: 4),
                  Text(
                    value,
                    style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                          fontWeight: FontWeight.bold,
                        ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
