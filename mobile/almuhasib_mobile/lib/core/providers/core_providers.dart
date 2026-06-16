import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../network/api_client.dart';
import '../services/notification_service.dart';
import '../storage/secure_storage_service.dart';
import '../theme/theme_provider.dart';
import '../../features/auth/data/auth_repository.dart';
import '../../features/dashboard/data/dashboard_repository.dart';
import '../../features/reports/data/reports_repository.dart';
import '../../features/data_tab/data/data_repository.dart';
import '../../features/hotel/data/hotel_repository.dart';
import '../../features/operations/data/mobile_operations_repository.dart';

final secureStorageProvider = Provider<SecureStorageService>((ref) {
  return SecureStorageService();
});

final apiClientProvider = Provider<ApiClient>((ref) {
  final secureStorage = ref.watch(secureStorageProvider);
  final prefs = ref.watch(preferencesServiceProvider);
  final client = ApiClient(
    secureStorage: secureStorage,
    baseUrlResolver: () => prefs.apiBaseUrl,
  );
  client.updateBaseUrl();
  return client;
});

final notificationServiceProvider = Provider<NotificationService>((ref) {
  return NotificationService(ref.watch(apiClientProvider));
});

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepository(
    apiClient: ref.watch(apiClientProvider),
    secureStorage: ref.watch(secureStorageProvider),
    preferences: ref.watch(preferencesServiceProvider),
    notificationService: ref.watch(notificationServiceProvider),
  );
});

final authStateProvider =
    StateNotifierProvider<AuthStateNotifier, AuthState>((ref) {
  return AuthStateNotifier(ref.watch(authRepositoryProvider));
});

final dashboardRepositoryProvider = Provider<DashboardRepository>((ref) {
  return DashboardRepository(ref.watch(apiClientProvider));
});

final reportsRepositoryProvider = Provider<ReportsRepository>((ref) {
  return ReportsRepository(ref.watch(apiClientProvider));
});

final dataRepositoryProvider = Provider<DataRepository>((ref) {
  return DataRepository(ref.watch(apiClientProvider));
});

final mobileOperationsRepositoryProvider =
    Provider<MobileOperationsRepository>((ref) {
  return MobileOperationsRepository(ref.watch(apiClientProvider));
});

final hotelRepositoryProvider = Provider<HotelRepository>((ref) {
  return HotelRepository(ref.watch(apiClientProvider));
});

class AuthState {
  const AuthState({this.isAuthenticated = false, this.isLoading = true});

  final bool isAuthenticated;
  final bool isLoading;

  AuthState copyWith({bool? isAuthenticated, bool? isLoading}) {
    return AuthState(
      isAuthenticated: isAuthenticated ?? this.isAuthenticated,
      isLoading: isLoading ?? this.isLoading,
    );
  }
}

class AuthStateNotifier extends StateNotifier<AuthState> {
  AuthStateNotifier(this._repository) : super(const AuthState()) {
    _bootstrap();
  }

  final AuthRepository _repository;

  Future<void> _bootstrap() async {
    final authenticated = await _repository.hasValidSession();
    state = AuthState(isAuthenticated: authenticated, isLoading: false);
  }

  Future<void> login(String username, String password) async {
    await _repository.login(username, password);
    state = const AuthState(isAuthenticated: true, isLoading: false);
  }

  Future<void> logout() async {
    await _repository.logout();
    state = const AuthState(isAuthenticated: false, isLoading: false);
  }

  Future<void> refreshAuth() async {
    final authenticated = await _repository.hasValidSession();
    state = state.copyWith(isAuthenticated: authenticated);
  }
}
