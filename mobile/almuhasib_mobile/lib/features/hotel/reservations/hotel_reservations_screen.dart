import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../controllers/hotel_reservations_controller.dart';
import '../models/hotel_models.dart';
import '../models/hotel_status_helpers.dart';

class HotelReservationsScreen extends GetView<HotelReservationsController> {
  const HotelReservationsScreen({super.key});

  @override
  final String? tag = 'hotel_reservations';

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => AppListPage<HotelReservation>(
        title: 'hotel_reservations_title'.tr(),
        isLoading: controller.isLoading,
        error: controller.error,
        items: controller.items,
        onRefresh: controller.load,
        onRetry: controller.load,
        fabLabel: 'hotel_new_reservation'.tr(),
        onFab: () => Get.toNamed(AppRoutes.hotelReservationNew),
        emptyMessage: controller.search.value.isEmpty
            ? 'no_data'.tr()
            : 'no_search_results'.tr(),
        filterPanel: AppFilterBar(
          onSearchChanged: controller.updateSearch,
          showDateRange: true,
          from: controller.from.value,
          to: controller.to.value,
          onPickFrom: controller.pickFromDate,
          onPickTo: controller.pickToDate,
          filterChips: [
            FilterChipOption(
              id: 'Confirmed',
              label: 'hotel_status_confirmed'.tr(),
            ),
            FilterChipOption(
              id: 'CheckedIn',
              label: 'hotel_status_checked_in'.tr(),
            ),
            FilterChipOption(
              id: 'CheckedOut',
              label: 'hotel_status_checked_out'.tr(),
            ),
            FilterChipOption(
              id: 'Cancelled',
              label: 'hotel_status_cancelled'.tr(),
            ),
          ],
          onFilterSelected: controller.updateStatusFilter,
          onClear: controller.clearFilters,
        ),
        itemBuilder: (context, reservation, index) {
          final statusColor = hotelReservationStatusColor(reservation.status);
          return AppEntityCard(
            title: reservation.guestName,
            subtitle:
                '${reservation.reservationNumber} • ${reservation.roomNumber ?? '—'}',
            status: hotelReservationStatusLabel(reservation.status),
            statusTone: statusColor,
            trailing: Text(
              formatDate(reservation.checkInDate),
              style: Theme.of(context).textTheme.bodySmall,
            ),
            onTap: () => Get.toNamed(
              AppRoutes.hotelReservationDetailPath(reservation.syncId),
              arguments: reservation,
            ),
          ).fadeSlideIn(delayMs: index * 40);
        },
      ),
    );
  }
}
