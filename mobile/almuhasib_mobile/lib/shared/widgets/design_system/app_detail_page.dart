import 'package:flutter/material.dart';

import '../../../core/theme/app_spacing.dart';
import 'app_sliver_app_bar.dart';

class AppDetailPage extends StatelessWidget {
  const AppDetailPage({
    super.key,
    required this.title,
    this.subtitle,
    required this.header,
    required this.sections,
    this.actions,
    this.floatingActionButton,
    this.onRefresh,
  });

  final String title;
  final String? subtitle;
  final Widget header;
  final List<AppDetailSection> sections;
  final List<Widget>? actions;
  final Widget? floatingActionButton;
  final Future<void> Function()? onRefresh;

  @override
  Widget build(BuildContext context) {
    final body = CustomScrollView(
      slivers: [
        AppSliverAppBar(
          title: title,
          subtitle: subtitle,
          actions: actions,
          expandedHeight: 120,
        ),
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.all(AppSpacing.xl),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                header,
                const SizedBox(height: AppSpacing.xl),
                for (final section in sections) ...[
                  Text(
                    section.title,
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.w700,
                        ),
                  ),
                  if (section.subtitle != null) ...[
                    const SizedBox(height: AppSpacing.xs),
                    Text(
                      section.subtitle!,
                      style: Theme.of(context).textTheme.bodySmall,
                    ),
                  ],
                  const SizedBox(height: AppSpacing.md),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(AppSpacing.lg),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: section.children,
                      ),
                    ),
                  ),
                  const SizedBox(height: AppSpacing.xl),
                ],
                const SizedBox(height: 80),
              ],
            ),
          ),
        ),
      ],
    );

    return Scaffold(
      extendBodyBehindAppBar: true,
      floatingActionButton: floatingActionButton,
      body: onRefresh != null
          ? RefreshIndicator(
              onRefresh: onRefresh!,
              child: body,
            )
          : body,
    );
  }
}

class AppDetailSection {
  const AppDetailSection({
    required this.title,
    required this.children,
    this.subtitle,
  });

  final String title;
  final String? subtitle;
  final List<Widget> children;
}

class AppDetailRow extends StatelessWidget {
  const AppDetailRow({
    super.key,
    required this.label,
    required this.value,
    this.icon,
  });

  final String label;
  final String value;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 18, color: Theme.of(context).colorScheme.secondary),
            const SizedBox(width: AppSpacing.sm),
          ],
          Expanded(
            flex: 2,
            child: Text(
              label,
              style: Theme.of(context).textTheme.bodyMedium,
            ),
          ),
          Expanded(
            flex: 3,
            child: Text(
              value,
              textAlign: TextAlign.end,
              style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                    fontWeight: FontWeight.w600,
                  ),
            ),
          ),
        ],
      ),
    );
  }
}
