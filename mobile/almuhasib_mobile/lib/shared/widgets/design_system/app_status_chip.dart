import 'package:flutter/material.dart';

import '../../../core/constants/app_colors.dart';
import '../../../core/theme/app_spacing.dart';

enum AppStatusTone { success, warning, error, info, neutral }

class AppStatusChip extends StatelessWidget {
  const AppStatusChip({
    super.key,
    required this.label,
    this.tone = AppStatusTone.neutral,
    this.color,
  });

  final String label;
  final AppStatusTone tone;
  final Color? color;

  Color _resolveColor(BuildContext context) {
    if (color != null) return color!;
    return switch (tone) {
      AppStatusTone.success => AppColors.success,
      AppStatusTone.warning => AppColors.warning,
      AppStatusTone.error => AppColors.error,
      AppStatusTone.info => Theme.of(context).colorScheme.primary,
      AppStatusTone.neutral => Theme.of(context).colorScheme.onSurface,
    };
  }

  @override
  Widget build(BuildContext context) {
    final c = _resolveColor(context);
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.md,
        vertical: AppSpacing.xs,
      ),
      decoration: BoxDecoration(
        color: c.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(AppSpacing.radiusFull),
        border: Border.all(color: c.withValues(alpha: 0.35)),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: c,
          fontSize: 12,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}
