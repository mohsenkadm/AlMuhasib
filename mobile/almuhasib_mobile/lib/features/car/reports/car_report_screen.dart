import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../controllers/car_report_controller.dart';
import '../models/car_models.dart';
import '../widgets/car_labels.dart';

class CarReportScreen extends GetView<CarReportController> {
  const CarReportScreen({super.key});

  @override
  final String? tag = 'car_report';

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: 'car_report_title'.tr(),
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
                FilterChipOption(
                  id: 'Active',
                  label: 'filter_status_active'.tr(),
                ),
                FilterChipOption(
                  id: 'Completed',
                  label: 'filter_status_completed'.tr(),
                ),
              ],
              onFilterSelected: controller.updateStatusFilter,
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

  final CarReportDto report;

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
          title: 'car_report_total'.tr(),
          value: formatCurrency(report.totalCarValue),
          subtitle: 'car_remaining'.tr(),
          trendLabel: formatCurrency(report.totalRemaining),
          trendPositive: report.totalRemaining <= 0,
        ),
        const SizedBox(height: 16),
        _KpiRow(
          left: KpiCard(
            title: 'car_contracts_title'.tr(),
            value: '${report.contractCount}',
            icon: Icons.description_outlined,
            color: AppColors.primary,
            compact: true,
          ),
          right: KpiCard(
            title: 'car_kpi_received'.tr(),
            value: formatCurrency(report.totalReceived),
            icon: Icons.payments_rounded,
            color: AppColors.success,
            compact: true,
          ),
        ),
        const SizedBox(height: 16),
        AppChartCard(
          title: 'car_chart_monthly'.tr(),
          child: AppGroupedBarChart(
            labels: monthlyLabels,
            series: [
              AppChartSeries(
                label: 'car_contracts_title'.tr(),
                values: monthlyValues,
                color: AppColors.primary,
              ),
            ],
          ),
        ),
        if (report.collectedVsRemaining.isNotEmpty) ...[
          const SizedBox(height: 16),
          AppChartCard(
            title: 'car_chart_collected_remaining'.tr(),
            height: 180,
            child: AppDonutChart(
              valueAsCurrency: true,
              centerLabel: 'car_remaining'.tr(),
              centerValue: formatCurrency(report.totalRemaining),
              sections: [
                for (final point in report.collectedVsRemaining)
                  (
                    carPaymentStatusLabel(point.name),
                    point.amount,
                    carPaymentStatusColor(point.name),
                  ),
              ],
            ),
          ),
        ],
        if (report.byCarType.isNotEmpty) ...[
          const SizedBox(height: 16),
          AppChartCard(
            title: 'car_chart_car_types'.tr(),
            height: (report.byCarType.length * 42.0).clamp(120, 240),
            child: AppHorizontalBarChart(
              points: [
                for (final p in report.byCarType.take(8))
                  (p.name.isEmpty ? '—' : p.name, p.value),
              ],
            ),
          ),
        ],
        const SizedBox(height: 20),
        Text(
          'car_contracts_title'.tr(),
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
                    '${r.buyerName} • ${formatDate(r.contractDate)}\n${r.carType.isEmpty ? r.plateNumber : r.carType}',
                leading: Container(
                  width: 46,
                  height: 46,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.12),
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Icons.description_outlined,
                    color: AppColors.primary,
                  ),
                ),
                trailing: Text(
                  formatCurrency(r.carPrice),
                  style: const TextStyle(fontWeight: FontWeight.w800),
                ),
                onTap: () =>
                    Get.toNamed(AppRoutes.carContractDetailPath(r.syncId)),
              ),
            ),
          ),
      ],
    );
  }
}

class _KpiRow extends StatelessWidget {
  const _KpiRow({required this.left, required this.right});

  final Widget left;
  final Widget right;

  @override
  Widget build(BuildContext context) {
    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Expanded(child: left),
          const SizedBox(width: 12),
          Expanded(child: right),
        ],
      ),
    );
  }
}
