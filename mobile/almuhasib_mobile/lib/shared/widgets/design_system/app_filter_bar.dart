import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';

import '../../../core/theme/app_spacing.dart';
import '../../utils/formatters.dart';
import '../search_filter_bar.dart';

/// Modern filter panel: search, date range, and chip filters in one card.
class AppFilterBar extends StatelessWidget {
  const AppFilterBar({
    super.key,
    this.onSearchChanged,
    this.searchHint,
    this.filterChips = const [],
    this.onFilterSelected,
    this.from,
    this.to,
    this.onPickFrom,
    this.onPickTo,
    this.onClear,
    this.showDateRange = false,
  });

  final ValueChanged<String>? onSearchChanged;
  final String? searchHint;
  final List<FilterChipOption> filterChips;
  final ValueChanged<String?>? onFilterSelected;
  final DateTime? from;
  final DateTime? to;
  final VoidCallback? onPickFrom;
  final VoidCallback? onPickTo;
  final VoidCallback? onClear;
  final bool showDateRange;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colorScheme = theme.colorScheme;

    return Container(
      margin: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.sm,
        AppSpacing.lg,
        AppSpacing.sm,
      ),
      decoration: BoxDecoration(
        color: colorScheme.surfaceContainerLowest,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: colorScheme.outlineVariant.withValues(alpha: 0.5),
        ),
        boxShadow: [
          BoxShadow(
            color: colorScheme.shadow.withValues(alpha: 0.04),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (onSearchChanged != null)
              SearchFilterBar(
                hint: searchHint,
                onSearchChanged: onSearchChanged!,
                filterChips: filterChips,
                onFilterSelected: onFilterSelected,
              )
            else if (filterChips.isNotEmpty && onFilterSelected != null)
              SearchFilterBar(
                onSearchChanged: (_) {},
                filterChips: filterChips,
                onFilterSelected: onFilterSelected,
              ),
            if (showDateRange && onPickFrom != null && onPickTo != null) ...[
              if (onSearchChanged != null || filterChips.isNotEmpty)
                const SizedBox(height: AppSpacing.sm),
              Row(
                children: [
                  Expanded(
                    child: _DatePill(
                      label: 'from_date'.tr(),
                      date: from,
                      icon: Icons.calendar_today_rounded,
                      onTap: onPickFrom!,
                    ),
                  ),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 6),
                    child: Icon(
                      Icons.arrow_forward_rounded,
                      size: 18,
                      color: colorScheme.outline,
                    ),
                  ),
                  Expanded(
                    child: _DatePill(
                      label: 'to_date'.tr(),
                      date: to,
                      icon: Icons.event_rounded,
                      onTap: onPickTo!,
                    ),
                  ),
                ],
              ),
            ],
            if (onClear != null) ...[
              const SizedBox(height: AppSpacing.xs),
              Align(
                alignment: AlignmentDirectional.centerEnd,
                child: TextButton.icon(
                  onPressed: onClear,
                  icon: const Icon(Icons.filter_alt_off_outlined, size: 18),
                  label: Text('filter_clear'.tr()),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _DatePill extends StatelessWidget {
  const _DatePill({
    required this.label,
    required this.date,
    required this.icon,
    required this.onTap,
  });

  final String label;
  final DateTime? date;
  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Material(
      color: colorScheme.secondaryContainer.withValues(alpha: 0.35),
      borderRadius: BorderRadius.circular(12),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
          child: Row(
            children: [
              Icon(icon, size: 18, color: colorScheme.primary),
              const SizedBox(width: 8),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      label,
                      style: Theme.of(context).textTheme.labelSmall?.copyWith(
                            color: colorScheme.outline,
                          ),
                    ),
                    Text(
                      date != null ? formatDate(date!) : '—',
                      style: Theme.of(context).textTheme.titleSmall?.copyWith(
                            fontWeight: FontWeight.w700,
                          ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
