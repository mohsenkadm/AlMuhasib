import 'package:flutter/material.dart';
import 'package:shimmer/shimmer.dart';

import '../../core/constants/app_colors.dart';

class ShimmerBox extends StatelessWidget {
  const ShimmerBox({
    super.key,
    this.width,
    this.height = 16,
    this.radius = 8,
  });

  final double? width;
  final double height;
  final double radius;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Shimmer.fromColors(
      baseColor: isDark ? Colors.white12 : Colors.grey.shade300,
      highlightColor: isDark ? Colors.white24 : Colors.grey.shade100,
      child: Container(
        width: width,
        height: height,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(radius),
        ),
      ),
    );
  }
}

class DashboardShimmer extends StatelessWidget {
  const DashboardShimmer({super.key});

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        const ShimmerBox(width: 180, height: 28),
        const SizedBox(height: 8),
        const ShimmerBox(width: 240, height: 16),
        const SizedBox(height: 24),
        Row(
          children: [
            Expanded(child: ShimmerBox(height: 100, radius: AppColors.cardRadius)),
            const SizedBox(width: 12),
            Expanded(child: ShimmerBox(height: 100, radius: AppColors.cardRadius)),
          ],
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(child: ShimmerBox(height: 100, radius: AppColors.cardRadius)),
            const SizedBox(width: 12),
            Expanded(child: ShimmerBox(height: 100, radius: AppColors.cardRadius)),
          ],
        ),
        const SizedBox(height: 24),
        ShimmerBox(height: 220, radius: AppColors.cardRadius),
      ],
    );
  }
}

class ListShimmer extends StatelessWidget {
  const ListShimmer({super.key, this.itemCount = 6});

  final int itemCount;

  @override
  Widget build(BuildContext context) {
    return ListView.separated(
      padding: const EdgeInsets.all(20),
      itemCount: itemCount,
      separatorBuilder: (_, __) => const SizedBox(height: 12),
      itemBuilder: (_, __) => ShimmerBox(height: 72, radius: AppColors.cardRadius),
    );
  }
}
