import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/shimmer_widgets.dart';
import '../controllers/hotel_dashboard_controller.dart';
import '../models/hotel_models.dart';

class HotelDashboardScreen extends GetView<HotelDashboardController> {
  const HotelDashboardScreen({super.key});

  @override
  final String? tag = 'hotel_dashboard';

  @override
  Widget build(BuildContext context) {
    final prefs = AppServices.prefs;
    final tenantName = prefs.tenantName ?? prefs.companyName ?? 'app_name'.tr();

    return Scaffold(
      extendBodyBehindAppBar: true,
      body: Column(
        children: [
          Obx(
            () => ConnectivityBanner(
              isOffline: AppServices.connectivity.isOffline.value,
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: controller.load,
              edgeOffset: 120,
              child: Obx(() {
                if (controller.isLoading.value) {
                  return const DashboardShimmer();
                }
                final error = controller.error.value;
                if (error != null) {
                  return ErrorStateWidget(
                    message: AppExceptionHandler.messageFor(error),
                    onRetry: controller.load,
                  );
                }
                final data = controller.data.value;
                if (data == null) {
                  return const SizedBox.shrink();
                }
                return _HotelDashboardBody(
                  data: data,
                  tenantName: tenantName,
                );
              }),
            ),
          ),
        ],
      ),
    );
  }
}

class _HotelDashboardBody extends StatelessWidget {
  const _HotelDashboardBody({
    required this.data,
    required this.tenantName,
  });

  final HotelDashboardData data;
  final String tenantName;

  @override
  Widget build(BuildContext context) {
    final topPadding = MediaQuery.paddingOf(context).top;
    final occupancy = data.occupancy;

    return ListView(
      padding: EdgeInsets.fromLTRB(20, topPadding + 12, 20, 120),
      children: [
        _HotelHeader(tenantName: tenantName).fadeSlideIn(),
        const SizedBox(height: 20),
        Text(
          'hotel_occupancy'.tr(),
          style: Theme.of(context).textTheme.titleMedium,
        ).fadeSlideIn(delayMs: 80),
        const SizedBox(height: 14),
        GridView.count(
          crossAxisCount: 2,
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          mainAxisSpacing: 14,
          crossAxisSpacing: 14,
          childAspectRatio: 0.92,
          children: [
            KpiCard(
              title: 'hotel_total_rooms'.tr(),
              value: '${occupancy.totalRooms}',
              icon: Icons.meeting_room_outlined,
              color: AppColors.primaryLight,
            ).fadeSlideInList(index: 0),
            KpiCard(
              title: 'hotel_occupied_rooms'.tr(),
              value: '${occupancy.occupiedRooms}',
              icon: Icons.hotel_rounded,
              color: AppColors.accent,
            ).fadeSlideInList(index: 1),
            KpiCard(
              title: 'hotel_available_rooms'.tr(),
              value: '${occupancy.availableRooms}',
              icon: Icons.check_circle_outline,
              color: AppColors.success,
            ).fadeSlideInList(index: 2),
            KpiCard(
              title: 'hotel_occupancy_rate'.tr(),
              value: '${occupancy.occupancyRate.toStringAsFixed(1)}%',
              icon: Icons.pie_chart_outline_rounded,
              color: AppColors.warning,
            ).fadeSlideInList(index: 3),
          ],
        ),
        const SizedBox(height: 24),
        Text(
          'hotel_today_summary'.tr(),
          style: Theme.of(context).textTheme.titleMedium,
        ).fadeSlideIn(delayMs: 160),
        const SizedBox(height: 14),
        Row(
          children: [
            Expanded(
              child: KpiCard(
                title: 'hotel_today_arrivals'.tr(),
                value: '${data.todayArrivals}',
                icon: Icons.flight_land_rounded,
                compact: true,
                color: AppColors.success,
              ),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: KpiCard(
                title: 'hotel_today_departures'.tr(),
                value: '${data.todayDepartures}',
                icon: Icons.flight_takeoff_rounded,
                compact: true,
                color: AppColors.warning,
              ),
            ),
          ],
        ).fadeSlideIn(delayMs: 200),
        const SizedBox(height: 14),
        Row(
          children: [
            Expanded(
              child: KpiCard(
                title: 'hotel_in_house'.tr(),
                value: '${data.inHouseGuests}',
                icon: Icons.people_outline_rounded,
                compact: true,
                color: AppColors.primaryLight,
              ),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: KpiCard(
                title: 'hotel_today_revenue'.tr(),
                value: formatCurrency(data.todayRevenue),
                icon: Icons.payments_outlined,
                compact: true,
                color: AppColors.accent,
              ),
            ),
          ],
        ).fadeSlideIn(delayMs: 240),
        const SizedBox(height: 24),
        Row(
          children: [
            Expanded(
              child: FilledButton.icon(
                onPressed: () => Get.offNamed(AppRoutes.hotelOperations),
                icon: const Icon(Icons.login_rounded),
                label: Text('hotel_go_check_in'.tr()),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: OutlinedButton.icon(
                onPressed: () => Get.toNamed(AppRoutes.hotelRestaurant),
                icon: const Icon(Icons.restaurant_rounded),
                label: Text('hotel_nav_restaurant'.tr()),
              ),
            ),
          ],
        ).fadeSlideIn(delayMs: 280),
      ],
    );
  }
}

class _HotelHeader extends StatelessWidget {
  const _HotelHeader({required this.tenantName});

  final String tenantName;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(18, 20, 18, 22),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(24),
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: isDark
              ? [
                  AppColors.primary.withValues(alpha: 0.55),
                  AppColors.accent.withValues(alpha: 0.25),
                ]
              : [
                  AppColors.primaryLight.withValues(alpha: 0.12),
                  AppColors.accent.withValues(alpha: 0.08),
                ],
        ),
        border: Border.all(
          color: isDark
              ? Colors.white.withValues(alpha: 0.1)
              : Colors.black.withValues(alpha: 0.05),
        ),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          const AppLogoMark(size: 56),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  '${'hotel_dashboard_greeting'.tr()} 👋',
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        fontSize: 14,
                      ),
                ),
                const SizedBox(height: 6),
                Text(
                  tenantName,
                  style: Theme.of(context).textTheme.titleLarge?.copyWith(
                        fontSize: 22,
                        height: 1.2,
                      ),
                ),
                const SizedBox(height: 4),
                Text(
                  'hotel_dashboard_subtitle'.tr(),
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
              ],
            ),
          ),
          IconButton(
            onPressed: () => Get.toNamed(AppRoutes.profile),
            icon: const Icon(Icons.person_outline_rounded),
          ),
        ],
      ),
    );
  }
}
