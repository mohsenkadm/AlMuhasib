import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/shimmer_widgets.dart';
import '../controllers/hotel_check_in_out_controller.dart';
import '../models/hotel_models.dart';
import '../models/hotel_status_helpers.dart';

class HotelCheckInOutScreen extends GetView<HotelCheckInOutController> {
  const HotelCheckInOutScreen({super.key});

  @override
  final String? tag = 'hotel_check_in_out';

  Future<void> _performCheckIn(
    BuildContext context,
    HotelReservation reservation,
  ) async {
    try {
      await controller.checkIn(reservation);
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('hotel_check_in_success'.tr())),
        );
      }
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString())),
        );
      }
    }
  }

  Future<void> _performCheckOut(
    BuildContext context,
    HotelReservation reservation,
  ) async {
    try {
      await controller.checkOut(reservation);
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('hotel_check_out_success'.tr())),
        );
      }
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString())),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 2,
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: TabBar(
              tabs: [
                Tab(text: 'hotel_today_arrivals'.tr()),
                Tab(text: 'hotel_today_departures'.tr()),
              ],
            ),
          ),
          Expanded(
            child: Obx(() {
              final reservations = controller.reservations.value;
              final isLoading = controller.isLoading.value;
              final error = controller.error.value;
              final hasData = reservations.isNotEmpty;

              if (!hasData && isLoading) {
                return const ListShimmer();
              }

              if (!hasData && error != null) {
                return ErrorStateWidget(
                  message: error.toString(),
                  onRetry: controller.load,
                );
              }

              final today = DateTime.now();
              final arrivals = reservations
                  .where(
                    (r) =>
                        r.checkInDate.year == today.year &&
                        r.checkInDate.month == today.month &&
                        r.checkInDate.day == today.day,
                  )
                  .toList();
              final departures = reservations
                  .where(
                    (r) =>
                        r.checkOutDate.year == today.year &&
                        r.checkOutDate.month == today.month &&
                        r.checkOutDate.day == today.day,
                  )
                  .toList();

              return TabBarView(
                children: [
                  RefreshIndicator(
                    onRefresh: controller.load,
                    child: _ReservationList(
                      items: arrivals,
                      emptyMessage: 'hotel_no_arrivals'.tr(),
                      actionLabel: 'hotel_check_in'.tr(),
                      onAction: (r) => _performCheckIn(context, r),
                    ),
                  ),
                  RefreshIndicator(
                    onRefresh: controller.load,
                    child: _ReservationList(
                      items: departures,
                      emptyMessage: 'hotel_no_departures'.tr(),
                      actionLabel: 'hotel_check_out'.tr(),
                      onAction: (r) => _performCheckOut(context, r),
                    ),
                  ),
                ],
              );
            }),
          ),
        ],
      ),
    );
  }
}

class _ReservationList extends StatelessWidget {
  const _ReservationList({
    required this.items,
    required this.emptyMessage,
    required this.actionLabel,
    required this.onAction,
  });

  final List<HotelReservation> items;
  final String emptyMessage;
  final String actionLabel;
  final Future<void> Function(HotelReservation) onAction;

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          SizedBox(
            height: MediaQuery.sizeOf(context).height * 0.35,
            child: EmptyStateWidget(message: emptyMessage),
          ),
        ],
      );
    }

    return ListView.builder(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 120),
      itemCount: items.length,
      itemBuilder: (context, index) {
        final item = items[index];
        final statusColor = hotelReservationStatusColor(item.status);

        return Card(
          margin: const EdgeInsets.only(bottom: 10),
          child: ListTile(
            contentPadding:
                const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
            leading: CircleAvatar(
              backgroundColor: statusColor.withValues(alpha: 0.15),
              child: Icon(Icons.person_outline, color: statusColor),
            ),
            title: Text(item.guestName),
            subtitle: Text(
              '${item.roomNumber ?? '—'} • ${formatDate(item.checkInDate)}',
            ),
            trailing: FilledButton.tonal(
              onPressed: () => onAction(item),
              child: Text(actionLabel),
            ),
            onTap: () => Get.toNamed(
              AppRoutes.hotelReservationDetailPath(item.syncId),
              arguments: item,
            ),
          ),
        ).fadeSlideInList(index: index);
      },
    );
  }
}
