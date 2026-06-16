import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/presentation/login_screen.dart';
import '../../features/dashboard/presentation/dashboard_screen.dart';
import '../../features/data_tab/presentation/data_list_screen.dart';
import '../../features/data_tab/presentation/data_screen.dart';
import '../../features/hotel/check_in_out/hotel_check_in_out_screen.dart';
import '../../features/hotel/dashboard/hotel_dashboard_screen.dart';
import '../../features/hotel/guests/hotel_guest_form_screen.dart';
import '../../features/hotel/guests/hotel_guests_screen.dart';
import '../../features/hotel/hotel_shell.dart';
import '../../features/hotel/models/hotel_models.dart';
import '../../features/hotel/reservations/hotel_reservation_detail_screen.dart';
import '../../features/hotel/reservations/hotel_reservations_screen.dart';
import '../../features/hotel/rooms/hotel_rooms_screen.dart';
import '../../features/onboarding/onboarding_screen.dart';
import '../../features/operations/presentation/forms/customer_form_screen.dart';
import '../../features/operations/presentation/forms/entity_forms.dart';
import '../../features/operations/presentation/invoice_wizard/invoice_wizard_screen.dart';
import '../../features/profile/about_screen.dart';
import '../../features/profile/privacy_policy_screen.dart';
import '../../features/profile/profile_screen.dart';
import '../../features/reports/presentation/report_detail_screen.dart';
import '../../features/reports/presentation/reports_screen.dart';
import '../../features/settings/settings_screen.dart';
import '../../features/shell/main_shell.dart';
import '../../features/splash/splash_screen.dart';
import '../../shared/models/master_data_models.dart';
import '../providers/core_providers.dart';
import '../theme/theme_provider.dart';
import 'page_transitions.dart';

final routerProvider = Provider<GoRouter>((ref) {
  final authState = ref.watch(authStateProvider);
  final prefs = ref.watch(preferencesServiceProvider);

  return GoRouter(
    initialLocation: '/splash',
    refreshListenable: _RouterRefresh(ref),
    redirect: (context, state) {
      final path = state.matchedLocation;
      final isSplash = path == '/splash';
      final isOnboarding = path == '/onboarding';
      final isLogin = path == '/login';
      final isProfileRoute = path.startsWith('/profile') ||
          path == '/settings' ||
          path == '/about' ||
          path == '/privacy';

      if (authState.isLoading) {
        return isSplash ? null : '/splash';
      }

      if (!prefs.onboardingCompleted && !isOnboarding && !isSplash) {
        return '/onboarding';
      }

      if (!authState.isAuthenticated &&
          !isLogin &&
          !isOnboarding &&
          !isSplash) {
        return '/login';
      }

      if (authState.isAuthenticated && (isLogin || isOnboarding || isSplash)) {
        return prefs.isHotelTenant ? '/hotel/home' : '/home';
      }

      if (authState.isAuthenticated && prefs.isHotelTenant) {
        const accountingPaths = ['/home', '/reports', '/data', '/settings'];
        if (accountingPaths.any((p) => path == p || path.startsWith('$p/'))) {
          return '/hotel/home';
        }
      }

      if (authState.isAuthenticated && !prefs.isHotelTenant && path.startsWith('/hotel')) {
        return '/home';
      }

      if (!authState.isAuthenticated && !authState.isLoading && isSplash) {
        if (!prefs.onboardingCompleted) return '/onboarding';
        return '/login';
      }

      if (!authState.isAuthenticated && isProfileRoute) {
        return '/login';
      }

      return null;
    },
    routes: [
      GoRoute(
        path: '/splash',
        pageBuilder: (_, state) => animatedPage(
          key: state.pageKey,
          child: const SplashScreen(),
        ),
      ),
      GoRoute(
        path: '/onboarding',
        pageBuilder: (_, state) => animatedPage(
          key: state.pageKey,
          child: const OnboardingScreen(),
        ),
      ),
      GoRoute(
        path: '/login',
        pageBuilder: (_, state) => animatedPage(
          key: state.pageKey,
          child: const LoginScreen(),
        ),
      ),
      StatefulShellRoute.indexedStack(
        builder: (_, __, navigationShell) =>
            MainShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/home',
                pageBuilder: (_, state) => animatedPage(
                  key: state.pageKey,
                  child: const DashboardScreen(),
                ),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/reports',
                pageBuilder: (_, state) => animatedPage(
                  key: state.pageKey,
                  child: const ReportsScreen(),
                ),
                routes: [
                  GoRoute(
                    path: 'detail/:type',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: ReportDetailScreen(
                        reportType: state.pathParameters['type']!,
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/data',
                pageBuilder: (_, state) => animatedPage(
                  key: state.pageKey,
                  child: const DataScreen(),
                ),
                routes: [
                  GoRoute(
                    path: 'list/:type',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: DataListScreen(
                        listType: state.pathParameters['type']!,
                      ),
                    ),
                  ),
                  GoRoute(
                    path: 'invoice/new',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: const InvoiceWizardScreen(),
                    ),
                  ),
                  GoRoute(
                    path: 'invoice/:syncId',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: InvoiceDetailScreen(
                        syncId: state.pathParameters['syncId']!,
                      ),
                    ),
                  ),
                  GoRoute(
                    path: 'customer/new',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: const CustomerFormScreen(),
                    ),
                  ),
                  GoRoute(
                    path: 'customer/:syncId/edit',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: CustomerFormScreen(
                        syncId: state.pathParameters['syncId'],
                      ),
                    ),
                  ),
                  GoRoute(
                    path: 'customer/:syncId',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: EntityDetailScreen(
                        entityType: 'customer',
                        syncId: state.pathParameters['syncId']!,
                        name: _entityName(state),
                      ),
                    ),
                  ),
                  GoRoute(
                    path: 'product/new',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: const ProductFormScreen(),
                    ),
                  ),
                  GoRoute(
                    path: 'product/:syncId/edit',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: ProductFormScreen(
                        syncId: state.pathParameters['syncId'],
                      ),
                    ),
                  ),
                  GoRoute(
                    path: 'product/:syncId',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: EntityDetailScreen(
                        entityType: 'product',
                        syncId: state.pathParameters['syncId']!,
                        name: _entityName(state),
                      ),
                    ),
                  ),
                  GoRoute(
                    path: 'supplier/new',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: const SupplierFormScreen(),
                    ),
                  ),
                  GoRoute(
                    path: 'supplier/:syncId/edit',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: SupplierFormScreen(
                        syncId: state.pathParameters['syncId'],
                      ),
                    ),
                  ),
                  GoRoute(
                    path: 'supplier/:syncId',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: EntityDetailScreen(
                        entityType: 'supplier',
                        syncId: state.pathParameters['syncId']!,
                        name: _entityName(state),
                      ),
                    ),
                  ),
                  GoRoute(
                    path: 'investor/new',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: const InvestorFormScreen(),
                    ),
                  ),
                  GoRoute(
                    path: 'investor/:syncId/edit',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: InvestorFormScreen(
                        syncId: state.pathParameters['syncId'],
                      ),
                    ),
                  ),
                  GoRoute(
                    path: 'investor/:syncId',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: EntityDetailScreen(
                        entityType: 'investor',
                        syncId: state.pathParameters['syncId']!,
                        name: _entityName(state),
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/settings',
                pageBuilder: (_, state) => animatedPage(
                  key: state.pageKey,
                  child: const SettingsScreen(),
                ),
              ),
            ],
          ),
        ],
      ),
      StatefulShellRoute.indexedStack(
        builder: (_, __, navigationShell) =>
            HotelShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/hotel/home',
                pageBuilder: (_, state) => animatedPage(
                  key: state.pageKey,
                  child: const HotelDashboardScreen(),
                ),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/hotel/reservations',
                pageBuilder: (_, state) => animatedPage(
                  key: state.pageKey,
                  child: const HotelReservationsScreen(),
                ),
                routes: [
                  GoRoute(
                    path: ':syncId',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: HotelReservationDetailScreen(
                        syncId: state.pathParameters['syncId']!,
                        reservation: _hotelReservation(state),
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/hotel/rooms',
                pageBuilder: (_, state) => animatedPage(
                  key: state.pageKey,
                  child: const HotelRoomsScreen(),
                ),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/hotel/operations',
                pageBuilder: (_, state) => animatedPage(
                  key: state.pageKey,
                  child: const HotelCheckInOutScreen(),
                ),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/hotel/guests',
                pageBuilder: (_, state) => animatedPage(
                  key: state.pageKey,
                  child: const HotelGuestsScreen(),
                ),
                routes: [
                  GoRoute(
                    path: 'new',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: const HotelGuestFormScreen(),
                    ),
                  ),
                  GoRoute(
                    path: ':syncId/edit',
                    pageBuilder: (context, state) => slideHorizontalPage(
                      key: state.pageKey,
                      child: HotelGuestFormScreen(
                        guest: _hotelGuest(state),
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ],
      ),
      GoRoute(
        path: '/profile',
        pageBuilder: (_, state) => slideHorizontalPage(
          key: state.pageKey,
          child: const ProfileScreen(),
        ),
      ),
      GoRoute(
        path: '/about',
        pageBuilder: (_, state) => slideHorizontalPage(
          key: state.pageKey,
          child: const AboutScreen(),
        ),
      ),
      GoRoute(
        path: '/privacy',
        pageBuilder: (_, state) => slideHorizontalPage(
          key: state.pageKey,
          child: const PrivacyPolicyScreen(),
        ),
      ),
    ],
  );
});

String _entityName(GoRouterState state) {
  final extra = state.extra;
  if (extra is LookupItem) return extra.name;
  return state.uri.queryParameters['name'] ?? '';
}

HotelReservation? _hotelReservation(GoRouterState state) {
  final extra = state.extra;
  if (extra is HotelReservation) return extra;
  return null;
}

HotelGuest? _hotelGuest(GoRouterState state) {
  final extra = state.extra;
  if (extra is HotelGuest) return extra;
  return null;
}

class _RouterRefresh extends ChangeNotifier {
  _RouterRefresh(this.ref) {
    ref.listen(authStateProvider, (_, __) => notifyListeners());
    ref.listen(preferencesServiceProvider, (_, __) => notifyListeners());
  }

  final Ref ref;
}
