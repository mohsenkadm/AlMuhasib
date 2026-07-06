import 'package:flutter/material.dart';

import '../../../core/config/system_profile.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/theme/app_spacing.dart';

class AppSliverAppBar extends StatelessWidget {
  const AppSliverAppBar({
    super.key,
    required this.title,
    this.subtitle,
    this.actions,
    this.expandedHeight = 140,
    this.pinned = true,
    this.floating = false,
    this.showGradient = true,
  });

  final String title;
  final String? subtitle;
  final List<Widget>? actions;
  final double expandedHeight;
  final bool pinned;
  final bool floating;
  final bool showGradient;

  @override
  Widget build(BuildContext context) {
    final profile = AppServices.prefs.systemProfile;
    final primary = profile.primary;
    final secondary = profile.secondary;

    return SliverAppBar(
      expandedHeight: expandedHeight,
      pinned: pinned,
      floating: floating,
      stretch: true,
      elevation: 0,
      scrolledUnderElevation: 0,
      backgroundColor: Colors.transparent,
      actions: actions,
      flexibleSpace: FlexibleSpaceBar(
        stretchModes: const [StretchMode.zoomBackground],
        titlePadding: const EdgeInsetsDirectional.only(
          start: AppSpacing.xl,
          bottom: AppSpacing.lg,
        ),
        title: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: const TextStyle(
                fontWeight: FontWeight.w800,
                fontSize: 18,
                color: Colors.white,
              ),
            ),
            if (subtitle != null)
              Text(
                subtitle!,
                style: TextStyle(
                  fontSize: 12,
                  color: Colors.white.withValues(alpha: 0.85),
                  fontWeight: FontWeight.w500,
                ),
              ),
          ],
        ),
        background: showGradient
            ? DecoratedBox(
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                    colors: [primary, secondary],
                  ),
                ),
              )
            : null,
      ),
    );
  }
}

class AppStandardAppBar extends StatelessWidget implements PreferredSizeWidget {
  const AppStandardAppBar({
    super.key,
    required this.title,
    this.subtitle,
    this.actions,
    this.leading,
    this.showProgress = false,
    this.bottom,
  });

  final String title;
  final String? subtitle;
  final List<Widget>? actions;
  final Widget? leading;
  final bool showProgress;
  final PreferredSizeWidget? bottom;

  @override
  Size get preferredSize => Size.fromHeight(
        kToolbarHeight + (bottom?.preferredSize.height ?? 0) + (showProgress ? 4 : 0),
      );

  @override
  Widget build(BuildContext context) {
    final profile = AppServices.prefs.systemProfile;
    return AppBar(
      leading: leading,
      title: subtitle == null
          ? Text(title)
          : Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title, style: const TextStyle(fontSize: 17)),
                Text(
                  subtitle!,
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: Theme.of(context)
                            .colorScheme
                            .onSurface
                            .withValues(alpha: 0.65),
                      ),
                ),
              ],
            ),
      actions: actions,
      bottom: bottom,
      flexibleSpace: DecoratedBox(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [
              profile.primary.withValues(alpha: 0.12),
              Colors.transparent,
            ],
          ),
        ),
        child: showProgress
            ? const Align(
                alignment: Alignment.bottomCenter,
                child: LinearProgressIndicator(minHeight: 3),
              )
            : null,
      ),
    );
  }
}
