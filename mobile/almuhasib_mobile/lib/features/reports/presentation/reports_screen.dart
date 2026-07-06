import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/config/system_profile.dart';
import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/design_system/design_system.dart';

class ReportsScreen extends StatelessWidget {
  const ReportsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final reports = [
      _ReportItem('sales', 'report_sales', Icons.point_of_sale, AppColors.success),
      _ReportItem('purchases', 'report_purchases', Icons.shopping_bag_outlined, profile.primary),
      _ReportItem('profit', 'report_profit', Icons.trending_up, profile.accent),
      _ReportItem('balance_sheet', 'report_balance_sheet', Icons.account_balance, profile.secondary),
      _ReportItem('overdue', 'report_overdue', Icons.warning_amber_rounded, AppColors.warning),
      _ReportItem('statement', 'report_statement', Icons.receipt_long, profile.primary),
      _ReportItem('investor_statement', 'report_investor_statement', Icons.savings_outlined, profile.accent),
      _ReportItem('warehouse', 'report_warehouse', Icons.warehouse_outlined, profile.secondary),
      _ReportItem('top_products', 'report_top_products', Icons.star_outline, AppColors.warning),
    ];

    final profile = SystemProfile.ofInt(AppServices.prefs.systemType);

    return AppPageScaffold(
      title: 'reports_title'.tr(),
      subtitle: 'reports_subtitle'.tr(),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 120),
        children: [
          Text('reports_subtitle'.tr(), style: Theme.of(context).textTheme.bodyMedium)
              .fadeSlideIn(),
          const SizedBox(height: 16),
          ...reports.asMap().entries.map(
            (entry) => Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: AppEntityCard(
                title: entry.value.titleKey.tr(),
                leading: Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: entry.value.color.withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Icon(entry.value.icon, color: entry.value.color),
                ),
                trailing: const Icon(Icons.chevron_left),
                onTap: () => Get.toNamed(
                  AppRoutes.reportDetailPath(entry.value.type),
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
