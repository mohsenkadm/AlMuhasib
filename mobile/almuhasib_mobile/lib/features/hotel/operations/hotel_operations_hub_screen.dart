import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';

import 'hotel_check_in_out_screen.dart';
import 'hotel_guests_screen.dart';

class HotelOperationsHubScreen extends StatefulWidget {
  const HotelOperationsHubScreen({super.key});

  @override
  State<HotelOperationsHubScreen> createState() =>
      _HotelOperationsHubScreenState();
}

class _HotelOperationsHubScreenState extends State<HotelOperationsHubScreen>
    with SingleTickerProviderStateMixin {
  late final TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Material(
          color: Theme.of(context).colorScheme.surface,
          child: TabBar(
            controller: _tabController,
            tabs: [
              Tab(text: 'hotel_nav_operations'.tr()),
              Tab(text: 'hotel_nav_guests'.tr()),
            ],
          ),
        ),
        Expanded(
          child: TabBarView(
            controller: _tabController,
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
