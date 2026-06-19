import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../../../shared/widgets/shimmer_widgets.dart';
import '../controllers/hotel_reservations_controller.dart';
import '../models/hotel_models.dart';
import '../models/hotel_status_helpers.dart';

class HotelReservationsScreen extends StatefulWidget {
  const HotelReservationsScreen({super.key});

  @override
  State<HotelReservationsScreen> createState() =>
      _HotelReservationsScreenState();
}

class _HotelReservationsScreenState extends State<HotelReservationsScreen> {
  late final HotelReservationsController _controller;

  @override
  void initState() {
    super.initState();
    _controller = Get.put(HotelReservationsController(), tag: 'hotel_reservations');
  }

  @override
  Widget build(BuildContext context) {
    final topPadding = MediaQuery.paddingOf(context).top;

    return Scaffold(
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => Get.toNamed(AppRoutes.hotelReservationNew),
        icon: const Icon(Icons.add),
        label: Text('hotel_new_reservation'.tr()),
      ),
      body: Column(
        children: [
          SizedBox(height: topPadding + 8),
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 0),
            child: Text(
              'hotel_reservations_title'.tr(),
              style: Theme.of(context).textTheme.headlineSmall,
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
            child: SearchFilterBar(
              onSearchChanged: _controller.updateSearch,
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _controller.load,
              child: Obx(() {
                if (_controller.isLoading.value) {
                  return const ListShimmer();
                }
                final error = _controller.error.value;
                if (error != null) {
                  return ErrorStateWidget(
                    message: error.toString(),
                    onRetry: _controller.load,
                  );
                }
                final page = _controller.page.value;
                if (page == null || page.items.isEmpty) {
                  return ListView(
                    children: [
                      SizedBox(
                        height: MediaQuery.sizeOf(context).height * 0.4,
                        child: EmptyStateWidget(
                          message: _controller.search.value.isEmpty
                              ? 'no_data'.tr()
                              : 'no_search_results'.tr(),
                        ),
                      ),
                    ],
                  );
                }
                return ListView.builder(
                  padding: const EdgeInsets.fromLTRB(16, 16, 16, 120),
                  itemCount: page.items.length,
                  itemBuilder: (context, index) {
                    final item = page.items[index];
                    return _ReservationTile(
                      reservation: item,
                      onTap: () => Get.toNamed(
                        AppRoutes.hotelReservationDetailPath(item.syncId),
                        arguments: item,
                      ),
                    ).fadeSlideInList(index: index);
                  },
                );
              }),
            ),
          ),
        ],
      ),
    );
  }
}

class _ReservationTile extends StatelessWidget {
  const _ReservationTile({
    required this.reservation,
    required this.onTap,
  });

  final HotelReservation reservation;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final statusColor = hotelReservationStatusColor(reservation.status);

    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: ListTile(
        onTap: onTap,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
        leading: CircleAvatar(
          backgroundColor: statusColor.withValues(alpha: 0.15),
          child: Icon(Icons.event_note_rounded, color: statusColor),
        ),
        title: Text(reservation.guestName),
        subtitle: Text(
          '${reservation.reservationNumber} • ${reservation.roomNumber ?? '—'}',
        ),
        trailing: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              hotelReservationStatusLabel(reservation.status),
              style: TextStyle(
                color: statusColor,
                fontWeight: FontWeight.w600,
                fontSize: 12,
              ),
            ),
            Text(
              formatDate(reservation.checkInDate),
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ],
        ),
      ),
    );
  }
}
