import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../controllers/car_trade_report_controller.dart';
import '../models/car_trade_models.dart';
import '../widgets/car_trade_labels.dart';

class CarTradeReportScreen extends GetView<CarTradeReportController> {
  const CarTradeReportScreen({super.key});

  @override
  final String? tag = 'car_trade_report';

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: 'car_trade_report_title'.tr(),
      actions: [
        IconButton(
          tooltip: 'car_trade_party_statement_title'.tr(),
          onPressed: () => Get.toNamed(AppRoutes.carTradePartyStatement),
          icon: const Icon(Icons.receipt_long_rounded, color: Colors.white),
        ),
      ],
      body: Column(
        children: [
          Obx(
            () => AppFilterBar(
              showDateRange: true,
              from: controller.from.value,
              to: controller.to.value,
              onPickFrom: controller.pickFromDate,
              onPickTo: controller.pickToDate,
              filterChips: [
                FilterChipOption(id: 'Buy', label: 'car_trade_type_buy'.tr()),
                FilterChipOption(id: 'Sell', label: 'car_trade_type_sell'.tr()),
                FilterChipOption(
                  id: 'Active',
                  label: 'filter_status_active'.tr(),
                ),
                FilterChipOption(
                  id: 'Completed',
                  label: 'filter_status_completed'.tr(),
                ),
              ],
              onFilterSelected: (id) {
                if (id == 'Buy' || id == 'Sell') {
                  controller.updateTradeTypeFilter(id);
                } else {
                  controller.updateStatusFilter(id);
                }
              },
              onClear: controller.clearFilters,
            ),
          ),
          Expanded(
            child: Obx(() {
              if (controller.isLoading.value && controller.report.value == null) {
                return const Center(child: CircularProgressIndicator());
              }
              if (controller.error.value != null &&
                  controller.report.value == null) {
                return ErrorStateWidget(
                  message: AppExceptionHandler.messageFor(
                    controller.error.value,
                  ),
                  onRetry: controller.load,
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

class _ReportBody extends StatelessWidget {
  const _ReportBody({required this.report});

  final CarTradeReportDto report;

  @override
  Widget build(BuildContext context) {
    final monthly = alignNamedSeries(
      report.monthlyBuy.map((e) => (e.name, e.value)).toList(),
      report.monthlySell.map((e) => (e.name, e.value)).toList(),
    );

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 120),
      children: [
        AppBalanceHeroCard(
          title: 'car_trade_report_total'.tr(),
          value: formatCurrency(report.totalValue),
          subtitle: 'car_trade_remaining'.tr(),
          trendLabel: formatCurrency(report.totalRemaining),
          trendPositive: report.totalRemaining <= 0,
        ).fadeSlideIn(),
        const SizedBox(height: 16),
        AppKpiGrid(
          childAspectRatio: 1.35,
          items: [
            AppKpiItem(
              title: 'car_trade_type_buy'.tr(),
              value: '${report.buyCount}',
              icon: Icons.shopping_cart_outlined,
              color: AppColors.moduleOrange,
              compact: true,
            ),
            AppKpiItem(
              title: 'car_trade_type_sell'.tr(),
              value: '${report.sellCount}',
              icon: Icons.sell_outlined,
              color: AppColors.moduleCyan,
              compact: true,
            ),
            AppKpiItem(
              title: 'car_trade_kpi_paid'.tr(),
              value: formatCurrency(report.totalPaid),
              icon: Icons.payments_rounded,
              color: AppColors.success,
              compact: true,
            ),
            AppKpiItem(
              title: 'car_trade_remaining'.tr(),
              value: formatCurrency(report.totalRemaining),
              icon: Icons.warning_amber_rounded,
              color: AppColors.warning,
              compact: true,
            ),
          ],
        ).fadeSlideIn(delayMs: 40),
        const SizedBox(height: 16),
        AppChartCard(
          title: 'car_trade_chart_monthly'.tr(),
          legend: AppChartLegend(
            items: [
              ('car_trade_type_buy'.tr(), AppColors.moduleOrange),
              ('car_trade_type_sell'.tr(), AppColors.moduleCyan),
            ],
          ),
          child: AppGroupedBarChart(
            labels: monthly.$1,
            series: [
              AppChartSeries(
                label: 'car_trade_type_buy'.tr(),
                values: monthly.$2,
                color: AppColors.moduleOrange,
              ),
              AppChartSeries(
                label: 'car_trade_type_sell'.tr(),
                values: monthly.$3,
                color: AppColors.moduleCyan,
              ),
            ],
          ),
        ).fadeSlideIn(delayMs: 80),
        if (report.collectedVsRemaining.isNotEmpty) ...[
          const SizedBox(height: 16),
          AppChartCard(
            title: 'car_trade_chart_debts'.tr(),
            height: 180,
            child: AppDonutChart(
              valueAsCurrency: true,
              centerLabel: 'car_trade_remaining'.tr(),
              centerValue: formatCurrency(report.totalRemaining),
              sections: [
                for (final point in report.collectedVsRemaining)
                  (
                    carTradePaymentStatusLabel(point.name),
                    point.amount,
                    carTradePaymentStatusColor(point.name),
                  ),
              ],
            ),
          ).fadeSlideIn(delayMs: 120),
        ],
        if (report.byCarType.isNotEmpty) ...[
          const SizedBox(height: 16),
          AppChartCard(
            title: 'car_trade_chart_car_types'.tr(),
            height: (report.byCarType.length * 42.0).clamp(120, 240),
            child: AppHorizontalBarChart(
              points: [
                for (final p in report.byCarType.take(8))
                  (p.name.isEmpty ? '—' : p.name, p.value),
              ],
            ),
          ).fadeSlideIn(delayMs: 160),
        ],
        const SizedBox(height: 20),
        Text(
          'car_trade_transactions_title'.tr(),
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
          ...report.rows.asMap().entries.map((entry) {
            final r = entry.value;
            final buy = r.isBuy;
            return Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: AppEntityCard(
                title: r.transactionNumber,
                subtitle:
                    '${carTradeTypeLabel(r.tradeType)} • ${formatDate(r.transactionDate)}\n${r.carName.isEmpty ? r.plateNumber : r.carName}',
                leading: Container(
                  width: 46,
                  height: 46,
                  decoration: BoxDecoration(
                    color: (buy ? AppColors.moduleOrange : AppColors.moduleCyan)
                        .withValues(alpha: 0.14),
                    shape: BoxShape.circle,
                  ),
                  child: Icon(
                    buy ? Icons.shopping_cart_outlined : Icons.sell_outlined,
                    color: buy ? AppColors.moduleOrange : AppColors.moduleCyan,
                  ),
                ),
                trailing: Text(
                  formatCurrency(r.totalAmount),
                  style: const TextStyle(fontWeight: FontWeight.w800),
                ),
                onTap: () => Get.toNamed(
                  AppRoutes.carTradeTransactionDetailPath(r.syncId),
                ),
              ).fadeSlideIn(delayMs: (entry.key * 25).clamp(0, 300)),
            );
          }),
      ],
    );
  }
}
