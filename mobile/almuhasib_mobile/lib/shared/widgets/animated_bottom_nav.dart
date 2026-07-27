import 'dart:ui' as ui;

import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';

import '../../core/constants/app_colors.dart';

class BottomNavItem {
  const BottomNavItem({
    required this.icon,
    required this.activeIcon,
    required this.labelKey,
  });

  final IconData icon;
  final IconData activeIcon;
  final String labelKey;
}

class AnimatedBottomNavBar extends StatelessWidget {
  const AnimatedBottomNavBar({
    super.key,
    required this.selectedIndex,
    required this.onTap,
    required this.items,
    this.onFabTap,
    this.fabIcon = Icons.add_rounded,
    this.accentColor,
    this.primaryColor,
  });

  final int selectedIndex;
  final ValueChanged<int> onTap;
  final List<BottomNavItem> items;
  final VoidCallback? onFabTap;
  final IconData fabIcon;
  final Color? accentColor;
  final Color? primaryColor;

  static const double _fabSize = 56;
  static const double _fabGap = 68;
  static const double _barHeight = 64;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final bottomInset = MediaQuery.paddingOf(context).bottom;
    final accent = accentColor ?? AppColors.accent;
    final primary = primaryColor ?? AppColors.primary;
    final showFab = onFabTap != null && items.length >= 2;
    final compact = items.length >= 5;

    // Keep geometric LTR order so the FAB stays centered and tabs stay
    // index-ordered (0…n) even when the app locale is RTL.
    final leftCount = items.length ~/ 2;
    final leftItems = [
      for (var i = 0; i < leftCount; i++) (index: i, item: items[i]),
    ];
    final rightItems = [
      for (var i = leftCount; i < items.length; i++) (index: i, item: items[i]),
    ];

    return Padding(
      padding: EdgeInsets.fromLTRB(12, 0, 12, bottomInset + 8),
      child: SizedBox(
        height: showFab ? 76 : _barHeight,
        child: Stack(
          clipBehavior: Clip.none,
          alignment: Alignment.bottomCenter,
          children: [
            Positioned(
              left: 0,
              right: 0,
              bottom: 0,
              child: Container(
                height: _barHeight,
                decoration: BoxDecoration(
                  color: isDark ? AppColors.surfaceDarkCard : Colors.white,
                  borderRadius: BorderRadius.circular(28),
                  boxShadow: [
                    BoxShadow(
                      color: AppColors.primary.withValues(
                        alpha: isDark ? 0.28 : 0.12,
                      ),
                      blurRadius: 24,
                      offset: const Offset(0, 8),
                    ),
                  ],
                ),
                child: Directionality(
                  textDirection: ui.TextDirection.ltr,
                  child: Row(
                    children: [
                      Expanded(
                        child: Row(
                          children: [
                            for (final entry in leftItems)
                              Expanded(
                                child: _NavItem(
                                  item: entry.item,
                                  selected: entry.index == selectedIndex,
                                  onTap: () => onTap(entry.index),
                                  accent: accent,
                                  primary: primary,
                                  compact: compact,
                                ),
                              ),
                          ],
                        ),
                      ),
                      if (showFab) const SizedBox(width: _fabGap),
                      Expanded(
                        child: Row(
                          children: [
                            for (final entry in rightItems)
                              Expanded(
                                child: _NavItem(
                                  item: entry.item,
                                  selected: entry.index == selectedIndex,
                                  onTap: () => onTap(entry.index),
                                  accent: accent,
                                  primary: primary,
                                  compact: compact,
                                ),
                              ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            if (showFab)
              Positioned(
                top: 0,
                child: GestureDetector(
                  onTap: onFabTap,
                  child: Container(
                    width: _fabSize,
                    height: _fabSize,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      gradient: LinearGradient(
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                        colors: [primary.withValues(alpha: 0.95), primary],
                      ),
                      boxShadow: [
                        BoxShadow(
                          color: primary.withValues(alpha: 0.4),
                          blurRadius: 16,
                          offset: const Offset(0, 8),
                        ),
                      ],
                    ),
                    child: Icon(fabIcon, color: Colors.white, size: 28),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _NavItem extends StatelessWidget {
  const _NavItem({
    required this.item,
    required this.selected,
    required this.onTap,
    required this.accent,
    required this.primary,
    this.compact = false,
  });

  final BottomNavItem item;
  final bool selected;
  final VoidCallback onTap;
  final Color accent;
  final Color primary;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final muted =
        Theme.of(context).colorScheme.onSurface.withValues(alpha: 0.5);
    final color = selected ? primary : muted;
    final iconSize = compact ? 22.0 : 24.0;
    final fontSize = compact ? (selected ? 9.5 : 9.0) : (selected ? 11.0 : 10.5);

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 2),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              AnimatedContainer(
                duration: const Duration(milliseconds: 220),
                padding: EdgeInsets.symmetric(
                  horizontal: compact ? 6 : 10,
                  vertical: 4,
                ),
                decoration: BoxDecoration(
                  color: selected
                      ? primary.withValues(alpha: 0.12)
                      : Colors.transparent,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(
                  selected ? item.activeIcon : item.icon,
                  color: selected ? primary : color,
                  size: iconSize,
                ),
              ),
              const SizedBox(height: 2),
              AnimatedDefaultTextStyle(
                duration: const Duration(milliseconds: 200),
                style: TextStyle(
                  fontSize: fontSize,
                  fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
                  color: selected ? primary : color,
                  height: 1.1,
                ),
                child: Text(
                  item.labelKey.tr(),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  textAlign: TextAlign.center,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
