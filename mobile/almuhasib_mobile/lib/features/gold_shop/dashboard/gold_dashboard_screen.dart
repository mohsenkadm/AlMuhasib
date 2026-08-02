import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/system_themes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/shimmer_widgets.dart';
import '../controllers/gold_dashboard_controller.dart';
import '../models/gold_shop_models.dart';
import '../widgets/gold_kpi_card.dart';
import '../widgets/gold_labels.dart';

class GoldDashboardScreen extends GetView<GoldDashboardController> {
  const GoldDashboardScreen({super.key});

  @override
  final String? tag = 'gold_dashboard';

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
                  child: _GoldDashboardBody(
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

class _GoldDashboardBody extends StatelessWidget {
  const _GoldDashboardBody({
    required this.data,
    required this.companyName,
  });

  final GoldDashboardDto data;
  final String companyName;

  @override
  Widget build(BuildContext context) {
    final topPadding = MediaQuery.paddingOf(context).top;
    final fx = data.latestUsdToIqd;

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: EdgeInsets.fromLTRB(20, topPadding + 8, 20, 120),
      children: [
        _Header(companyName: companyName).fadeSlideIn(),
        const SizedBox(height: 18),
        GoldHeroBanner(
          title: 'gold_kpi_today_sales'.tr(),
          value: '${formatCurrency(data.todaySalesIqd)} د.ع',
          subtitle: fx != null
              ? '${'gold_fx_rate'.tr()}: ${formatCurrency(fx)}'
              : '${formatCurrency(data.todaySalesUsd)} \$',
          trailing: Icon(
            Icons.diamond_rounded,
            size: 40,
            color: Colors.white.withValues(alpha: 0.35),
          ),
        ).fadeSlideIn(delayMs: 40),
        const SizedBox(height: 20),
        GoldSectionHeader(title: 'dashboard_stats'.tr())
            .fadeSlideIn(delayMs: 80),
        const SizedBox(height: 10),
        SizedBox(
          height: 148,
          child: Row(
            children: [
              Expanded(
                child: GoldKpiCard(
                  title: 'gold_kpi_cash'.tr(),
                  value: formatCurrency(data.cashBalanceIqd),
                  subtitle: '${formatCurrency(data.cashBalanceUsd)} \$',
                  icon: Icons.account_balance_wallet_outlined,
                  color: SystemThemes.goldPrimary,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: GoldKpiCard(
                  title: 'gold_kpi_stock'.tr(),
                  value: '${formatCurrency(data.totalStockGrams)} غ',
                  subtitle: formatCurrency(data.totalStockValueIqd),
                  icon: Icons.inventory_2_outlined,
                  color: const Color(0xFF6D4C41),
                ),
              ),
            ],
          ),
        ).fadeSlideIn(delayMs: 100),
        const SizedBox(height: 12),
        SizedBox(
          height: 148,
          child: Row(
            children: [
              Expanded(
                child: GoldKpiCard(
                  title: 'gold_kpi_credit'.tr(),
                  value: '${data.openCreditCount}',
                  subtitle: formatCurrency(data.openCreditIqd),
                  icon: Icons.credit_card_outlined,
                  color: const Color(0xFFE65100),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: GoldKpiCard(
                  title: 'gold_kpi_purchases'.tr(),
                  value: formatCurrency(data.todayPurchasesIqd),
                  subtitle: '${formatCurrency(data.todayPurchasesUsd)} \$',
                  icon: Icons.shopping_bag_outlined,
                  color: const Color(0xFF00838F),
                ),
              ),
            ],
          ),
        ).fadeSlideIn(delayMs: 120),
        if (data.alerts.isNotEmpty) ...[
          const SizedBox(height: 22),
          GoldSectionHeader(
            title: 'gold_alerts'.tr(),
            actionLabel: 'view_all'.tr(),
            onAction: () => Get.toNamed(AppRoutes.goldShopNotifications),
          ).fadeSlideIn(delayMs: 140),
          const SizedBox(height: 8),
          ...data.alerts.take(4).map(
            (a) => Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: _AlertTile(alert: a),
            ),
          ),
        ],
        if (data.latestPrices.isNotEmpty) ...[
          const SizedBox(height: 18),
          GoldSectionHeader(
            title: 'gold_latest_prices'.tr(),
            actionLabel: 'view_all'.tr(),
            onAction: () => Get.toNamed(AppRoutes.goldShopPrices),
          ).fadeSlideIn(delayMs: 160),
          const SizedBox(height: 8),
          ...data.latestPrices.take(5).map(
            (p) => Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: _PriceTile(price: p),
            ),
          ),
        ],
        if (data.stockByKarat.isNotEmpty) ...[
          const SizedBox(height: 18),
          GoldSectionHeader(title: 'gold_stock_by_karat'.tr()),
          const SizedBox(height: 8),
          AppChartCard(
            title: 'gold_stock_by_karat'.tr(),
            height: (data.stockByKarat.length * 42.0).clamp(120, 240),
            child: AppHorizontalBarChart(
              color: SystemThemes.goldPrimary,
              points: [
                for (final s in data.stockByKarat.take(8))
                  (
                    goldKaratLabel(s.karatValue, karatName: s.karatName),
                    s.gramsOnHand,
                  ),
              ],
            ),
          ),
        ],
        const SizedBox(height: 22),
        GoldSectionHeader(
          title: 'gold_recent_sales'.tr(),
          actionLabel: 'view_all'.tr(),
          onAction: () => Get.toNamed(AppRoutes.goldShopSales),
        ),
        const SizedBox(height: 8),
        if (data.recentInvoices.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 24),
            child: Center(child: Text('gold_no_sales'.tr())),
          )
        else
          ...data.recentInvoices.take(6).map(
            (inv) => Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: AppEntityCard(
                title: inv.invoiceNumber,
                subtitle:
                    '${inv.customerName.isEmpty ? '—' : inv.customerName} • ${formatDate(inv.invoiceDate)}',
                leading: Container(
                  width: 46,
                  height: 46,
                  decoration: BoxDecoration(
                    color: SystemThemes.goldPrimary.withValues(alpha: 0.14),
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Icons.receipt_long_outlined,
                    color: SystemThemes.goldPrimary,
                  ),
                ),
                trailing: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      formatCurrency(inv.totalAmountIqd),
                      style: const TextStyle(fontWeight: FontWeight.w800),
                    ),
                    Text(
                      goldInvoiceStatusLabel(inv.status),
                      style: TextStyle(
                        color: goldInvoiceStatusColor(inv.status),
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ),
                onTap: () =>
                    Get.toNamed(AppRoutes.goldShopSaleDetailPath(inv.id)),
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
        Container(
          width: 48,
          height: 48,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            gradient: const LinearGradient(
              colors: [
                SystemThemes.goldPrimary,
                SystemThemes.goldSecondary,
              ],
            ),
            boxShadow: [
              BoxShadow(
                color: SystemThemes.goldPrimary.withValues(alpha: 0.3),
                blurRadius: 12,
                offset: const Offset(0, 4),
              ),
            ],
          ),
          child: const Icon(Icons.diamond_rounded, color: Colors.white),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'gold_dashboard_title'.tr(),
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
        _CircleIconButton(
          icon: Icons.notifications_outlined,
          onPressed: () => Get.toNamed(AppRoutes.goldShopNotifications),
        ),
        const SizedBox(width: 8),
        _CircleIconButton(
          icon: Icons.person_outline_rounded,
          onPressed: () => Get.toNamed(AppRoutes.profile),
        ),
      ],
    );
  }
}

class _CircleIconButton extends StatelessWidget {
  const _CircleIconButton({required this.icon, required this.onPressed});

  final IconData icon;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Container(
      width: 44,
      height: 44,
      decoration: BoxDecoration(
        color: isDark ? SystemThemes.goldDarkCard : Colors.white,
        shape: BoxShape.circle,
        boxShadow: [
          BoxShadow(
            color: SystemThemes.goldPrimary.withValues(alpha: 0.12),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: IconButton(
        onPressed: onPressed,
        icon: Icon(icon),
        color: SystemThemes.goldPrimary,
      ),
    );
  }
}

class _AlertTile extends StatelessWidget {
  const _AlertTile({required this.alert});

  final GoldAlertItem alert;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(16),
        color: Theme.of(context).brightness == Brightness.dark
            ? SystemThemes.goldDarkCard
            : const Color(0xFFFFF8E7),
        border: Border.all(
          color: SystemThemes.goldPrimary.withValues(alpha: 0.2),
        ),
      ),
      child: Row(
        children: [
          Icon(
            Icons.warning_amber_rounded,
            color: SystemThemes.goldPrimary,
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  alert.title,
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
                Text(
                  alert.message,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _PriceTile extends StatelessWidget {
  const _PriceTile({required this.price});

  final GoldMithqalPriceRow price;

  @override
  Widget build(BuildContext context) {
    return AppEntityCard(
      title: goldKaratLabel(price.karatValue, karatName: price.karatName),
      subtitle: formatDate(price.priceDate),
      leading: Container(
        width: 46,
        height: 46,
        decoration: BoxDecoration(
          color: SystemThemes.goldSecondary.withValues(alpha: 0.2),
          shape: BoxShape.circle,
        ),
        child: const Icon(
          Icons.monetization_on_outlined,
          color: SystemThemes.goldPrimary,
        ),
      ),
      trailing: Text(
        '${formatCurrency(price.pricePerMithqal)} ${goldCurrencyLabel(price.currency)}',
        style: const TextStyle(fontWeight: FontWeight.w800),
      ),
      onTap: () => Get.toNamed(AppRoutes.goldShopPrices),
    );
  }
}
