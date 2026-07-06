import 'package:flutter/material.dart';

import '../../../core/theme/app_spacing.dart';
import '../common_widgets.dart';

class AppKpiGrid extends StatelessWidget {
  const AppKpiGrid({
    super.key,
    required this.items,
    this.crossAxisCount = 2,
    this.childAspectRatio = 1.35,
  });

  final List<AppKpiItem> items;
  final int crossAxisCount;
  final double childAspectRatio;

  @override
  Widget build(BuildContext context) {
    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: crossAxisCount,
        mainAxisSpacing: AppSpacing.md,
        crossAxisSpacing: AppSpacing.md,
        childAspectRatio: childAspectRatio,
      ),
      itemCount: items.length,
      itemBuilder: (context, index) {
        final item = items[index];
        return KpiCard(
          title: item.title,
          value: item.value,
          icon: item.icon,
          color: item.color,
          compact: item.compact,
        );
      },
    );
  }
}

class AppKpiItem {
  const AppKpiItem({
    required this.title,
    required this.value,
    required this.icon,
    this.color,
    this.compact = false,
  });

  final String title;
  final String value;
  final IconData icon;
  final Color? color;
  final bool compact;
}
