import 'package:flutter/material.dart';

import 'application_system_type.dart';

class SystemOnboardingSlide {
  const SystemOnboardingSlide({
    required this.icon,
    required this.titleKey,
    required this.descKey,
    required this.colors,
  });

  final IconData icon;
  final String titleKey;
  final String descKey;
  final List<Color> colors;
}

class SystemProfile {
  const SystemProfile({
    required this.type,
    required this.nameKey,
    required this.taglineKey,
    required this.icon,
    required this.primary,
    required this.secondary,
    required this.accent,
    required this.homeRoute,
    required this.launchRoute,
    required this.onboardingSlides,
  });

  final ApplicationSystemType type;
  final String nameKey;
  final String taglineKey;
  final IconData icon;
  final Color primary;
  final Color secondary;
  final Color accent;
  final String homeRoute;
  final String launchRoute;
  final List<SystemOnboardingSlide> onboardingSlides;

  static SystemProfile of(ApplicationSystemType type) =>
      _profiles[type] ?? _profiles[ApplicationSystemType.accounting]!;

  static SystemProfile ofInt(int raw) => of(ApplicationSystemType.fromInt(raw));

  static const _profiles = {
    ApplicationSystemType.accounting: SystemProfile(
      type: ApplicationSystemType.accounting,
      nameKey: 'system_accounting_name',
      taglineKey: 'system_accounting_tagline',
      icon: Icons.account_balance_wallet_rounded,
      primary: Color(0xFF0D47A1),
      secondary: Color(0xFF1565C0),
      accent: Color(0xFF00ACC1),
      homeRoute: '/home',
      launchRoute: '/launch/accounting',
      onboardingSlides: [
        SystemOnboardingSlide(
          icon: Icons.analytics_outlined,
          titleKey: 'onboarding_acc_title_1',
          descKey: 'onboarding_acc_desc_1',
          colors: [Color(0xFF1565C0), Color(0xFF00ACC1)],
        ),
        SystemOnboardingSlide(
          icon: Icons.receipt_long_outlined,
          titleKey: 'onboarding_acc_title_2',
          descKey: 'onboarding_acc_desc_2',
          colors: [Color(0xFF0D47A1), Color(0xFF1565C0)],
        ),
        SystemOnboardingSlide(
          icon: Icons.notifications_active_outlined,
          titleKey: 'onboarding_acc_title_3',
          descKey: 'onboarding_acc_desc_3',
          colors: [Color(0xFF006064), Color(0xFF00ACC1)],
        ),
      ],
    ),
    ApplicationSystemType.carContracts: SystemProfile(
      type: ApplicationSystemType.carContracts,
      nameKey: 'system_car_name',
      taglineKey: 'system_car_tagline',
      icon: Icons.directions_car_filled_rounded,
      primary: Color(0xFFE65100),
      secondary: Color(0xFFFF8F00),
      accent: Color(0xFFFFB300),
      homeRoute: '/car/home',
      launchRoute: '/launch/car',
      onboardingSlides: [
        SystemOnboardingSlide(
          icon: Icons.description_outlined,
          titleKey: 'onboarding_car_title_1',
          descKey: 'onboarding_car_desc_1',
          colors: [Color(0xFFE65100), Color(0xFFFF8F00)],
        ),
        SystemOnboardingSlide(
          icon: Icons.payments_outlined,
          titleKey: 'onboarding_car_title_2',
          descKey: 'onboarding_car_desc_2',
          colors: [Color(0xFFBF360C), Color(0xFFE65100)],
        ),
        SystemOnboardingSlide(
          icon: Icons.sync_outlined,
          titleKey: 'onboarding_car_title_3',
          descKey: 'onboarding_car_desc_3',
          colors: [Color(0xFF4E342E), Color(0xFFFF8F00)],
        ),
      ],
    ),
    ApplicationSystemType.hotelManagement: SystemProfile(
      type: ApplicationSystemType.hotelManagement,
      nameKey: 'system_hotel_name',
      taglineKey: 'system_hotel_tagline',
      icon: Icons.hotel_rounded,
      primary: Color(0xFF00695C),
      secondary: Color(0xFF00897B),
      accent: Color(0xFFFFB74D),
      homeRoute: '/hotel/home',
      launchRoute: '/launch/hotel',
      onboardingSlides: [
        SystemOnboardingSlide(
          icon: Icons.hotel_outlined,
          titleKey: 'onboarding_hotel_title_1',
          descKey: 'onboarding_hotel_desc_1',
          colors: [Color(0xFF00695C), Color(0xFF00897B)],
        ),
        SystemOnboardingSlide(
          icon: Icons.event_available_outlined,
          titleKey: 'onboarding_hotel_title_2',
          descKey: 'onboarding_hotel_desc_2',
          colors: [Color(0xFF004D40), Color(0xFF26A69A)],
        ),
        SystemOnboardingSlide(
          icon: Icons.bed_outlined,
          titleKey: 'onboarding_hotel_title_3',
          descKey: 'onboarding_hotel_desc_3',
          colors: [Color(0xFF006064), Color(0xFFFFB74D)],
        ),
      ],
    ),
    ApplicationSystemType.carTrading: SystemProfile(
      type: ApplicationSystemType.carTrading,
      nameKey: 'system_car_trade_name',
      taglineKey: 'system_car_trade_tagline',
      icon: Icons.swap_horiz_rounded,
      primary: Color(0xFFE65100),
      secondary: Color(0xFFFF8F00),
      accent: Color(0xFFFFB300),
      homeRoute: '/car-trade/home',
      launchRoute: '/launch/car-trade',
      onboardingSlides: [
        SystemOnboardingSlide(
          icon: Icons.swap_horiz_outlined,
          titleKey: 'onboarding_car_trade_title_1',
          descKey: 'onboarding_car_trade_desc_1',
          colors: [Color(0xFFE65100), Color(0xFFFF8F00)],
        ),
        SystemOnboardingSlide(
          icon: Icons.payments_outlined,
          titleKey: 'onboarding_car_trade_title_2',
          descKey: 'onboarding_car_trade_desc_2',
          colors: [Color(0xFFBF360C), Color(0xFFE65100)],
        ),
        SystemOnboardingSlide(
          icon: Icons.sync_outlined,
          titleKey: 'onboarding_car_trade_title_3',
          descKey: 'onboarding_car_trade_desc_3',
          colors: [Color(0xFF4E342E), Color(0xFFFF8F00)],
        ),
      ],
    ),
    ApplicationSystemType.realEstateContracts: SystemProfile(
      type: ApplicationSystemType.realEstateContracts,
      nameKey: 'system_real_estate_name',
      taglineKey: 'system_real_estate_tagline',
      icon: Icons.home_work_rounded,
      primary: Color(0xFF37474F),
      secondary: Color(0xFF546E7A),
      accent: Color(0xFF00838F),
      homeRoute: '/real-estate/home',
      launchRoute: '/launch/real-estate',
      onboardingSlides: [
        SystemOnboardingSlide(
          icon: Icons.home_work_outlined,
          titleKey: 'onboarding_real_estate_title_1',
          descKey: 'onboarding_real_estate_desc_1',
          colors: [Color(0xFF37474F), Color(0xFF546E7A)],
        ),
        SystemOnboardingSlide(
          icon: Icons.payments_outlined,
          titleKey: 'onboarding_real_estate_title_2',
          descKey: 'onboarding_real_estate_desc_2',
          colors: [Color(0xFF263238), Color(0xFF00838F)],
        ),
        SystemOnboardingSlide(
          icon: Icons.sync_outlined,
          titleKey: 'onboarding_real_estate_title_3',
          descKey: 'onboarding_real_estate_desc_3',
          colors: [Color(0xFF455A64), Color(0xFF26A69A)],
        ),
      ],
    ),
    ApplicationSystemType.goldShop: SystemProfile(
      type: ApplicationSystemType.goldShop,
      nameKey: 'system_gold_shop_name',
      taglineKey: 'system_gold_shop_tagline',
      icon: Icons.diamond_rounded,
      primary: Color(0xFFB8860B),
      secondary: Color(0xFFD4AF37),
      accent: Color(0xFF8B6914),
      homeRoute: '/gold-shop/home',
      launchRoute: '/launch/gold-shop',
      onboardingSlides: [
        SystemOnboardingSlide(
          icon: Icons.diamond_outlined,
          titleKey: 'onboarding_gold_shop_title_1',
          descKey: 'onboarding_gold_shop_desc_1',
          colors: [Color(0xFFB8860B), Color(0xFFD4AF37)],
        ),
        SystemOnboardingSlide(
          icon: Icons.receipt_long_outlined,
          titleKey: 'onboarding_gold_shop_title_2',
          descKey: 'onboarding_gold_shop_desc_2',
          colors: [Color(0xFF8B6914), Color(0xFFB8860B)],
        ),
        SystemOnboardingSlide(
          icon: Icons.sync_outlined,
          titleKey: 'onboarding_gold_shop_title_3',
          descKey: 'onboarding_gold_shop_desc_3',
          colors: [Color(0xFF5D4E37), Color(0xFFD4AF37)],
        ),
      ],
    ),
  };
}

const accountingRoutePrefixes = ['/home', '/reports', '/data', '/settings'];
const carRoutePrefixes = ['/car'];
const carTradeRoutePrefixes = ['/car-trade'];
const hotelRoutePrefixes = ['/hotel'];
const realEstateRoutePrefixes = ['/real-estate'];
const goldShopRoutePrefixes = ['/gold-shop'];

bool routeBelongsToSystem(String path, ApplicationSystemType type) {
  bool matches(List<String> prefixes) =>
      prefixes.any((p) => path == p || path.startsWith('$p/'));

  return switch (type) {
    ApplicationSystemType.accounting => matches(accountingRoutePrefixes),
    ApplicationSystemType.carContracts => matches(carRoutePrefixes),
    ApplicationSystemType.hotelManagement => matches(hotelRoutePrefixes),
    ApplicationSystemType.carTrading => matches(carTradeRoutePrefixes),
    ApplicationSystemType.realEstateContracts =>
      matches(realEstateRoutePrefixes),
    ApplicationSystemType.goldShop => matches(goldShopRoutePrefixes),
  };
}

String homeRouteFor(ApplicationSystemType type) => SystemProfile.of(type).homeRoute;

String launchRouteFor(ApplicationSystemType type) =>
    SystemProfile.of(type).launchRoute;
