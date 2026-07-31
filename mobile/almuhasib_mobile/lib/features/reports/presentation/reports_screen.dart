import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/design_system/design_system.dart';

/// مجموعات مطابقة لمنيو سطح المكتب
enum ReportGroup {
  all,
  salesProfit,
  installments,
  partners,
  inventoryFinance,
  financial,
  supervisory,
}

class ReportsScreen extends StatefulWidget {
  const ReportsScreen({super.key});

  @override
  State<ReportsScreen> createState() => _ReportsScreenState();
}

class _ReportsScreenState extends State<ReportsScreen> {
  final _search = TextEditingController();
  late Set<String> _favorites;
  ReportGroup _group = ReportGroup.all;

  static const _reports = <_ReportItem>[
    // ── المبيعات والأرباح ──
    _ReportItem('sales', 'report_sales', Icons.point_of_sale_rounded, AppColors.moduleGreen, ReportGroup.salesProfit),
    _ReportItem('purchases', 'report_purchases', Icons.shopping_bag_outlined, AppColors.primary, ReportGroup.salesProfit),
    _ReportItem('profit', 'report_profit', Icons.trending_up_rounded, AppColors.modulePurple, ReportGroup.salesProfit),
    _ReportItem('profit_comparison', 'report_profit_comparison', Icons.compare_arrows_rounded, AppColors.modulePink, ReportGroup.salesProfit),
    _ReportItem('top_products', 'report_top_products', Icons.star_outline_rounded, AppColors.warning, ReportGroup.salesProfit),
    _ReportItem('product_margin', 'report_product_margin', Icons.percent_rounded, AppColors.moduleGreen, ReportGroup.salesProfit),
    _ReportItem('sales_by_payment_method', 'report_sales_by_payment_method', Icons.payments_outlined, AppColors.moduleCyan, ReportGroup.salesProfit),
    _ReportItem('daily_sales', 'report_daily_sales', Icons.calendar_today_rounded, AppColors.moduleIndigo, ReportGroup.salesProfit),
    _ReportItem('sales_by_warehouse_user', 'report_sales_by_warehouse_user', Icons.store_mall_directory_outlined, AppColors.moduleOrange, ReportGroup.salesProfit),
    _ReportItem('gross_profit_margin', 'report_gross_profit_margin', Icons.pie_chart_outline_rounded, AppColors.moduleGreen, ReportGroup.salesProfit),
    _ReportItem('operating_profit', 'report_operating_profit', Icons.show_chart_rounded, AppColors.modulePurple, ReportGroup.salesProfit),

    // ── الأقساط ──
    _ReportItem('installments_aging', 'report_installments_aging', Icons.timelapse_rounded, AppColors.error, ReportGroup.installments),
    _ReportItem('installments_summary', 'report_installments_summary', Icons.summarize_outlined, AppColors.warning, ReportGroup.installments),
    _ReportItem('installments_detail', 'report_installments_detail', Icons.list_alt_rounded, AppColors.warning, ReportGroup.installments),
    _ReportItem('installments_paid', 'report_installments_paid', Icons.check_circle_outline, AppColors.moduleGreen, ReportGroup.installments),
    _ReportItem('installments_unpaid', 'report_installments_unpaid', Icons.pending_outlined, AppColors.moduleOrange, ReportGroup.installments),
    _ReportItem('overdue', 'report_overdue', Icons.warning_amber_rounded, AppColors.warning, ReportGroup.installments),
    _ReportItem('opening_installment_balances', 'report_opening_installment_balances', Icons.book_outlined, AppColors.moduleIndigo, ReportGroup.installments),
    _ReportItem('company_fees', 'report_company_fees', Icons.percent_rounded, AppColors.modulePurple, ReportGroup.installments),
    _ReportItem('installment_schedule', 'report_installment_schedule', Icons.event_note_rounded, AppColors.moduleCyan, ReportGroup.installments),

    // ── العملاء والموردين ──
    _ReportItem('customers_overview', 'report_customers_overview', Icons.people_outline_rounded, AppColors.primary, ReportGroup.partners),
    _ReportItem('suppliers_overview', 'report_suppliers_overview', Icons.storefront_outlined, AppColors.modulePurple, ReportGroup.partners),
    _ReportItem('statement', 'report_statement', Icons.receipt_long_rounded, AppColors.moduleCyan, ReportGroup.partners),
    _ReportItem('supplier_statement', 'report_supplier_statement', Icons.local_shipping_outlined, AppColors.modulePurple, ReportGroup.partners),
    _ReportItem('receivables_aging', 'report_receivables_aging', Icons.account_balance_wallet_outlined, AppColors.error, ReportGroup.partners),
    _ReportItem('payables_aging', 'report_payables_aging', Icons.account_balance_outlined, AppColors.moduleOrange, ReportGroup.partners),
    _ReportItem('customer_collections', 'report_customer_collections', Icons.payments_rounded, AppColors.moduleGreen, ReportGroup.partners),
    _ReportItem('overdue_customers', 'report_overdue_customers', Icons.person_off_outlined, AppColors.error, ReportGroup.partners),
    _ReportItem('supplier_payments', 'report_supplier_payments', Icons.money_off_csred_outlined, AppColors.modulePink, ReportGroup.partners),

    // ── المخزون والمالية ──
    _ReportItem('warehouse', 'report_warehouse', Icons.warehouse_outlined, AppColors.moduleOrange, ReportGroup.inventoryFinance),
    _ReportItem('product_movement', 'report_product_movement', Icons.sync_alt_rounded, AppColors.moduleIndigo, ReportGroup.inventoryFinance),
    _ReportItem('stock_health', 'report_stock_health', Icons.health_and_safety_outlined, AppColors.moduleCyan, ReportGroup.inventoryFinance),
    _ReportItem('inventory_replenishment', 'report_inventory_replenishment', Icons.inventory_outlined, AppColors.moduleOrange, ReportGroup.inventoryFinance),
    _ReportItem('expenses', 'report_expenses', Icons.money_off_outlined, AppColors.error, ReportGroup.inventoryFinance),
    _ReportItem('cash_flow', 'report_cash_flow', Icons.waterfall_chart_rounded, AppColors.moduleCyan, ReportGroup.inventoryFinance),
    _ReportItem('investor_statement', 'report_investor_statement', Icons.savings_outlined, AppColors.modulePink, ReportGroup.inventoryFinance),
    _ReportItem('bank_account_statement', 'report_bank_account_statement', Icons.account_balance_rounded, AppColors.primary, ReportGroup.inventoryFinance),
    _ReportItem('cash_box_movement', 'report_cash_box_movement', Icons.point_of_sale_outlined, AppColors.moduleGreen, ReportGroup.inventoryFinance),
    _ReportItem('cash_balances_summary', 'report_cash_balances_summary', Icons.account_balance_wallet_rounded, AppColors.moduleIndigo, ReportGroup.inventoryFinance),
    _ReportItem('transfers', 'report_transfers', Icons.swap_horiz_rounded, AppColors.modulePurple, ReportGroup.inventoryFinance),
    _ReportItem('inventory_valuation', 'report_inventory_valuation', Icons.attach_money_rounded, AppColors.moduleGreen, ReportGroup.inventoryFinance),
    _ReportItem('stock_taking', 'report_stock_taking', Icons.checklist_rtl_rounded, AppColors.moduleOrange, ReportGroup.inventoryFinance),
    _ReportItem('cogs', 'report_cogs', Icons.shopping_cart_checkout_rounded, AppColors.error, ReportGroup.inventoryFinance),

    // ── التقارير المالية ──
    _ReportItem('balance_sheet', 'report_balance_sheet', Icons.balance_rounded, AppColors.moduleIndigo, ReportGroup.financial),
    _ReportItem('income_expense', 'report_income_expense', Icons.swap_vert_rounded, AppColors.moduleOrange, ReportGroup.financial),
    _ReportItem('financial_position_summary', 'report_financial_position_summary', Icons.dashboard_customize_outlined, AppColors.moduleCyan, ReportGroup.financial),
    _ReportItem('profit_and_loss', 'report_profit_and_loss', Icons.assessment_outlined, AppColors.modulePurple, ReportGroup.financial),
    _ReportItem('statement_of_financial_position', 'report_statement_of_financial_position', Icons.account_balance_rounded, AppColors.primary, ReportGroup.financial),

    // ── التقارير الرقابية ──
    _ReportItem('investor_profit_distributions', 'report_investor_profit_distributions', Icons.diversity_3_rounded, AppColors.modulePink, ReportGroup.supervisory),
    _ReportItem('capital_movement', 'report_capital_movement', Icons.account_balance_outlined, AppColors.moduleIndigo, ReportGroup.supervisory),
  ];

  static const _groupMeta = <ReportGroup, ({String titleKey, String tipKey, IconData icon, Color color})>{
    ReportGroup.salesProfit: (
      titleKey: 'report_group_sales_profit',
      tipKey: 'report_tip_sales_profit',
      icon: Icons.trending_up_rounded,
      color: AppColors.moduleGreen,
    ),
    ReportGroup.installments: (
      titleKey: 'report_group_installments',
      tipKey: 'report_tip_installments',
      icon: Icons.calendar_month_rounded,
      color: AppColors.warning,
    ),
    ReportGroup.partners: (
      titleKey: 'report_group_partners',
      tipKey: 'report_tip_partners',
      icon: Icons.groups_rounded,
      color: AppColors.primary,
    ),
    ReportGroup.inventoryFinance: (
      titleKey: 'report_group_inventory_finance',
      tipKey: 'report_tip_inventory_finance',
      icon: Icons.account_balance_wallet_rounded,
      color: AppColors.moduleOrange,
    ),
    ReportGroup.financial: (
      titleKey: 'report_group_financial',
      tipKey: 'report_tip_financial',
      icon: Icons.account_balance_rounded,
      color: AppColors.moduleIndigo,
    ),
    ReportGroup.supervisory: (
      titleKey: 'report_group_supervisory',
      tipKey: 'report_tip_supervisory',
      icon: Icons.shield_outlined,
      color: AppColors.error,
    ),
  };

  @override
  void initState() {
    super.initState();
    _favorites = AppServices.prefs.reportFavorites.toSet();
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Future<void> _persistFavorites() async {
    await AppServices.prefs.setReportFavorites(_favorites.toList());
  }

  Future<void> _toggleFavorite(String type) async {
    setState(() {
      if (_favorites.contains(type)) {
        _favorites.remove(type);
      } else {
        _favorites.add(type);
      }
    });
    await _persistFavorites();
  }

  String _groupLabel(ReportGroup g) => switch (g) {
        ReportGroup.all => 'report_category_all'.tr(),
        ReportGroup.salesProfit => 'report_group_sales_profit'.tr(),
        ReportGroup.installments => 'report_group_installments'.tr(),
        ReportGroup.partners => 'report_group_partners'.tr(),
        ReportGroup.inventoryFinance => 'report_group_inventory_finance'.tr(),
        ReportGroup.financial => 'report_group_financial'.tr(),
        ReportGroup.supervisory => 'report_group_supervisory'.tr(),
      };

  List<_ReportItem> get _filtered {
    final query = _search.text.trim().toLowerCase();
    return _reports.where((r) {
      if (_group != ReportGroup.all && r.group != _group) return false;
      if (query.isEmpty) return true;
      return r.titleKey.tr().toLowerCase().contains(query) ||
          r.type.contains(query);
    }).toList();
  }

  @override
  Widget build(BuildContext context) {
    final filtered = _filtered;
    final favorites =
        filtered.where((r) => _favorites.contains(r.type)).toList();
    final isDark = Theme.of(context).brightness == Brightness.dark;

    final groupsToShow = _group == ReportGroup.all
        ? ReportGroup.values.where((g) => g != ReportGroup.all).toList()
        : [_group];

    return AppPageScaffold(
      title: 'reports_title'.tr(),
      subtitle: 'reports_subtitle'.tr(),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 120),
        children: [
          TextField(
            controller: _search,
            onChanged: (_) => setState(() {}),
            decoration: InputDecoration(
              hintText: 'search_hint'.tr(),
              prefixIcon: const Icon(Icons.search_rounded),
              filled: true,
              fillColor: isDark ? AppColors.surfaceDarkCard : Colors.white,
            ),
          ).fadeSlideIn(),
          const SizedBox(height: 14),
          SizedBox(
            height: 42,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: ReportGroup.values.length,
              separatorBuilder: (_, __) => const SizedBox(width: 8),
              itemBuilder: (context, index) {
                final g = ReportGroup.values[index];
                final selected = _group == g;
                return ChoiceChip(
                  label: Text(_groupLabel(g)),
                  selected: selected,
                  onSelected: (_) => setState(() => _group = g),
                  selectedColor: AppColors.primary.withValues(alpha: 0.18),
                  labelStyle: TextStyle(
                    fontWeight: FontWeight.w700,
                    color: selected ? AppColors.primary : null,
                    fontSize: 12,
                  ),
                );
              },
            ),
          ).fadeSlideIn(delayMs: 40),
          const SizedBox(height: 18),

          // Favorites
          Text(
            'favorite_reports'.tr(),
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
          ).fadeSlideIn(delayMs: 60),
          const SizedBox(height: 10),
          if (favorites.isEmpty)
            Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: Text(
                'favorite_reports_hint'.tr(),
                style: Theme.of(context).textTheme.bodyMedium,
              ),
            )
          else
            ...favorites.asMap().entries.map(
                  (entry) => Padding(
                    padding: const EdgeInsets.only(bottom: 10),
                    child: _ReportListCard(
                      item: entry.value,
                      favorited: true,
                      onFavorite: () => _toggleFavorite(entry.value.type),
                      onTap: () => Get.toNamed(
                        AppRoutes.reportDetailPath(entry.value.type),
                      ),
                    ).fadeSlideInList(index: entry.key),
                  ),
                ),

          const SizedBox(height: 8),

          // Grouped sections
          ...groupsToShow.asMap().entries.expand((entry) {
            final gi = entry.key;
            final g = entry.value;
            final meta = _groupMeta[g]!;
            final items = filtered.where((r) => r.group == g).toList();
            if (items.isEmpty) return <Widget>[];

            return [
              const SizedBox(height: 14),
              _GroupHeader(
                title: meta.titleKey.tr(),
                tip: meta.tipKey.tr(),
                icon: meta.icon,
                color: meta.color,
                count: items.length,
              ).fadeSlideIn(delayMs: 80 + gi * 30),
              const SizedBox(height: 10),
              ...items.asMap().entries.map(
                    (e) => Padding(
                      padding: const EdgeInsets.only(bottom: 10),
                      child: _ReportListCard(
                        item: e.value,
                        favorited: _favorites.contains(e.value.type),
                        onFavorite: () => _toggleFavorite(e.value.type),
                        onTap: () => Get.toNamed(
                          AppRoutes.reportDetailPath(e.value.type),
                        ),
                      ).fadeSlideInList(index: e.key + gi * 3),
                    ),
                  ),
            ];
          }),

          if (filtered.isEmpty)
            Padding(
              padding: const EdgeInsets.only(top: 40),
              child: Center(
                child: Text(
                  'no_data'.tr(),
                  style: Theme.of(context).textTheme.bodyLarge,
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class _GroupHeader extends StatelessWidget {
  const _GroupHeader({
    required this.title,
    required this.tip,
    required this.icon,
    required this.color,
    required this.count,
  });

  final String title;
  final String tip;
  final IconData icon;
  final Color color;
  final int count;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [
            color.withValues(alpha: isDark ? 0.22 : 0.12),
            color.withValues(alpha: isDark ? 0.08 : 0.04),
          ],
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
        ),
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: color.withValues(alpha: 0.22)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.18),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(icon, color: color, size: 22),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  title,
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w900,
                      ),
                ),
              ),
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.16),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  '$count',
                  style: TextStyle(
                    color: color,
                    fontWeight: FontWeight.w800,
                    fontSize: 12,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(Icons.lightbulb_outline_rounded,
                  size: 16, color: color.withValues(alpha: 0.9)),
              const SizedBox(width: 6),
              Expanded(
                child: Text(
                  tip,
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        height: 1.35,
                        color: Theme.of(context)
                            .textTheme
                            .bodySmall
                            ?.color
                            ?.withValues(alpha: 0.85),
                      ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _ReportListCard extends StatelessWidget {
  const _ReportListCard({
    required this.item,
    required this.favorited,
    required this.onFavorite,
    required this.onTap,
  });

  final _ReportItem item;
  final bool favorited;
  final VoidCallback onFavorite;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return AppEntityCard(
      title: item.titleKey.tr(),
      subtitle: item.infoKey.tr(),
      leading: Container(
        width: 48,
        height: 48,
        decoration: BoxDecoration(
          color: item.color.withValues(alpha: 0.14),
          borderRadius: BorderRadius.circular(14),
        ),
        child: Icon(item.icon, color: item.color),
      ),
      trailing: IconButton(
        onPressed: onFavorite,
        icon: Icon(
          favorited ? Icons.star_rounded : Icons.star_outline_rounded,
          color: favorited ? AppColors.warning : AppColors.textDarkMuted,
        ),
      ),
      onTap: onTap,
    );
  }
}

class _ReportItem {
  const _ReportItem(
    this.type,
    this.titleKey,
    this.icon,
    this.color,
    this.group,
  );

  final String type;
  final String titleKey;
  final IconData icon;
  final Color color;
  final ReportGroup group;

  String get infoKey => 'report_info_$type';
}
