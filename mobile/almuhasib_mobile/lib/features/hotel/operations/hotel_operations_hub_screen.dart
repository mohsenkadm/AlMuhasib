import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../check_in_out/hotel_check_in_out_screen.dart';
import '../controllers/hotel_operations_hub_controller.dart';
import '../guests/hotel_guests_screen.dart';

class HotelOperationsHubScreen extends GetView<HotelOperationsHubController> {
  const HotelOperationsHubScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final topPadding = MediaQuery.paddingOf(context).top;

    return Column(
      children: [
        SizedBox(height: topPadding + 8),
        Padding(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 0),
          child: Align(
            alignment: AlignmentDirectional.centerStart,
            child: Text(
              'hotel_operations_title'.tr(),
              style: Theme.of(context).textTheme.headlineSmall,
            ),
          ),
        ),
        Material(
          color: Theme.of(context).colorScheme.surface,
          child: TabBar(
            controller: controller.tabController,
            tabs: [
              Tab(text: 'hotel_nav_operations'.tr()),
              Tab(text: 'hotel_nav_guests'.tr()),
            ],
          ),
        ),
        Expanded(
          child: TabBarView(
            controller: controller.tabController,
            children: const [
              HotelCheckInOutScreen(),
              HotelGuestsScreen(),
            ],
          ),
        ),
      ],
    );
  }
}
