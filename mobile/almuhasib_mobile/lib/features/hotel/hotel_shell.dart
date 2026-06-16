import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../shared/widgets/animated_bottom_nav.dart';

class HotelShell extends StatelessWidget {
  const HotelShell({super.key, required this.navigationShell});

  final StatefulNavigationShell navigationShell;

  static const _navItems = [
    BottomNavItem(
      icon: Icons.hotel_outlined,
      activeIcon: Icons.hotel_rounded,
      labelKey: 'hotel_nav_home',
    ),
    BottomNavItem(
      icon: Icons.event_note_outlined,
      activeIcon: Icons.event_note_rounded,
      labelKey: 'hotel_nav_reservations',
    ),
    BottomNavItem(
      icon: Icons.meeting_room_outlined,
      activeIcon: Icons.meeting_room_rounded,
      labelKey: 'hotel_nav_rooms',
    ),
    BottomNavItem(
      icon: Icons.login_outlined,
      activeIcon: Icons.login_rounded,
      labelKey: 'hotel_nav_operations',
    ),
    BottomNavItem(
      icon: Icons.people_outline_rounded,
      activeIcon: Icons.people_rounded,
      labelKey: 'hotel_nav_guests',
    ),
  ];

  void _onTap(int index) {
    navigationShell.goBranch(
      index,
      initialLocation: index == navigationShell.currentIndex,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      extendBody: true,
      body: navigationShell,
      bottomNavigationBar: AnimatedBottomNavBar(
        selectedIndex: navigationShell.currentIndex,
        onTap: _onTap,
        items: _navItems,
      ),
    );
  }
}
