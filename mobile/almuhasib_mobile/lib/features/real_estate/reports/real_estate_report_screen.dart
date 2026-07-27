import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../controllers/real_estate_report_controller.dart';
import '../models/real_estate_models.dart';
import '../widgets/real_estate_labels.dart';

class RealEstateReportScreen extends GetView<RealEstateReportController> {
  const RealEstateReportScreen({super.key});

  @override
  final String? tag = 'real_estate_report';

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: 'real_estate_report_title'.tr(),
      actions: [
        IconButton(
          onPressed: () => Get.toNamed(AppRoutes.realEstateExpenses),
          icon: const Icon(Icons.money_off_csred_outlined),
          tooltip: 'real_estate_expenses_title'.tr(),
        ),
      ],
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 0),
            child: Obx(
              () => SegmentedButton<String>(
                segments: [
                  ButtonSegment(
                    value: 'contracts',
                    label: Text('real_estate_contracts_title'.tr()),
                    icon: const Icon(Icons.home_work_outlined, size: 18),
                  ),
                  ButtonSegment(
                    value: 'profit',
                    label: Text('real_estate_profit_title'.tr()),
                    icon: const Icon(Icons.trending_up_rounded, size: 18),
                  ),
                ],
                selected: {controller.mode.value},
                onSelectionChanged: (s) => controller.setMode(s.first),
              ),
            ),
          ),
          Obx(
            () => AppFilterBar(
              showDateRange: true,
              from: controller.from.value,
              to: controller.to.value,
              onPickFrom: controller.pickFromDate,
              onPickTo: controller.pickToDate,
              filterChips: controller.mode.value == 'contracts'
                  ? [
                      FilterChipOption(
                        id: 'Active',
                        label: 'filter_status_active'.tr(),
                      ),
                      FilterChipOption(
                        id: 'Completed',
                        label: 'filter_status_completed'.tr(),
                      ),
                    ]
                  : const [],
              onFilterSelected: controller.updateStatusFilter,
              onClear: controller.clearFilters,
            ),
          ),
          Expanded(
            child: Obx(() {
              if (controller.isLoading.value &&
                  ((controller.mode.value == 'contracts' &&
                          controller.report.value == null) ||
                      (controller.mode.value == 'profit' &&
                          controller.profit.value == null))) {
                return const Center(child: CircularProgressIndicator());
              }
              if (controller.error.value != null &&
                  ((controller.mode.value == 'contracts' &&
                          controller.report.value == null) ||
                      (controller.mode.value == 'profit' &&
                          controller.profit.value == null))) {
                return ErrorStateWidget(
                  message: AppExceptionHandler.messageFor(
                    controller.error.value,
                  ),
                  onRetry: controller.load,
                );
              }

              if (controller.mode.value == 'profit') {
                final profit = controller.profit.value;
                if (profit == null) {
                  return EmptyStateWidget(
                    message: 'no_data'.tr(),
                    onRetry: controller.load,
                  );
                }
                return RefreshIndicator(
                  onRefresh: controller.load,
                  child: _ProfitBody(profit: profit),
                );
              }

              final report = controller.report.value;
              if (report == null) {
                return EmptyStateWidget(
                  message: 'no_data'.tr(),
                  onRetry: controller.load,
                );
              }

              return RefreshIndicator(
                onRefresh: controller.load,
                child: _ReportBody(report: report),
              );
            }),
          ),
        ],
      ),
    );
  }
}

class _ProfitBody extends StatelessWidget {
  const _ProfitBody({required this.profit});

  final RealEstateProfitReportDto profit;

  @override
  Widget build(BuildContext context) {
    final monthlyLabels =
        profit.monthlySeries.map((e) => e.period).toList(growable: false);

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 120),
      children: [
        Text(
          'real_estate_profit_formula'.tr(),
          style: Theme.of(context).textTheme.bodySmall?.copyWith(
                fontStyle: FontStyle.italic,
                color: Theme.of(context).hintColor,
              ),
        ),
        const SizedBox(height: 12),
        AppBalanceHeroCard(
          title: 'real_estate_net_profit'.tr(),
          value: formatCurrency(profit.netProfit),
          subtitle: 'real_estate_gross_profit'.tr(),
          trendLabel: formatCurrency(profit.grossProfit),
          trendPositive: profit.netProfit >= 0,
        ),
        const SizedBox(height: 16),
        AppKpiGrid(
          childAspectRatio: 1.35,
          items: [
            AppKpiItem(
              title: 'real_estate_sale_revenue'.tr(),
              value: formatCurrency(profit.saleRevenue),
              icon: Icons.trending_up_rounded,
              color: AppColors.success,
              compact: true,
            ),
            AppKpiItem(
              title: 'real_estate_purchase_cost'.tr(),
              value: formatCurrency(profit.purchaseCost),
              icon: Icons.trending_down_rounded,
              color: AppColors.moduleOrange,
              compact: true,
            ),
            AppKpiItem(
              title: 'real_estate_expense_total'.tr(),
              value: formatCurrency(profit.totalExpenses),
              icon: Icons.money_off_csred_outlined,
              color: AppColors.warning,
              compact: true,
            ),
            AppKpiItem(
              title: 'real_estate_net_cash'.tr(),
              value: formatCurrency(profit.netCash),
              icon: Icons.account_balance_wallet_outlined,
              color: AppColors.moduleCyan,
              compact: true,
            ),
          ],
        ),
        if (profit.monthlySeries.isNotEmpty) ...[
          const SizedBox(height: 16),
          AppChartCard(
            title: 'real_estate_profit_monthly'.tr(),
            child: AppGroupedBarChart(
              labels: monthlyLabels,
              series: [
                AppChartSeries(
                  label: 'real_estate_sale_revenue'.tr(),
                  values: profit.monthlySeries
                      .map((e) => e.saleRevenue)
                      .toList(growable: false),
                  color: AppColors.success,
                ),
                AppChartSeries(
                  label: 'real_estate_expense_total'.tr(),
                  values: profit.monthlySeries
                      .map((e) => e.expenses)
                      .toList(growable: false),
                  color: AppColors.warning,
                ),
                AppChartSeries(
                  label: 'real_estate_net_profit'.tr(),
                  values: profit.monthlySeries
                      .map((e) => e.netProfit)
                      .toList(growable: false),
                  color: AppColors.primary,
                ),
              ],
            ),
          ),
        ],
        if (profit.expensesByType.isNotEmpty) ...[
          const SizedBox(height: 16),
          AppChartCard(
            title: 'real_estate_expenses_by_type'.tr(),
            height: 180,
            child: AppDonutChart(
              valueAsCurrency: true,
              centerLabel: 'real_estate_expense_total'.tr(),
              centerValue: formatCurrency(profit.totalExpenses),
              sections: [
                for (final point in profit.expensesByType.take(6))
                  (point.name, point.amount, AppColors.moduleOrange),
              ],
            ),
          ),
        ],
      ],
    );
  }
}

class _ReportBody extends StatelessWidget {
  const _ReportBody({required this.report});

  final RealEstateReportDto report;

  @override
  Widget build(BuildContext context) {
    final monthlyLabels =
        report.monthlyContracts.map((e) => e.name).toList(growable: false);
    final monthlyValues =
        report.monthlyContracts.map((e) => e.value).toList(growable: false);

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 120),
      children: [
        AppBalanceHeroCard(
          title: 'real_estate_report_total'.tr(),
          value: formatCurrency(report.totalValue),
          subtitle: 'real_estate_remaining'.tr(),
          trendLabel: formatCurrency(report.totalRemaining),
          trendPositive: report.totalRemaining <= 0,
        ),
        const SizedBox(height: 16),
        AppKpiGrid(
          childAspectRatio: 1.55,
          items: [
            AppKpiItem(
              title: 'real_estate_contracts_title'.tr(),
              value: '${report.contractCount}',
              icon: Icons.home_work_outlined,
              color: AppColors.primary,
              compact: true,
            ),
            AppKpiItem(
              title: 'real_estate_kpi_received'.tr(),
              value: formatCurrency(report.totalReceived),
              icon: Icons.payments_rounded,
              color: AppColors.success,
              compact: true,
            ),
          ],
        ),
        const SizedBox(height: 16),
        AppChartCard(
          title: 'real_estate_chart_monthly'.tr(),
          child: AppGroupedBarChart(
            labels: monthlyLabels,
            series: [
              AppChartSeries(
                label: 'real_estate_contracts_title'.tr(),
                values: monthlyValues,
                color: AppColors.primary,
              ),
            ],
          ),
        ),
        if (report.collectedVsRemaining.isNotEmpty) ...[
          const SizedBox(height: 16),
          AppChartCard(
            title: 'real_estate_chart_collected_remaining'.tr(),
            height: 180,
            child: AppDonutChart(
              valueAsCurrency: true,
              centerLabel: 'real_estate_remaining'.tr(),
              centerValue: formatCurrency(report.totalRemaining),
              sections: [
                for (final point in report.collectedVsRemaining)
                  (
                    realEstatePaymentStatusLabel(point.name),
                    point.amount,
                    realEstatePaymentStatusColor(point.name),
                  ),
              ],
            ),
          ),
        ],
        if (report.byPropertyType.isNotEmpty) ...[
          const SizedBox(height: 16),
          AppChartCard(
            title: 'real_estate_chart_property_types'.tr(),
            height: (report.byPropertyType.length * 42.0).clamp(120, 240),
            child: AppHorizontalBarChart(
              points: [
                for (final p in report.byPropertyType.take(8))
                  (
                    p.name.isEmpty
                        ? '—'
                        : realEstatePaymentStatusLabel(p.name),
                    p.value,
                  ),
              ],
            ),
          ),
        ],
        const SizedBox(height: 20),
        Text(
          'real_estate_contracts_title'.tr(),
          style: Theme.of(context).textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.w800,
              ),
        ),
        const SizedBox(height: 10),
        if (report.rows.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 32),
            child: EmptyStateWidget(message: 'no_data'.tr()),
          )
        else
          ...report.rows.map(
            (r) => Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: AppEntityCard(
                title: r.contractNumber,
                subtitle:
                    '${r.buyerName} • ${formatDate(r.contractDate)}\n${r.propertySummary.isEmpty ? realEstatePaymentStatusLabel(r.propertyType) : r.propertySummary}',
                leading: Container(
                  width: 46,
                  height: 46,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.12),
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Icons.home_work_outlined,
                    color: AppColors.primary,
                  ),
                ),
                trailing: Text(
                  formatCurrency(r.totalPrice),
                  style: const TextStyle(fontWeight: FontWeight.w800),
                ),
                onTap: () => Get.toNamed(
                  AppRoutes.realEstateContractDetailPath(r.syncId),
                ),
              ),
            ),
          ),
      ],
    );
  }
}
