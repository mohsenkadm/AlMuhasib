import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/shimmer_widgets.dart';
import '../controllers/car_trade_dashboard_controller.dart';
import '../models/car_trade_models.dart';
import '../widgets/car_trade_labels.dart';

class CarTradeDashboardScreen extends GetView<CarTradeDashboardController> {
  const CarTradeDashboardScreen({super.key});

  @override
  final String? tag = 'car_trade_dashboard';

  @override
  Widget build(BuildContext context) {
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
              final isLoading = controller.isLoading.value;
              final error = controller.error.value;

              if (data != null) {
                return RefreshIndicator(
                  onRefresh: controller.load,
                  child: _CarTradeDashboardBody(data: data),
                );
              }

              if (isLoading) return const DashboardShimmer();

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

class _CarTradeDashboardBody extends StatelessWidget {
  const _CarTradeDashboardBody({required this.data});

  final CarTradeDashboardDto data;

  @override
  Widget build(BuildContext context) {
    final topPadding = MediaQuery.paddingOf(context).top;
    final company =
        AppServices.prefs.companyName ?? AppServices.prefs.tenantName ?? '';
    final monthly = alignNamedSeries(
      data.monthlyBuy.map((e) => (e.name, e.value)).toList(),
      data.monthlySell.map((e) => (e.name, e.value)).toList(),
    );

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: EdgeInsets.fromLTRB(20, topPadding + 8, 20, 120),
      children: [
        _Header(companyName: company).fadeSlideIn(),
        const SizedBox(height: 18),
        AppBalanceHeroCard(
          title: 'car_trade_kpi_paid'.tr(),
          value: formatCurrency(data.totalPaid),
          subtitle: 'car_trade_remaining'.tr(),
          trendLabel: formatCurrency(data.totalRemaining),
          trendPositive: data.totalRemaining <= 0,
        ).fadeSlideIn(delayMs: 40),
        const SizedBox(height: 20),
        Text(
          'dashboard_stats'.tr(),
          style: Theme.of(context).textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.w800,
              ),
        ).fadeSlideIn(delayMs: 80),
        const SizedBox(height: 8),
        AppKpiGrid(
          childAspectRatio: 1.2,
          items: [
            AppKpiItem(
              title: 'car_trade_kpi_today'.tr(),
              value: '${data.todayTransactions}',
              icon: Icons.today_rounded,
              color: AppColors.moduleGreen,
            ),
            AppKpiItem(
              title: 'car_trade_kpi_month'.tr(),
              value: '${data.monthTransactions}',
              icon: Icons.calendar_month_rounded,
              color: AppColors.modulePurple,
            ),
            AppKpiItem(
              title: 'car_trade_type_buy'.tr(),
              value: '${data.buyCount}',
              icon: Icons.shopping_cart_outlined,
              color: AppColors.moduleOrange,
            ),
            AppKpiItem(
              title: 'car_trade_type_sell'.tr(),
              value: '${data.sellCount}',
              icon: Icons.sell_outlined,
              color: AppColors.moduleCyan,
            ),
          ],
        ).fadeSlideIn(delayMs: 100),
        const SizedBox(height: 20),
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
        ).fadeSlideIn(delayMs: 160),
        const SizedBox(height: 16),
        AppChartCard(
          title: 'car_trade_chart_payment_status'.tr(),
          height: 180,
          child: AppDonutChart(
            centerLabel: 'car_trade_kpi_unpaid'.tr(),
            centerValue: '${data.unpaidTransactions}',
            sections: [
              for (final point in data.paymentStatusChart)
                (
                  carTradePaymentStatusLabel(point.name),
                  point.amount,
                  carTradePaymentStatusColor(point.name),
                ),
            ],
          ),
        ).fadeSlideIn(delayMs: 200),
        if (data.topCarTypes.isNotEmpty) ...[
          const SizedBox(height: 16),
          AppChartCard(
            title: 'car_trade_chart_car_types'.tr(),
            height: (data.topCarTypes.length * 42.0).clamp(120, 240),
            child: AppHorizontalBarChart(
              color: AppColors.primary,
              points: [
                for (final p in data.topCarTypes.take(8))
                  (p.name.isEmpty ? '—' : p.name, p.value),
              ],
            ),
          ).fadeSlideIn(delayMs: 240),
        ],
        const SizedBox(height: 22),
        Row(
          children: [
            Expanded(
              child: Text(
                'recent_transactions'.tr(),
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
              ),
            ),
            TextButton(
              onPressed: () => Get.toNamed(AppRoutes.carTradeTransactions),
              child: Text('view_all'.tr()),
            ),
          ],
        ),
        const SizedBox(height: 8),
        if (data.recentTransactions.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 24),
            child: Center(child: Text('car_trade_no_transactions'.tr())),
          )
        else
          ...data.recentTransactions.take(6).toList().asMap().entries.map(
            (entry) {
              final tx = entry.value;
              final buy = tx.isBuy;
              return Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: AppEntityCard(
                  title: tx.transactionNumber,
                  subtitle:
                      '${carTradeTypeLabel(tx.tradeType)} • ${tx.carName.isEmpty ? tx.plateNumber : tx.carName}\n${formatDate(tx.transactionDate)}',
                  leading: Container(
                    width: 46,
                    height: 46,
                    decoration: BoxDecoration(
                      color: (buy ? AppColors.moduleOrange : AppColors.moduleCyan)
                          .withValues(alpha: 0.14),
                      shape: BoxShape.circle,
                    ),
                    child: Icon(
                      buy
                          ? Icons.shopping_cart_outlined
                          : Icons.sell_outlined,
                      color: buy ? AppColors.moduleOrange : AppColors.moduleCyan,
                    ),
                  ),
                  trailing: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        formatCurrency(tx.totalAmount),
                        style: const TextStyle(fontWeight: FontWeight.w800),
                      ),
                      if (tx.remainingAmount > 0)
                        Text(
                          formatCurrency(tx.remainingAmount),
                          style: TextStyle(
                            color: Theme.of(context).colorScheme.error,
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                    ],
                  ),
                  onTap: () => Get.toNamed(
                    AppRoutes.carTradeTransactionDetailPath(tx.syncId),
                  ),
                ).fadeSlideInList(index: entry.key),
              );
            },
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
                'car_trade_dashboard_title'.tr(),
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
            onPressed: () => Get.toNamed(AppRoutes.carTradeTransactionNew),
            icon: const Icon(Icons.add_rounded),
            color: AppColors.primary,
            tooltip: 'car_trade_new_transaction'.tr(),
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
