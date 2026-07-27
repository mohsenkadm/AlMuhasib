import 'package:get/get.dart';

import '../../router/app_routes.dart';
import '../../storage/preferences_service.dart';
import '../../../features/auth/data/auth_repository.dart';

class AuthController extends GetxController {
  AuthController({
    required AuthRepository repository,
    required PreferencesService preferences,
  })  : _repository = repository,
        _preferences = preferences;

  final AuthRepository _repository;
  final PreferencesService _preferences;

  final isAuthenticated = false.obs;
  final isLoading = true.obs;

  AuthRepository get repository => _repository;
  PreferencesService get preferences => _preferences;

  @override
  void onInit() {
    super.onInit();
    _bootstrap();
  }

  Future<void> _bootstrap() async {
    isLoading.value = true;
    try {
      isAuthenticated.value = await _repository
          .hasValidSession()
          .timeout(const Duration(seconds: 4), onTimeout: () => false);
    } catch (_) {
      isAuthenticated.value = false;
    } finally {
      isLoading.value = false;
    }
  }

  /// Waits until bootstrap finishes (or [timeout] elapses).
  Future<void> waitUntilReady({
    Duration timeout = const Duration(seconds: 6),
  }) async {
    if (!isLoading.value) return;
    final end = DateTime.now().add(timeout);
    while (isLoading.value && DateTime.now().isBefore(end)) {
      await Future<void>.delayed(const Duration(milliseconds: 40));
    }
    if (isLoading.value) {
      isLoading.value = false;
    }
  }

  void _navigateIfNeeded() {
    if (isLoading.value) return;
    final current = Get.currentRoute;
    if (current.isEmpty || current == AppRoutes.splash) return;
    if (!isAuthenticated.value) {
      if (current != AppRoutes.login && current != AppRoutes.onboarding) {
        Get.offAllNamed(
          _preferences.onboardingCompleted
              ? AppRoutes.login
              : AppRoutes.onboarding,
        );
      }
      return;
    }
    if (current == AppRoutes.login || current == AppRoutes.onboarding) {
      Get.offAllNamed(_preferences.launchRoute);
    }
  }

  /// Leaves the splash after the brand animation; resolves the next route.
  void leaveSplash() {
    if (!_preferences.onboardingCompleted) {
      Get.offAllNamed(AppRoutes.onboarding);
      return;
    }
    if (!isAuthenticated.value) {
      Get.offAllNamed(AppRoutes.login);
      return;
    }
    Get.offAllNamed(_preferences.launchRoute);
  }

  Future<void> login(String username, String password) async {
    await _repository.login(username, password);
    isAuthenticated.value = true;
    isLoading.value = false;
    _navigateIfNeeded();
  }

  Future<void> logout() async {
    await _repository.logout();
    isAuthenticated.value = false;
    Get.offAllNamed(AppRoutes.login);
  }

  Future<void> refreshAuth() async {
    try {
      isAuthenticated.value = await _repository.hasValidSession();
    } catch (_) {
      isAuthenticated.value = false;
    }
  }
}
