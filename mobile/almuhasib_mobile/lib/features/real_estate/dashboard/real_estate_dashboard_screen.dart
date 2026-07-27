import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/shimmer_widgets.dart';
import '../controllers/real_estate_dashboard_controller.dart';
import '../models/real_estate_models.dart';
import '../widgets/real_estate_labels.dart';

class RealEstateDashboardScreen extends GetView<RealEstateDashboardController> {
  const RealEstateDashboardScreen({super.key});

  @override
  final String? tag = 'real_estate_dashboard';

  @override
  Widget build(BuildContext context) {
    final company =
        AppServices.prefs.companyName ?? AppServices.prefs.tenantName ?? '';

    return Scaffold(
      body: Column(
        children: [
          Obx(
            () => ConnectivityBanner(
              isOffline: AppServices.connectivity.isOffline.value,
            ),
          ),
          Expanded(
            child: Obx(() {
              final data = controller.data.value;
              if (data != null) {
                return RefreshIndicator(
                  onRefresh: controller.load,
                  child: _RealEstateDashboardBody(
                    data: data,
                    companyName: company,
                  ),
                );
              }

              if (controller.isLoading.value) {
                return const DashboardShimmer();
              }

              final error = controller.error.value;
              if (error != null) {
                return RefreshIndicator(
                  onRefresh: controller.load,
                  child: ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    children: [
                      SizedBox(
                        height: MediaQuery.sizeOf(context).height * 0.6,
                        child: ErrorStateWidget(
                          message: AppExceptionHandler.messageFor(error),
                          onRetry: controller.load,
                        ),
                      ),
                    ],
                  ),
                );
              }

              return const DashboardShimmer();
            }),
          ),
        ],
      ),
    );
  }
}

class _RealEstateDashboardBody extends StatelessWidget {
  const _RealEstateDashboardBody({
    required this.data,
    required this.companyName,
  });

  final RealEstateDashboardDto data;
  final String companyName;

  @override
  Widget build(BuildContext context) {
    final topPadding = MediaQuery.paddingOf(context).top;
    final monthlyLabels =
        data.monthlyContracts.map((e) => e.name).toList(growable: false);
    final monthlyValues =
        data.monthlyContracts.map((e) => e.value).toList(growable: false);

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: EdgeInsets.fromLTRB(20, topPadding + 8, 20, 120),
      children: [
        _Header(companyName: companyName),
        const SizedBox(height: 18),
        AppBalanceHeroCard(
          title: 'real_estate_kpi_received'.tr(),
          value: formatCurrency(data.totalReceived),
          subtitle: 'real_estate_remaining'.tr(),
          trendLabel: formatCurrency(data.totalRemaining),
          trendPositive: data.totalRemaining <= 0,
        ),
        const SizedBox(height: 20),
        Text(
          'dashboard_stats'.tr(),
          style: Theme.of(context).textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.w800,
              ),
        ),
        const SizedBox(height: 8),
        AppKpiGrid(
          childAspectRatio: 1.2,
          items: [
            AppKpiItem(
              title: 'real_estate_kpi_today'.tr(),
              value: '${data.todayContracts}',
              icon: Icons.today_rounded,
              color: AppColors.moduleGreen,
            ),
            AppKpiItem(
              title: 'real_estate_kpi_month'.tr(),
              value: '${data.monthContracts}',
              icon: Icons.calendar_month_rounded,
              color: AppColors.moduleCyan,
            ),
            AppKpiItem(
              title: 'real_estate_kpi_unpaid'.tr(),
              value: '${data.unpaidContracts}',
              icon: Icons.warning_amber_rounded,
              color: AppColors.warning,
            ),
            AppKpiItem(
              title: 'real_estate_total_price'.tr(),
              value: formatCurrency(data.totalValue),
              icon: Icons.home_work_outlined,
              color: AppColors.moduleOrange,
            ),
          ],
        ),
        const SizedBox(height: 20),
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
        const SizedBox(height: 16),
        AppChartCard(
          title: 'real_estate_chart_payment_status'.tr(),
          height: 180,
          child: AppDonutChart(
            centerLabel: 'real_estate_kpi_unpaid'.tr(),
            centerValue: '${data.unpaidContracts}',
            sections: [
              for (final point in data.paymentStatusChart)
                (
                  realEstatePaymentStatusLabel(point.name),
                  point.amount,
                  realEstatePaymentStatusColor(point.name),
                ),
            ],
          ),
        ),
        if (data.byPropertyType.isNotEmpty) ...[
          const SizedBox(height: 16),
          AppChartCard(
            title: 'real_estate_chart_property_types'.tr(),
            height: (data.byPropertyType.length * 42.0).clamp(120, 240),
            child: AppHorizontalBarChart(
              points: [
                for (final p in data.byPropertyType.take(8))
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
        if (data.byContractType.isNotEmpty) ...[
          const SizedBox(height: 16),
          AppChartCard(
            title: 'real_estate_chart_contract_types'.tr(),
            height: (data.byContractType.length * 42.0).clamp(120, 240),
            child: AppHorizontalBarChart(
              color: AppColors.moduleOrange,
              points: [
                for (final p in data.byContractType.take(8))
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
        const SizedBox(height: 22),
        Row(
          children: [
            Expanded(
              child: Text(
                'real_estate_recent_contracts'.tr(),
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
              ),
            ),
            TextButton(
              onPressed: () => Get.toNamed(AppRoutes.realEstateContracts),
              child: Text('view_all'.tr()),
            ),
          ],
        ),
        const SizedBox(height: 8),
        if (data.recentContracts.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 24),
            child: Center(child: Text('real_estate_no_contracts'.tr())),
          )
        else
          ...data.recentContracts.take(6).map(
            (c) => Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: AppEntityCard(
                title: c.contractNumber,
                subtitle:
                    '${c.buyerName} • ${c.propertySummary.isEmpty ? realEstatePaymentStatusLabel(c.propertyType) : c.propertySummary}\n${formatDate(c.contractDate)}',
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
                trailing: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      formatCurrency(c.totalPrice),
                      style: const TextStyle(fontWeight: FontWeight.w800),
                    ),
                    if (c.remainingAmount > 0)
                      Text(
                        formatCurrency(c.remainingAmount),
                        style: TextStyle(
                          color: Theme.of(context).colorScheme.error,
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                  ],
                ),
                onTap: () => Get.toNamed(
                  AppRoutes.realEstateContractDetailPath(c.syncId),
                ),
              ),
            ),
          ),
      ],
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.companyName});

  final String companyName;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        const AppLogoMark(size: 48),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'real_estate_dashboard_title'.tr(),
                style: Theme.of(context).textTheme.bodyMedium,
              ),
              Text(
                companyName.isEmpty ? 'app_name'.tr() : companyName,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: Theme.of(context).textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
              ),
            ],
          ),
        ),
        Container(
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            color: Theme.of(context).brightness == Brightness.dark
                ? AppColors.surfaceDarkCard
                : Colors.white,
            shape: BoxShape.circle,
            boxShadow: AppColors.cardShadow(),
          ),
          child: IconButton(
            onPressed: () => Get.toNamed(AppRoutes.realEstateContractNew),
            icon: const Icon(Icons.add_rounded),
            color: AppColors.primary,
            tooltip: 'real_estate_new_contract'.tr(),
          ),
        ),
        const SizedBox(width: 10),
        Container(
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            color: Theme.of(context).brightness == Brightness.dark
                ? AppColors.surfaceDarkCard
                : Colors.white,
            shape: BoxShape.circle,
            boxShadow: AppColors.cardShadow(),
          ),
          child: IconButton(
            onPressed: () => Get.toNamed(AppRoutes.profile),
            icon: const Icon(Icons.person_outline_rounded),
            color: AppColors.primary,
          ),
        ),
      ],
    );
  }
}
