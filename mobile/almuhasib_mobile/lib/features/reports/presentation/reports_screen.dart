import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/design_system/design_system.dart';

enum _ReportCategory { all, sales, inventory, finance }

class ReportsScreen extends StatefulWidget {
  const ReportsScreen({super.key});

  @override
  State<ReportsScreen> createState() => _ReportsScreenState();
}

class _ReportsScreenState extends State<ReportsScreen> {
  final _search = TextEditingController();
  late Set<String> _favorites;
  _ReportCategory _category = _ReportCategory.all;

  static const _reports = [
    _ReportItem(
      'sales',
      'report_sales',
      Icons.point_of_sale_rounded,
      AppColors.moduleGreen,
      _ReportCategory.sales,
    ),
    _ReportItem(
      'purchases',
      'report_purchases',
      Icons.shopping_bag_outlined,
      AppColors.primary,
      _ReportCategory.sales,
    ),
    _ReportItem(
      'profit',
      'report_profit',
      Icons.trending_up_rounded,
      AppColors.modulePurple,
      _ReportCategory.finance,
    ),
    _ReportItem(
      'balance_sheet',
      'report_balance_sheet',
      Icons.account_balance_rounded,
      AppColors.moduleIndigo,
      _ReportCategory.finance,
    ),
    _ReportItem(
      'overdue',
      'report_overdue',
      Icons.warning_amber_rounded,
      AppColors.warning,
      _ReportCategory.finance,
    ),
    _ReportItem(
      'statement',
      'report_statement',
      Icons.receipt_long_rounded,
      AppColors.moduleCyan,
      _ReportCategory.finance,
    ),
    _ReportItem(
      'investor_statement',
      'report_investor_statement',
      Icons.savings_outlined,
      AppColors.modulePink,
      _ReportCategory.finance,
    ),
    _ReportItem(
      'warehouse',
      'report_warehouse',
      Icons.warehouse_outlined,
      AppColors.moduleOrange,
      _ReportCategory.inventory,
    ),
    _ReportItem(
      'top_products',
      'report_top_products',
      Icons.star_outline_rounded,
      AppColors.warning,
      _ReportCategory.inventory,
    ),
  ];

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

  Future<void> _openFilterSheet() async {
    final selected = await showModalBottomSheet<_ReportCategory>(
      context: context,
      showDragHandle: true,
      builder: (ctx) {
        return SafeArea(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                child: Text(
                  'filter_reports'.tr(),
                  style: Theme.of(ctx).textTheme.titleLarge,
                ),
              ),
              for (final cat in _ReportCategory.values)
                ListTile(
                  leading: Icon(
                    cat == _category
                        ? Icons.radio_button_checked
                        : Icons.radio_button_off,
                    color: AppColors.primary,
                  ),
                  title: Text(_categoryLabel(cat)),
                  onTap: () => Navigator.pop(ctx, cat),
                ),
            ],
          ),
        );
      },
    );
    if (selected != null) setState(() => _category = selected);
  }

  String _categoryLabel(_ReportCategory cat) => switch (cat) {
        _ReportCategory.all => 'report_category_all'.tr(),
        _ReportCategory.sales => 'report_category_sales'.tr(),
        _ReportCategory.inventory => 'report_category_inventory'.tr(),
        _ReportCategory.finance => 'report_category_finance'.tr(),
      };

  @override
  Widget build(BuildContext context) {
    final query = _search.text.trim().toLowerCase();
    final filtered = _reports.where((r) {
      if (_category != _ReportCategory.all && r.category != _category) {
        return false;
      }
      if (query.isEmpty) return true;
      return r.titleKey.tr().toLowerCase().contains(query) ||
          r.type.contains(query);
    }).toList();

    final favorites =
        filtered.where((r) => _favorites.contains(r.type)).toList();
    final others =
        filtered.where((r) => !_favorites.contains(r.type)).toList();

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
              suffixIcon: IconButton(
                onPressed: _openFilterSheet,
                icon: Badge(
                  isLabelVisible: _category != _ReportCategory.all,
                  child: const Icon(Icons.tune_rounded),
                ),
              ),
              filled: true,
              fillColor: Theme.of(context).brightness == Brightness.dark
                  ? AppColors.surfaceDarkCard
                  : Colors.white,
            ),
          ).fadeSlideIn(),
          const SizedBox(height: 18),
          Text(
            'favorite_reports'.tr(),
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
          ).fadeSlideIn(delayMs: 40),
          const SizedBox(height: 10),
          if (favorites.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 12),
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
          const SizedBox(height: 18),
          Text(
            'other_reports'.tr(),
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
          ).fadeSlideIn(delayMs: 80),
          const SizedBox(height: 12),
          if (others.isEmpty)
            Text('no_data'.tr(), style: Theme.of(context).textTheme.bodyMedium)
          else
            SizedBox(
              height: 104,
              child: ListView.separated(
                scrollDirection: Axis.horizontal,
                itemCount: others.length,
                separatorBuilder: (_, __) => const SizedBox(width: 12),
                itemBuilder: (context, index) {
                  final item = others[index];
                  return _QuickReportChip(
                    item: item,
                    onTap: () =>
                        Get.toNamed(AppRoutes.reportDetailPath(item.type)),
                    onFavorite: () => _toggleFavorite(item.type),
                  ).fadeSlideInList(index: index);
                },
              ),
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
      subtitle: '${'last_updated'.tr()} — ${DateFormat.MMMd().format(DateTime.now())}',
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

class _QuickReportChip extends StatelessWidget {
  const _QuickReportChip({
    required this.item,
    required this.onTap,
    required this.onFavorite,
  });

  final _ReportItem item;
  final VoidCallback onTap;
  final VoidCallback onFavorite;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      onLongPress: onFavorite,
      borderRadius: BorderRadius.circular(16),
      child: SizedBox(
        width: 88,
        child: Column(
          children: [
            Container(
              width: 58,
              height: 58,
              decoration: BoxDecoration(
                color: Theme.of(context).brightness == Brightness.dark
                    ? AppColors.surfaceDarkCard
                    : Colors.white,
                borderRadius: BorderRadius.circular(18),
                boxShadow: AppColors.cardShadow(
                  dark: Theme.of(context).brightness == Brightness.dark,
                ),
              ),
              child: Icon(item.icon, color: item.color),
            ),
            const SizedBox(height: 8),
            Text(
              item.titleKey.tr(),
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.labelLarge?.copyWith(
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                    height: 1.2,
                  ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ReportItem {
  const _ReportItem(
    this.type,
    this.titleKey,
    this.icon,
    this.color,
    this.category,
  );

  final String type;
  final String titleKey;
  final IconData icon;
  final Color color;
  final _ReportCategory category;
}
