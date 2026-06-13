import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';

/// Reusable animation presets for consistent motion across the app.
abstract final class AppAnimations {
  static const Duration fast = Duration(milliseconds: 350);
  static const Duration normal = Duration(milliseconds: 500);
  static const Duration slow = Duration(milliseconds: 700);

  static const Curve curve = Curves.easeOutCubic;
  static const Curve bounce = Curves.easeOutBack;
}

extension AnimatedEntry on Widget {
  Widget fadeSlideIn({
    int delayMs = 0,
    double slideY = 0.08,
    Duration? duration,
  }) {
    return animate(delay: Duration(milliseconds: delayMs))
        .fadeIn(
          duration: duration ?? AppAnimations.normal,
          curve: AppAnimations.curve,
        )
        .slideY(
          begin: slideY,
          end: 0,
          duration: duration ?? AppAnimations.normal,
          curve: AppAnimations.curve,
        );
  }

  Widget fadeSlideInList({required int index, double slideY = 0.12}) {
    return fadeSlideIn(
      delayMs: 60 * index,
      slideY: slideY,
      duration: AppAnimations.fast,
    );
  }

  Widget scaleIn({int delayMs = 0}) {
    return animate(delay: Duration(milliseconds: delayMs))
        .scale(
          begin: const Offset(0.85, 0.85),
          end: const Offset(1, 1),
          duration: AppAnimations.normal,
          curve: AppAnimations.bounce,
        )
        .fadeIn(duration: AppAnimations.fast);
  }
}

/// Wraps list children with staggered entrance animations.
class AnimatedListColumn extends StatelessWidget {
  const AnimatedListColumn({
    super.key,
    required this.children,
    this.spacing = 12,
    this.padding,
  });

  final List<Widget> children;
  final double spacing;
  final EdgeInsetsGeometry? padding;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: padding ?? EdgeInsets.zero,
      child: Column(
        children: [
          for (var i = 0; i < children.length; i++) ...[
            if (i > 0) SizedBox(height: spacing),
            children[i].fadeSlideInList(index: i),
          ],
        ],
      ),
    );
  }
}

class AnimatedPageWrapper extends StatelessWidget {
  const AnimatedPageWrapper({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return child.fadeSlideIn(delayMs: 50, slideY: 0.04);
  }
}
