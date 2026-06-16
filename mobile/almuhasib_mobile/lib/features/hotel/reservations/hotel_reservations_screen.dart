import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/providers/core_providers.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../../../shared/widgets/shimmer_widgets.dart';
import '../models/hotel_models.dart';
import '../models/hotel_status_helpers.dart';

final hotelReservationsProvider =
    FutureProvider.autoDispose.family<HotelReservationPage, String>(
  (ref, search) {
    return ref.watch(hotelRepositoryProvider).getReservations(
          search: search,
          pageSize: 50,
        );
  },
);

class HotelReservationsScreen extends ConsumerStatefulWidget {
  const HotelReservationsScreen({super.key});

  @override
  ConsumerState<HotelReservationsScreen> createState() =>
      _HotelReservationsScreenState();
}

class _HotelReservationsScreenState
    extends ConsumerState<HotelReservationsScreen> {
  String _search = '';

  @override
  Widget build(BuildContext context) {
    final reservationsAsync = ref.watch(hotelReservationsProvider(_search));
    final topPadding = MediaQuery.paddingOf(context).top;

    return Scaffold(
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
              onSearchChanged: (v) => setState(() => _search = v),
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: () async =>
                  ref.invalidate(hotelReservationsProvider(_search)),
              child: reservationsAsync.when(
                loading: () => const ListShimmer(),
                error: (e, _) => ErrorStateWidget(
                  message: e.toString(),
                  onRetry: () =>
                      ref.invalidate(hotelReservationsProvider(_search)),
                ),
                data: (page) {
                  if (page.items.isEmpty) {
                    return ListView(
                      children: [
                        SizedBox(
                          height: MediaQuery.sizeOf(context).height * 0.4,
                          child: EmptyStateWidget(
                            message: _search.isEmpty
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
                        onTap: () => context.push(
                          '/hotel/reservations/${item.syncId}',
                          extra: item,
                        ),
                      ).fadeSlideInList(index: index);
                    },
                  );
                },
              ),
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
