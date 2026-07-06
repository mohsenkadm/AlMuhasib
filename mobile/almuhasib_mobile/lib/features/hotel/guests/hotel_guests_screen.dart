import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/router/app_routes.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/shimmer_widgets.dart';
import '../controllers/hotel_guests_controller.dart';
import '../models/hotel_models.dart';

class HotelGuestsScreen extends StatefulWidget {
  const HotelGuestsScreen({super.key});

  @override
  State<HotelGuestsScreen> createState() => _HotelGuestsScreenState();
}

class _HotelGuestsScreenState extends State<HotelGuestsScreen> {
  late final HotelGuestsController _controller;

  @override
  void initState() {
    super.initState();
    _controller = Get.put(HotelGuestsController(), tag: 'hotel_guests');
  }

  @override
  Widget build(BuildContext context) {
    final topPadding = MediaQuery.paddingOf(context).top;

    return Scaffold(
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final saved = await Get.toNamed<bool>(AppRoutes.hotelGuestNew);
          if (saved == true) {
            _controller.load();
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
                    message: AppExceptionHandler.messageFor(error),
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
                    final guest = page.items[index];
                    return Card(
                      margin: const EdgeInsets.only(bottom: 10),
                      child: ListTile(
                        onTap: () async {
                          final saved = await Get.toNamed<bool>(
                            AppRoutes.hotelGuestEditPath(guest.syncId),
                            arguments: guest,
                          );
                          if (saved == true) {
                            _controller.load();
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
              }),
            ),
          ),
        ],
      ),
    );
  }
}
