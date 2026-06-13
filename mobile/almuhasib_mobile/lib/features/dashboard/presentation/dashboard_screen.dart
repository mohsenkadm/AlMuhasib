import 'package:easy_localization/easy_localization.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/constants/app_colors.dart';
import '../../../core/providers/core_providers.dart';
import '../../../core/theme/theme_provider.dart';
import '../../../shared/models/dashboard_models.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/connectivity_provider.dart';
import '../../../shared/widgets/shimmer_widgets.dart';

final dashboardProvider = FutureProvider.autoDispose<DashboardData>((ref) {
  return ref.watch(dashboardRepositoryProvider).getDashboard();
});

class DashboardScreen extends ConsumerWidget {
  const DashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final dashboardAsync = ref.watch(dashboardProvider);
    final companyName = ref.watch(preferencesServiceProvider).companyName;
    final isOffline = ref.watch(isOfflineProvider);

    return Scaffold(
      extendBodyBehindAppBar: true,
      body: Column(
        children: [
          ConnectivityBanner(isOffline: isOffline),
          Expanded(
            child: RefreshIndicator(
              onRefresh: () async => ref.invalidate(dashboardProvider),
              edgeOffset: 120,
              child: dashboardAsync.when(
                loading: () => const DashboardShimmer(),
                error: (e, _) => ErrorStateWidget(
                  message: e.toString(),
                  onRetry: () => ref.invalidate(dashboardProvider),
                ),
                data: (data) => _DashboardBody(
                  data: data,
                  companyName: companyName ?? 'app_name'.tr(),
                ),
              ),
            ),
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
      padding: EdgeInsets.fromLTRB(20, topPadding + 12, 20, 120),
      children: [
        _DashboardHeader(companyName: companyName).fadeSlideIn(),
        const SizedBox(height: 20),
        _QuickActionButton(
          onPressed: () => context.push('/data/invoice/new'),
        ).fadeSlideIn(delayMs: 80),
        const SizedBox(height: 24),
        Text(
          'dashboard_stats'.tr(),
          style: Theme.of(context).textTheme.titleMedium,
        ).fadeSlideIn(delayMs: 120),
        const SizedBox(height: 14),
        GridView.count(
          crossAxisCount: 2,
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          mainAxisSpacing: 14,
          crossAxisSpacing: 14,
          childAspectRatio: 0.92,
          children: [
            KpiCard(
              title: 'today_sales'.tr(),
              value: formatCurrency(data.todaySales),
              icon: Icons.trending_up_rounded,
              color: AppColors.success,
            ).fadeSlideInList(index: 0),
            KpiCard(
              title: 'net_profit'.tr(),
              value: formatCurrency(data.netProfit),
              icon: Icons.account_balance_wallet_outlined,
              color: AppColors.accent,
            ).fadeSlideInList(index: 1),
            KpiCard(
              title: 'today_purchases'.tr(),
              value: formatCurrency(data.todayPurchases),
              icon: Icons.shopping_cart_outlined,
              color: AppColors.primaryLight,
            ).fadeSlideInList(index: 2),
            KpiCard(
              title: 'overdue_installments'.tr(),
              value: '${data.overdueInstallmentsCount}',
              icon: Icons.warning_amber_rounded,
              color: AppColors.warning,
            ).fadeSlideInList(index: 3),
          ],
        ),
        const SizedBox(height: 24),
        Card(
          elevation: 0,
          child: Padding(
            padding: const EdgeInsets.all(18),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'sales_chart'.tr(),
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: 18),
                SizedBox(
                  height: 210,
                  child: _SalesChart(points: data.salesLast30Days),
                ),
              ],
            ),
          ),
        ).fadeSlideIn(delayMs: 280),
        const SizedBox(height: 20),
        _SectionTitle(title: 'recent_transactions'.tr()),
        ...data.recentTransactions.take(5).toList().asMap().entries.map(
              (entry) => Card(
                margin: const EdgeInsets.only(bottom: 10),
                child: ListTile(
                  contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                  leading: CircleAvatar(
                    backgroundColor: AppColors.accent.withValues(alpha: 0.15),
                    child: const Icon(Icons.receipt, color: AppColors.accent),
                  ),
                  title: Text('${entry.value.type} — ${entry.value.number}'),
                  subtitle: Text(
                    '${entry.value.party} • ${formatDate(entry.value.date)}',
                  ),
                  trailing: Text(formatCurrency(entry.value.amount)),
                ),
              ).fadeSlideInList(index: entry.key + 4),
            ),
        const SizedBox(height: 16),
        _SectionTitle(title: 'upcoming_installments'.tr()),
        ...data.upcomingInstallments.take(5).map(
              (i) => Card(
                margin: const EdgeInsets.only(bottom: 10),
                child: ListTile(
                  contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                  leading: CircleAvatar(
                    backgroundColor: AppColors.warning.withValues(alpha: 0.15),
                    child: const Icon(Icons.schedule, color: AppColors.warning),
                  ),
                  title: Text(i.customerName),
                  subtitle: Text(
                    '${formatDate(i.dueDate)} • ${i.daysRemaining} يوم',
                  ),
                  trailing: Text(formatCurrency(i.amount)),
                ),
              ),
            ),
        const SizedBox(height: 16),
        Row(
          children: [
            Expanded(
              child: KpiCard(
                title: 'bank_balance'.tr(),
                value: formatCurrency(data.bankBalance),
                icon: Icons.account_balance,
                compact: true,
              ),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: KpiCard(
                title: 'inventory_value'.tr(),
                value: formatCurrency(data.totalInventoryValue),
                icon: Icons.inventory_2_outlined,
                compact: true,
              ),
            ),
          ],
        ),
        if (data.cashBoxes.isNotEmpty) ...[
          const SizedBox(height: 20),
          _SectionTitle(title: 'cash_boxes'.tr()),
          ...data.cashBoxes.map(
            (c) => Card(
              margin: const EdgeInsets.only(bottom: 10),
              child: ListTile(
                title: Text(c.name),
                trailing: Text(formatCurrency(c.balance)),
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
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(18, 20, 18, 22),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(24),
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: isDark
              ? [
                  AppColors.primary.withValues(alpha: 0.55),
                  AppColors.accent.withValues(alpha: 0.25),
                ]
              : [
                  AppColors.primaryLight.withValues(alpha: 0.12),
                  AppColors.accent.withValues(alpha: 0.08),
                ],
        ),
        border: Border.all(
          color: isDark
              ? Colors.white.withValues(alpha: 0.1)
              : Colors.black.withValues(alpha: 0.05),
        ),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          const AppLogoMark(size: 56),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  '${'dashboard_greeting'.tr()} 👋',
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        fontSize: 14,
                      ),
                ),
                const SizedBox(height: 6),
                Text(
                  companyName,
                  style: Theme.of(context).textTheme.titleLarge?.copyWith(
                        fontSize: 22,
                        height: 1.2,
                      ),
                ),
                const SizedBox(height: 4),
                Text(
                  'dashboard_subtitle'.tr(),
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _QuickActionButton extends StatelessWidget {
  const _QuickActionButton({required this.onPressed});

  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Material(
      borderRadius: BorderRadius.circular(16),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onPressed,
        child: Ink(
          decoration: BoxDecoration(
            gradient: AppColors.primaryGradient,
            borderRadius: BorderRadius.circular(16),
          ),
          padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 14),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.receipt_long_rounded, color: Colors.white),
              const SizedBox(width: 10),
              Text(
                'new_invoice'.tr(),
                style: Theme.of(context).textTheme.labelLarge?.copyWith(
                      color: Colors.white,
                    ),
              ),
            ],
          ),
        ),
      ),
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
      child: Text(title, style: Theme.of(context).textTheme.titleMedium),
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
            color: Theme.of(context).dividerColor.withValues(alpha: 0.15),
            strokeWidth: 1,
          ),
        ),
        titlesData: FlTitlesData(
          topTitles: const AxisTitles(),
          rightTitles: const AxisTitles(),
          leftTitles: AxisTitles(
            sideTitles: SideTitles(
              showTitles: true,
              reservedSize: 46,
              getTitlesWidget: (value, _) => Text(
                value >= 1000 ? '${(value / 1000).toStringAsFixed(0)}k' : '${value.toInt()}',
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
            color: AppColors.accent,
            barWidth: 3,
            dotData: const FlDotData(show: false),
            belowBarData: BarAreaData(
              show: true,
              color: AppColors.accent.withValues(alpha: 0.12),
            ),
          ),
        ],
      ),
    );
  }
}
