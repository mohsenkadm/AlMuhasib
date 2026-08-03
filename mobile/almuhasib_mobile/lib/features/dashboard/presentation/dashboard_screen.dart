import 'package:easy_localization/easy_localization.dart';
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

  double get _totalCash =>
      data.cashBoxes.fold<double>(0, (sum, box) => sum + box.balance);

  double get _totalLiquidity => _totalCash + data.bankBalance;

  static const _chartPalette = [
    AppColors.primary,
    AppColors.accent,
    AppColors.moduleOrange,
    AppColors.moduleGreen,
    AppColors.modulePink,
    AppColors.moduleIndigo,
    AppColors.modulePurple,
    AppColors.warning,
  ];

  @override
  Widget build(BuildContext context) {
    final topPadding = MediaQuery.paddingOf(context).top;
    final expenseSections = data.expenseDistribution
        .where((e) => e.amount > 0)
        .toList()
        .asMap()
        .entries
        .map(
          (e) => (
            e.value.category,
            e.value.amount,
            _chartPalette[e.key % _chartPalette.length],
          ),
        )
        .toList();
    final expenseTotal =
        expenseSections.fold<double>(0, (s, e) => s + e.$2);

    final collectionPoints = <(String, double)>[
      if (data.customerCreditBalance > 0)
        ('customer_credit_balance'.tr(), data.customerCreditBalance),
      if (data.unpaidInstallmentsBalance > 0)
        ('unpaid_installments_balance'.tr(), data.unpaidInstallmentsBalance),
      if (data.investorBalance > 0)
        ('investor_balance'.tr(), data.investorBalance),
    ];

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: EdgeInsets.fromLTRB(20, topPadding + 8, 20, 120),
      children: [
        _DashboardHeader(companyName: companyName).fadeSlideIn(),
        const SizedBox(height: 18),
        AppBalanceHeroCard(
          title: 'total_liquidity'.tr(),
          value: formatCurrency(_totalLiquidity),
          subtitle:
              '${'cash_total'.tr()}: ${formatCurrency(_totalCash)}  ·  ${'bank_balance'.tr()}: ${formatCurrency(data.bankBalance)}',
          trendLabel:
              '${'inventory_value'.tr()} ${formatCurrency(data.totalInventoryValue)}',
          trendPositive: true,
        ).fadeSlideIn(delayMs: 40),
        const SizedBox(height: 16),
        _AlertsStrip(data: data).fadeSlideIn(delayMs: 80),
        const SizedBox(height: 18),
        _SectionHeader(
          title: 'dashboard_stats'.tr(),
          subtitle: 'dashboard_stats_hint'.tr(),
          actionLabel: 'view_all'.tr(),
          onAction: () => Get.toNamed(AppRoutes.data),
        ).fadeSlideIn(delayMs: 100),
        const SizedBox(height: 10),
        AppKpiGrid(
          childAspectRatio: 1.28,
          items: [
            AppKpiItem(
              title: 'today_sales'.tr(),
              value: formatCurrency(data.todaySales),
              icon: Icons.trending_up_rounded,
              color: AppColors.moduleGreen,
              compact: true,
            ),
            AppKpiItem(
              title: 'today_purchases'.tr(),
              value: formatCurrency(data.todayPurchases),
              icon: Icons.shopping_cart_outlined,
              color: AppColors.moduleOrange,
              compact: true,
            ),
            AppKpiItem(
              title: 'net_profit'.tr(),
              value: formatCurrency(data.netProfit),
              icon: Icons.account_balance_wallet_outlined,
              color: AppColors.primary,
              compact: true,
            ),
            AppKpiItem(
              title: 'overdue_installments'.tr(),
              value: '${data.overdueInstallmentsCount}',
              icon: Icons.warning_amber_rounded,
              color: AppColors.warning,
              compact: true,
              onTap: () => Get.toNamed(
                AppRoutes.installments,
                arguments: 'overdue',
              ),
            ),
            AppKpiItem(
              title: 'customer_credit_balance'.tr(),
              value: formatCurrency(data.customerCreditBalance),
              icon: Icons.people_outline_rounded,
              color: AppColors.moduleCyan,
              compact: true,
              onTap: () => Get.toNamed(
                AppRoutes.reportDetailPath('customers_overview'),
              ),
            ),
            AppKpiItem(
              title: 'unpaid_installments_balance'.tr(),
              value: formatCurrency(data.unpaidInstallmentsBalance),
              icon: Icons.event_note_outlined,
              color: AppColors.modulePink,
              compact: true,
              onTap: () => Get.toNamed(
                AppRoutes.installments,
                arguments: 'unpaid',
              ),
            ),
            AppKpiItem(
              title: 'investor_balance'.tr(),
              value: formatCurrency(data.investorBalance),
              icon: Icons.savings_outlined,
              color: AppColors.moduleIndigo,
              compact: true,
              onTap: () => Get.toNamed(
                AppRoutes.reportDetailPath('investor_statement'),
              ),
            ),
            AppKpiItem(
              title: 'cash_total'.tr(),
              value: formatCurrency(_totalCash),
              icon: Icons.payments_outlined,
              color: AppColors.accent,
              compact: true,
            ),
            AppKpiItem(
              title: 'inventory_value'.tr(),
              value: formatCurrency(data.totalInventoryValue),
              icon: Icons.warehouse_outlined,
              color: AppColors.moduleGreen,
              compact: true,
            ),
            AppKpiItem(
              title: 'bank_balance'.tr(),
              value: formatCurrency(data.bankBalance),
              icon: Icons.account_balance_outlined,
              color: AppColors.moduleIndigo,
              compact: true,
            ),
          ],
        ).fadeSlideIn(delayMs: 120),
        const SizedBox(height: 22),
        _SectionHeader(
          title: 'sales_chart'.tr(),
          subtitle: 'sales_chart_hint'.tr(),
        ).fadeSlideIn(delayMs: 140),
        const SizedBox(height: 10),
        AppChartCard(
          title: 'sales_last_30_days'.tr(),
          height: 200,
          child: AppLineChart(
            values: data.salesLast30Days.map((e) => e.amount).toList(),
            labels: data.salesLast30Days
                .map((e) => shortDateFormat.format(e.date))
                .toList(),
            color: AppColors.primary,
          ),
        ).fadeSlideIn(delayMs: 160),
        if (data.cashBoxes.isNotEmpty) ...[
          const SizedBox(height: 22),
          _SectionHeader(
            title: 'cash_boxes'.tr(),
            subtitle:
                '${'cash_total'.tr()}: ${formatCurrency(_totalCash)}',
          ).fadeSlideIn(delayMs: 170),
          const SizedBox(height: 10),
          AppChartCard(
            title: 'cash_distribution'.tr(),
            height: 180,
            child: AppDonutChart(
              sections: data.cashBoxes
                  .where((c) => c.balance > 0)
                  .toList()
                  .asMap()
                  .entries
                  .map(
                    (e) => (
                      e.value.name,
                      e.value.balance,
                      _chartPalette[e.key % _chartPalette.length],
                    ),
                  )
                  .toList(),
              centerLabel: 'cash_total'.tr(),
              centerValue: formatCurrency(_totalCash),
              valueAsCurrency: true,
            ),
          ).fadeSlideIn(delayMs: 180),
        ],
        if (expenseSections.isNotEmpty) ...[
          const SizedBox(height: 22),
          _SectionHeader(
            title: 'expense_distribution'.tr(),
            subtitle: 'expense_distribution_hint'.tr(),
          ).fadeSlideIn(delayMs: 180),
          const SizedBox(height: 10),
          AppChartCard(
            title: 'expense_by_category'.tr(),
            height: 180,
            child: AppDonutChart(
              sections: expenseSections,
              centerLabel: 'total'.tr(),
              centerValue: formatCurrency(expenseTotal),
              valueAsCurrency: true,
            ),
          ).fadeSlideIn(delayMs: 200),
        ],
        if (collectionPoints.isNotEmpty) ...[
          const SizedBox(height: 22),
          _SectionHeader(
            title: 'collection_summary'.tr(),
            subtitle: 'collection_summary_hint'.tr(),
          ).fadeSlideIn(delayMs: 210),
          const SizedBox(height: 10),
          AppChartCard(
            title: 'receivables_overview'.tr(),
            height: (collectionPoints.length * 52.0).clamp(100, 220),
            child: AppHorizontalBarChart(
              points: collectionPoints,
              color: AppColors.moduleCyan,
              valueAsCurrency: true,
            ),
          ).fadeSlideIn(delayMs: 220),
        ],
        const SizedBox(height: 22),
        _SectionHeader(
          title: 'recent_transactions'.tr(),
          subtitle: 'recent_transactions_hint'.tr(),
        ),
        const SizedBox(height: 8),
        if (data.recentTransactions.isEmpty)
          AppEntityCard(
            title: 'no_data'.tr(),
            leading: const _IconBadge(
              icon: Icons.receipt_long_outlined,
              color: AppColors.primary,
            ),
          )
        else
          ...data.recentTransactions.take(5).toList().asMap().entries.map(
            (entry) {
              final tx = entry.value;
              final positive = tx.amount >= 0;
              return Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: AppEntityCard(
                  title: '${tx.type} — ${tx.number}',
                  subtitle: '${tx.party} • ${formatDate(tx.date)}',
                  leading: _IconBadge(
                    icon: positive
                        ? Icons.arrow_downward_rounded
                        : Icons.arrow_upward_rounded,
                    color: positive ? AppColors.success : AppColors.error,
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
        const SizedBox(height: 14),
        _SectionHeader(
          title: 'upcoming_installments'.tr(),
          subtitle: 'upcoming_installments_hint'.tr(),
          actionLabel: 'view_all'.tr(),
          onAction: () => Get.toNamed(AppRoutes.installments),
        ),
        const SizedBox(height: 8),
        if (data.upcomingInstallments.isEmpty)
          AppEntityCard(
            title: 'no_data'.tr(),
            leading: const _IconBadge(
              icon: Icons.schedule_rounded,
              color: AppColors.warning,
            ),
          )
        else
          ...data.upcomingInstallments.take(5).toList().asMap().entries.map(
            (entry) {
              final i = entry.value;
              return Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: AppEntityCard(
                  title: i.customerName,
                  subtitle:
                      '${formatDate(i.dueDate)} • ${i.daysRemaining} ${'days'.tr()}',
                  leading: const _IconBadge(
                    icon: Icons.schedule_rounded,
                    color: AppColors.warning,
                  ),
                  trailing: Text(
                    formatCurrency(i.amount),
                    style: const TextStyle(fontWeight: FontWeight.w800),
                  ),
                  onTap: () => Get.toNamed(
                    AppRoutes.installments,
                    arguments: 'upcoming',
                  ),
                ).fadeSlideInList(index: entry.key),
              );
            },
          ),
        if (data.cashBoxes.isNotEmpty) ...[
          const SizedBox(height: 14),
          _SectionHeader(
            title: 'cash_box_balances'.tr(),
            subtitle:
                '${'cash_total'.tr()}: ${formatCurrency(_totalCash)}',
          ),
          const SizedBox(height: 8),
          ...data.cashBoxes.toList().asMap().entries.map(
            (entry) {
              final c = entry.value;
              return Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: AppEntityCard(
                  title: c.name,
                  leading: const _IconBadge(
                    icon: Icons.account_balance_wallet_outlined,
                    color: AppColors.moduleCyan,
                  ),
                  trailing: Text(
                    formatCurrency(c.balance),
                    style: const TextStyle(fontWeight: FontWeight.w800),
                  ),
                ).fadeSlideInList(index: entry.key),
              );
            },
          ),
        ],
      ],
    );
  }
}

class _AlertsStrip extends StatelessWidget {
  const _AlertsStrip({required this.data});

  final DashboardData data;

  @override
  Widget build(BuildContext context) {
    final alerts = <_AlertChipData>[
      if (data.overdueInstallmentsCount > 0)
        _AlertChipData(
          label:
              '${'overdue_installments'.tr()}: ${data.overdueInstallmentsCount}',
          color: AppColors.error,
          icon: Icons.warning_amber_rounded,
          onTap: () => Get.toNamed(
            AppRoutes.installments,
            arguments: 'overdue',
          ),
        ),
      if (data.customerCreditBalance > 0)
        _AlertChipData(
          label:
              '${'customer_credit_balance'.tr()}: ${formatCurrency(data.customerCreditBalance)}',
          color: AppColors.moduleCyan,
          icon: Icons.people_outline_rounded,
          onTap: () => Get.toNamed(
            AppRoutes.reportDetailPath('customers_overview'),
          ),
        ),
      if (data.unpaidInstallmentsBalance > 0)
        _AlertChipData(
          label:
              '${'unpaid_installments_balance'.tr()}: ${formatCurrency(data.unpaidInstallmentsBalance)}',
          color: AppColors.warning,
          icon: Icons.event_note_outlined,
          onTap: () => Get.toNamed(
            AppRoutes.installments,
            arguments: 'unpaid',
          ),
        ),
    ];

    if (alerts.isEmpty) {
      return AppEntityCard(
        title: 'monitoring_all_clear'.tr(),
        subtitle: 'monitoring_all_clear_hint'.tr(),
        leading: const _IconBadge(
          icon: Icons.verified_outlined,
          color: AppColors.success,
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _SectionHeader(
          title: 'monitoring_alerts'.tr(),
          subtitle: 'monitoring_alerts_hint'.tr(),
        ),
        const SizedBox(height: 10),
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(
            children: [
              for (final alert in alerts)
                Padding(
                  padding: const EdgeInsetsDirectional.only(end: 10),
                  child: Material(
                    color: alert.color.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(16),
                    child: InkWell(
                      onTap: alert.onTap,
                      borderRadius: BorderRadius.circular(16),
                      child: Padding(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 14,
                          vertical: 12,
                        ),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(alert.icon, color: alert.color, size: 20),
                            const SizedBox(width: 8),
                            Text(
                              alert.label,
                              style: TextStyle(
                                color: alert.color,
                                fontWeight: FontWeight.w700,
                                fontSize: 13,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
            ],
          ),
        ),
      ],
    );
  }
}

class _AlertChipData {
  const _AlertChipData({
    required this.label,
    required this.color,
    required this.icon,
    required this.onTap,
  });

  final String label;
  final Color color;
  final IconData icon;
  final VoidCallback onTap;
}

class _DashboardHeader extends StatelessWidget {
  const _DashboardHeader({required this.companyName});

  final String companyName;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final buttonBg = isDark ? AppColors.surfaceDarkCard : Colors.white;

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
              Text(
                'dashboard_monitor_subtitle'.tr(),
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: Theme.of(context).colorScheme.onSurfaceVariant,
                    ),
              ),
            ],
          ),
        ),
        _RoundAction(
          bg: buttonBg,
          onPressed: () => Get.toNamed(AppRoutes.invoiceNew),
          icon: Icons.add_rounded,
          tooltip: 'new_invoice'.tr(),
        ),
        const SizedBox(width: 10),
        Container(
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            color: buttonBg,
            shape: BoxShape.circle,
            boxShadow: AppColors.cardShadow(dark: isDark),
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

class _RoundAction extends StatelessWidget {
  const _RoundAction({
    required this.bg,
    required this.onPressed,
    required this.icon,
    this.tooltip,
  });

  final Color bg;
  final VoidCallback onPressed;
  final IconData icon;
  final String? tooltip;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 44,
      height: 44,
      decoration: BoxDecoration(
        color: bg,
        shape: BoxShape.circle,
        boxShadow: AppColors.cardShadow(
          dark: Theme.of(context).brightness == Brightness.dark,
        ),
      ),
      child: IconButton(
        onPressed: onPressed,
        tooltip: tooltip,
        icon: Icon(icon),
        color: AppColors.primary,
      ),
    );
  }
}

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({
    required this.title,
    this.subtitle,
    this.actionLabel,
    this.onAction,
  });

  final String title;
  final String? subtitle;
  final String? actionLabel;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
              ),
              if (subtitle != null) ...[
                const SizedBox(height: 2),
                Text(
                  subtitle!,
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: Theme.of(context).colorScheme.onSurfaceVariant,
                      ),
                ),
              ],
            ],
          ),
        ),
        if (actionLabel != null && onAction != null)
          TextButton(onPressed: onAction, child: Text(actionLabel!)),
      ],
    );
  }
}

class _IconBadge extends StatelessWidget {
  const _IconBadge({required this.icon, required this.color});

  final IconData icon;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 46,
      height: 46,
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.14),
        shape: BoxShape.circle,
      ),
      child: Icon(icon, color: color, size: 22),
    );
  }
}
