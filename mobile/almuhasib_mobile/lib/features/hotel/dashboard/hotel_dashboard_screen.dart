import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
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
      body: Column(
        children: [
          Obx(
            () => ConnectivityBanner(
              isOffline: AppServices.connectivity.isOffline.value,
            ),
          ),
          Expanded(
            child: Obx(() {
              final data = controller.data.value;
              if (data != null) {
                return RefreshIndicator(
                  onRefresh: controller.load,
                  child: _HotelDashboardBody(
                    data: data,
                    tenantName: tenantName,
                  ),
                );
              }

              if (controller.isLoading.value) {
                return const DashboardShimmer();
              }

              final error = controller.error.value;
              if (error != null) {
                return RefreshIndicator(
                  onRefresh: controller.load,
                  child: ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    children: [
                      SizedBox(
                        height: MediaQuery.sizeOf(context).height * 0.6,
                        child: ErrorStateWidget(
                          message: AppExceptionHandler.messageFor(error),
                          onRetry: controller.load,
                        ),
                      ),
                    ],
                  ),
                );
              }

              return const DashboardShimmer();
            }),
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
      physics: const AlwaysScrollableScrollPhysics(),
      padding: EdgeInsets.fromLTRB(20, topPadding + 12, 20, 120),
      children: [
        _HotelHeader(tenantName: tenantName),
        const SizedBox(height: 20),
        Text(
          'hotel_occupancy'.tr(),
          style: Theme.of(context).textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.w800,
              ),
        ),
        const SizedBox(height: 12),
        _KpiRow(
          left: KpiCard(
            title: 'hotel_total_rooms'.tr(),
            value: '${occupancy.totalRooms}',
            icon: Icons.meeting_room_outlined,
            color: AppColors.primaryLight,
          ),
          right: KpiCard(
            title: 'hotel_occupied_rooms'.tr(),
            value: '${occupancy.occupiedRooms}',
            icon: Icons.hotel_rounded,
            color: AppColors.accent,
          ),
        ),
        const SizedBox(height: 12),
        _KpiRow(
          left: KpiCard(
            title: 'hotel_available_rooms'.tr(),
            value: '${occupancy.availableRooms}',
            icon: Icons.check_circle_outline,
            color: AppColors.success,
          ),
          right: KpiCard(
            title: 'hotel_occupancy_rate'.tr(),
            value: '${occupancy.occupancyRate.toStringAsFixed(1)}%',
            icon: Icons.pie_chart_outline_rounded,
            color: AppColors.warning,
          ),
        ),
        const SizedBox(height: 22),
        Text(
          'hotel_today_summary'.tr(),
          style: Theme.of(context).textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.w800,
              ),
        ),
        const SizedBox(height: 12),
        _KpiRow(
          left: KpiCard(
            title: 'hotel_today_arrivals'.tr(),
            value: '${data.todayArrivals}',
            icon: Icons.flight_land_rounded,
            compact: true,
            color: AppColors.success,
          ),
          right: KpiCard(
            title: 'hotel_today_departures'.tr(),
            value: '${data.todayDepartures}',
            icon: Icons.flight_takeoff_rounded,
            compact: true,
            color: AppColors.warning,
          ),
        ),
        const SizedBox(height: 12),
        _KpiRow(
          left: KpiCard(
            title: 'hotel_in_house'.tr(),
            value: '${data.inHouseGuests}',
            icon: Icons.people_outline_rounded,
            compact: true,
            color: AppColors.primaryLight,
          ),
          right: KpiCard(
            title: 'hotel_today_revenue'.tr(),
            value: formatCurrency(data.todayRevenue),
            icon: Icons.payments_outlined,
            compact: true,
            color: AppColors.accent,
          ),
        ),
        const SizedBox(height: 22),
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
        ),
      ],
    );
  }
}

class _KpiRow extends StatelessWidget {
  const _KpiRow({required this.left, required this.right});

  final Widget left;
  final Widget right;

  @override
  Widget build(BuildContext context) {
    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Expanded(child: left),
          const SizedBox(width: 12),
          Expanded(child: right),
        ],
      ),
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
        children: [
          const AppLogoMark(size: 56),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'hotel_dashboard_greeting'.tr(),
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        fontSize: 14,
                      ),
                ),
                const SizedBox(height: 6),
                Text(
                  tenantName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: Theme.of(context).textTheme.titleLarge?.copyWith(
                        fontSize: 22,
                        height: 1.2,
                        fontWeight: FontWeight.w800,
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
