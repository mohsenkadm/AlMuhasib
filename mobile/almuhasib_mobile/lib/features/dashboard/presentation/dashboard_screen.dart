import 'package:easy_localization/easy_localization.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../controllers/dashboard_controller.dart';
import '../../../shared/models/dashboard_models.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/shimmer_widgets.dart';

class DashboardScreen extends GetView<DashboardController> {
  const DashboardScreen({super.key});

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
                  onRefresh: controller.reload,
                  child: _DashboardBody(
                    data: data,
                    companyName: controller.companyName,
                  ),
                );
              }

              if (isLoading) return const DashboardShimmer();

              if (error != null) {
                return RefreshIndicator(
                  onRefresh: controller.reload,
                  child: ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    children: [
                      SizedBox(
                        height: MediaQuery.sizeOf(context).height * 0.6,
                        child: ErrorStateWidget(
                          message: AppExceptionHandler.messageFor(error),
                          onRetry: controller.reload,
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

class _DashboardBody extends StatelessWidget {
  const _DashboardBody({required this.data, required this.companyName});

  final DashboardData data;
  final String companyName;

  @override
  Widget build(BuildContext context) {
    final topPadding = MediaQuery.paddingOf(context).top;

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: EdgeInsets.fromLTRB(20, topPadding + 8, 20, 120),
      children: [
        _DashboardHeader(companyName: companyName).fadeSlideIn(),
        const SizedBox(height: 18),
        AppBalanceHeroCard(
          title: 'bank_balance'.tr(),
          value: formatCurrency(data.bankBalance),
          subtitle: 'inventory_value'.tr(),
          trendLabel: formatCurrency(data.totalInventoryValue),
          trendPositive: true,
        ).fadeSlideIn(delayMs: 60),
        const SizedBox(height: 20),
        Row(
          children: [
            Expanded(
              child: Text(
                'dashboard_stats'.tr(),
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
              ),
            ),
            TextButton(
              onPressed: () => Get.toNamed(AppRoutes.data),
              child: Text('view_all'.tr()),
            ),
          ],
        ).fadeSlideIn(delayMs: 100),
        const SizedBox(height: 8),
        GridView.count(
          crossAxisCount: 2,
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          mainAxisSpacing: 12,
          crossAxisSpacing: 12,
          childAspectRatio: 1.05,
          children: [
            KpiCard(
              title: 'today_sales'.tr(),
              value: formatCurrency(data.todaySales),
              icon: Icons.trending_up_rounded,
              color: AppColors.moduleGreen,
            ).fadeSlideInList(index: 0),
            KpiCard(
              title: 'net_profit'.tr(),
              value: formatCurrency(data.netProfit),
              icon: Icons.account_balance_wallet_outlined,
              color: AppColors.modulePurple,
            ).fadeSlideInList(index: 1),
            KpiCard(
              title: 'today_purchases'.tr(),
              value: formatCurrency(data.todayPurchases),
              icon: Icons.shopping_cart_outlined,
              color: AppColors.moduleOrange,
            ).fadeSlideInList(index: 2),
            GestureDetector(
              onTap: () => Get.toNamed(
                AppRoutes.installments,
                arguments: 'overdue',
              ),
              child: KpiCard(
                title: 'overdue_installments'.tr(),
                value: '${data.overdueInstallmentsCount}',
                icon: Icons.warning_amber_rounded,
                color: AppColors.warning,
              ),
            ).fadeSlideInList(index: 3),
          ],
        ),
        const SizedBox(height: 22),
        Container(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 12),
          decoration: BoxDecoration(
            color: Theme.of(context).brightness == Brightness.dark
                ? AppColors.surfaceDarkCard
                : Colors.white,
            borderRadius: BorderRadius.circular(20),
            boxShadow: AppColors.cardShadow(
              dark: Theme.of(context).brightness == Brightness.dark,
            ),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'sales_chart'.tr(),
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
              ),
              const SizedBox(height: 14),
              SizedBox(
                height: 200,
                child: _SalesChart(points: data.salesLast30Days),
              ),
            ],
          ),
        ).fadeSlideIn(delayMs: 220),
        const SizedBox(height: 22),
        _SectionTitle(title: 'recent_transactions'.tr()),
        ...data.recentTransactions.take(5).toList().asMap().entries.map(
              (entry) {
                final tx = entry.value;
                final positive = tx.amount >= 0;
                return Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: AppEntityCard(
                    title: '${tx.type} — ${tx.number}',
                    subtitle: '${tx.party} • ${formatDate(tx.date)}',
                    leading: Container(
                      width: 46,
                      height: 46,
                      decoration: BoxDecoration(
                        color: (positive ? AppColors.success : AppColors.error)
                            .withValues(alpha: 0.14),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        positive
                            ? Icons.arrow_downward_rounded
                            : Icons.arrow_upward_rounded,
                        color: positive ? AppColors.success : AppColors.error,
                        size: 22,
                      ),
                    ),
                    trailing: Text(
                      formatCurrency(tx.amount),
                      style: TextStyle(
                        fontWeight: FontWeight.w800,
                        color: positive ? AppColors.success : AppColors.error,
                      ),
                    ),
                  ).fadeSlideInList(index: entry.key),
                );
              },
            ),
        const SizedBox(height: 12),
        _SectionTitle(title: 'upcoming_installments'.tr()),
        ...data.upcomingInstallments.take(5).map(
              (i) => Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: AppEntityCard(
                  title: i.customerName,
                  subtitle:
                      '${formatDate(i.dueDate)} • ${i.daysRemaining} ${'days'.tr()}',
                  leading: Container(
                    width: 46,
                    height: 46,
                    decoration: BoxDecoration(
                      color: AppColors.warning.withValues(alpha: 0.14),
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(
                      Icons.schedule_rounded,
                      color: AppColors.warning,
                    ),
                  ),
                  trailing: Text(
                    formatCurrency(i.amount),
                    style: const TextStyle(fontWeight: FontWeight.w800),
                  ),
                  onTap: () => Get.toNamed(
                    AppRoutes.installments,
                    arguments: 'upcoming',
                  ),
                ),
              ),
            ),
        TextButton(
          onPressed: () => Get.toNamed(AppRoutes.installments),
          child: Text('view_all_installments'.tr()),
        ),
        if (data.cashBoxes.isNotEmpty) ...[
          const SizedBox(height: 8),
          _SectionTitle(title: 'cash_boxes'.tr()),
          ...data.cashBoxes.map(
            (c) => Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: AppEntityCard(
                title: c.name,
                leading: Container(
                  width: 46,
                  height: 46,
                  decoration: BoxDecoration(
                    color: AppColors.moduleCyan.withValues(alpha: 0.14),
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Icons.account_balance_wallet_outlined,
                    color: AppColors.moduleCyan,
                  ),
                ),
                trailing: Text(
                  formatCurrency(c.balance),
                  style: const TextStyle(fontWeight: FontWeight.w800),
                ),
              ),
            ),
          ),
        ],
      ],
    );
  }
}

class _DashboardHeader extends StatelessWidget {
  const _DashboardHeader({required this.companyName});

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
                'dashboard_greeting'.tr(),
                style: Theme.of(context).textTheme.bodyMedium,
              ),
              Text(
                companyName,
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
            color: Colors.white,
            shape: BoxShape.circle,
            boxShadow: AppColors.cardShadow(),
          ),
          child: IconButton(
            onPressed: () => Get.toNamed(AppRoutes.invoiceNew),
            icon: const Icon(Icons.add_rounded),
            color: AppColors.primary,
            tooltip: 'new_invoice'.tr(),
          ),
        ),
        const SizedBox(width: 10),
        Container(
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            color: Colors.white,
            shape: BoxShape.circle,
            boxShadow: AppColors.cardShadow(),
          ),
          child: Obx(() {
            final unread = AppServices.notifications.unreadCount;
            return IconButton(
              onPressed: () => Get.toNamed(AppRoutes.notifications),
              icon: Badge(
                isLabelVisible: unread > 0,
                label: Text('$unread'),
                child: const Icon(Icons.notifications_none_rounded),
              ),
              color: AppColors.primary,
            );
          }),
        ),
      ],
    );
  }
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle({required this.title});

  final String title;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10, top: 4),
      child: Text(
        title,
        style: Theme.of(context).textTheme.titleMedium?.copyWith(
              fontWeight: FontWeight.w800,
            ),
      ),
    );
  }
}

class _SalesChart extends StatelessWidget {
  const _SalesChart({required this.points});

  final List<DailySalesPoint> points;

  @override
  Widget build(BuildContext context) {
    if (points.isEmpty) {
      return Center(child: Text('no_data'.tr()));
    }

    final spots = points.asMap().entries.map((e) {
      return FlSpot(e.key.toDouble(), e.value.amount);
    }).toList();

    return LineChart(
      LineChartData(
        gridData: FlGridData(
          show: true,
          drawVerticalLine: false,
          getDrawingHorizontalLine: (v) => FlLine(
            color: Theme.of(context).dividerColor.withValues(alpha: 0.12),
            strokeWidth: 1,
          ),
        ),
        titlesData: FlTitlesData(
          topTitles: const AxisTitles(),
          rightTitles: const AxisTitles(),
          leftTitles: AxisTitles(
            sideTitles: SideTitles(
              showTitles: true,
              reservedSize: 42,
              getTitlesWidget: (value, _) => Text(
                value >= 1000
                    ? '${(value / 1000).toStringAsFixed(0)}k'
                    : '${value.toInt()}',
                style: const TextStyle(fontSize: 10),
              ),
            ),
          ),
          bottomTitles: AxisTitles(
            sideTitles: SideTitles(
              showTitles: true,
              interval: (points.length / 4).clamp(1, 7).toDouble(),
              getTitlesWidget: (value, _) {
                final index = value.toInt();
                if (index < 0 || index >= points.length) {
                  return const SizedBox.shrink();
                }
                return Padding(
                  padding: const EdgeInsets.only(top: 6),
                  child: Text(
                    shortDateFormat.format(points[index].date),
                    style: const TextStyle(fontSize: 10),
                  ),
                );
              },
            ),
          ),
        ),
        borderData: FlBorderData(show: false),
        lineBarsData: [
          LineChartBarData(
            spots: spots,
            isCurved: true,
            color: AppColors.primary,
            barWidth: 3.5,
            isStrokeCapRound: true,
            dotData: FlDotData(
              show: true,
              getDotPainter: (spot, percent, bar, index) => FlDotCirclePainter(
                radius: 3.5,
                color: Colors.white,
                strokeWidth: 2.5,
                strokeColor: AppColors.primary,
              ),
            ),
            belowBarData: BarAreaData(
              show: true,
              gradient: LinearGradient(
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
                colors: [
                  AppColors.primary.withValues(alpha: 0.28),
                  AppColors.primary.withValues(alpha: 0.02),
                ],
              ),
            ),
          ),
        ],
        lineTouchData: LineTouchData(
          handleBuiltInTouches: true,
          touchTooltipData: LineTouchTooltipData(
            getTooltipItems: (touched) => touched
                .map(
                  (spot) => LineTooltipItem(
                    formatCurrency(spot.y),
                    const TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.w600,
                      fontSize: 12,
                    ),
                  ),
                )
                .toList(),
          ),
        ),
      ),
    );
  }
}
