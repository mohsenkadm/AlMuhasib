import 'package:flutter/material.dart';

import '../../../core/constants/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import 'app_balance_hero_card.dart';
import 'app_kpi_grid.dart';

/// Compact statistic chip used inside [PageStatsHeader] strips.
class StatsChipData {
  const StatsChipData({
    required this.label,
    required this.value,
    required this.icon,
    this.color = AppColors.primary,
  });

  final String label;
  final String value;
  final IconData icon;
  final Color color;
}

/// Modern page-level statistics header: optional hero + horizontal stats strip.
class PageStatsHeader extends StatelessWidget {
  const PageStatsHeader({
    super.key,
    required this.heroTitle,
    required this.heroValue,
    this.heroSubtitle,
    this.trendLabel,
    this.trendPositive = true,
    this.stats = const [],
    this.useKpiGrid = false,
  });

  final String heroTitle;
  final String heroValue;
  final String? heroSubtitle;
  final String? trendLabel;
  final bool trendPositive;
  final List<StatsChipData> stats;
  final bool useKpiGrid;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        AppBalanceHeroCard(
          title: heroTitle,
          value: heroValue,
          subtitle: heroSubtitle,
          trendLabel: trendLabel,
          trendPositive: trendPositive,
        ),
        if (stats.isNotEmpty) ...[
          const SizedBox(height: 12),
          if (useKpiGrid || stats.length > 4)
            AppKpiGrid(
              childAspectRatio: 1.55,
              items: [
                for (final s in stats)
                  AppKpiItem(
                    title: s.label,
                    value: s.value,
                    icon: s.icon,
                    color: s.color,
                    compact: true,
                  ),
              ],
            )
          else
            StatsStrip(items: stats),
        ],
      ],
    );
  }
}

/// Single-row equal-width mini KPI tiles for list headers.
class StatsStrip extends StatelessWidget {
  const StatsStrip({super.key, required this.items});

  final List<StatsChipData> items;

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) return const SizedBox.shrink();

    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Row(
      children: [
        for (var i = 0; i < items.length; i++) ...[
          if (i > 0) const SizedBox(width: 8),
          Expanded(child: _StatsChip(data: items[i], isDark: isDark)),
        ],
      ],
    );
  }
}

class _StatsChip extends StatelessWidget {
  const _StatsChip({required this.data, required this.isDark});

  final StatsChipData data;
  final bool isDark;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 12),
      decoration: BoxDecoration(
        color: isDark ? AppColors.surfaceDarkCard : Colors.white,
        borderRadius: BorderRadius.circular(AppSpacing.radiusLg),
        border: Border.all(
          color: data.color.withValues(alpha: isDark ? 0.22 : 0.12),
        ),
        boxShadow: AppColors.cardShadow(dark: isDark),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 28,
            height: 28,
            decoration: BoxDecoration(
              color: data.color.withValues(alpha: 0.14),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(data.icon, size: 16, color: data.color),
          ),
          const SizedBox(height: 8),
          Text(
            data.value,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: Theme.of(context).textTheme.titleSmall?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
          ),
          const SizedBox(height: 2),
          Text(
            data.label,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: Theme.of(context).textTheme.labelSmall?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                  fontWeight: FontWeight.w600,
                ),
          ),
        ],
      ),
    );
  }
}
