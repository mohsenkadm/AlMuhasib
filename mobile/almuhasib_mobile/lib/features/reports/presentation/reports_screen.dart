import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../core/constants/app_colors.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';

class ReportsScreen extends StatelessWidget {
  const ReportsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final reports = [
      _ReportItem('sales', 'report_sales', Icons.point_of_sale, AppColors.success),
      _ReportItem('purchases', 'report_purchases', Icons.shopping_bag_outlined, AppColors.primaryLight),
      _ReportItem('profit', 'report_profit', Icons.trending_up, AppColors.accent),
      _ReportItem('overdue', 'report_overdue', Icons.warning_amber_rounded, AppColors.warning),
      _ReportItem('statement', 'report_statement', Icons.receipt_long, AppColors.primary),
      _ReportItem('investor_statement', 'report_investor_statement', Icons.savings_outlined, const Color(0xFF00897B)),
      _ReportItem('warehouse', 'report_warehouse', Icons.warehouse_outlined, const Color(0xFF7E57C2)),
      _ReportItem('top_products', 'report_top_products', Icons.star_outline, const Color(0xFFFF7043)),
    ];

    return Scaffold(
      appBar: AppBar(title: Text('reports_title'.tr())),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 120),
        children: [
          Text('reports_subtitle'.tr(), style: Theme.of(context).textTheme.bodyMedium)
              .fadeSlideIn(),
          const SizedBox(height: 16),
          ...reports.asMap().entries.map(
            (entry) => Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: GradientCard(
                child: ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: entry.value.color.withValues(alpha: 0.15),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Icon(entry.value.icon, color: entry.value.color),
                  ),
                  title: Text(entry.value.titleKey.tr()),
                  trailing: const Icon(Icons.chevron_left),
                  onTap: () => context.push('/reports/detail/${entry.value.type}'),
                ),
              ).fadeSlideInList(index: entry.key),
            ),
          ),
        ],
      ),
    );
  }
}

class _ReportItem {
  const _ReportItem(this.type, this.titleKey, this.icon, this.color);

  final String type;
  final String titleKey;
  final IconData icon;
  final Color color;
}
