import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/providers/core_providers.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../../../shared/widgets/shimmer_widgets.dart';
import '../models/hotel_models.dart';

final hotelGuestsProvider =
    FutureProvider.autoDispose.family<HotelGuestPage, String>(
  (ref, search) {
    return ref.watch(hotelRepositoryProvider).getGuests(
          search: search,
          pageSize: 50,
        );
  },
);

class HotelGuestsScreen extends ConsumerStatefulWidget {
  const HotelGuestsScreen({super.key});

  @override
  ConsumerState<HotelGuestsScreen> createState() => _HotelGuestsScreenState();
}

class _HotelGuestsScreenState extends ConsumerState<HotelGuestsScreen> {
  String _search = '';

  @override
  Widget build(BuildContext context) {
    final guestsAsync = ref.watch(hotelGuestsProvider(_search));
    final topPadding = MediaQuery.paddingOf(context).top;

    return Scaffold(
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final saved = await context.push<bool>('/hotel/guests/new');
          if (saved == true && mounted) {
            ref.invalidate(hotelGuestsProvider(_search));
          }
        },
        child: const Icon(Icons.person_add_outlined),
      ),
      body: Column(
        children: [
          SizedBox(height: topPadding + 8),
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 0),
            child: Text(
              'hotel_guests_title'.tr(),
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
                  ref.invalidate(hotelGuestsProvider(_search)),
              child: guestsAsync.when(
                loading: () => const ListShimmer(),
                error: (e, _) => ErrorStateWidget(
                  message: e.toString(),
                  onRetry: () =>
                      ref.invalidate(hotelGuestsProvider(_search)),
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
                      final guest = page.items[index];
                      return Card(
                        margin: const EdgeInsets.only(bottom: 10),
                        child: ListTile(
                          onTap: () async {
                            final saved = await context.push<bool>(
                              '/hotel/guests/${guest.syncId}/edit',
                              extra: guest,
                            );
                            if (saved == true && mounted) {
                              ref.invalidate(hotelGuestsProvider(_search));
                            }
                          },
                          leading: CircleAvatar(
                            child: Text(
                              guest.fullName.isNotEmpty
                                  ? guest.fullName[0]
                                  : '?',
                            ),
                          ),
                          title: Text(guest.fullName),
                          subtitle: Text(
                            [
                              if (guest.phone != null && guest.phone!.isNotEmpty)
                                guest.phone!,
                              if (guest.idNumber != null &&
                                  guest.idNumber!.isNotEmpty)
                                guest.idNumber!,
                            ].join(' • '),
                          ),
                          trailing: const Icon(Icons.chevron_right_rounded),
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
