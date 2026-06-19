import 'package:get/get.dart';

import '../../storage/preferences_service.dart';
import '../../router/route_guard.dart';
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
    isAuthenticated.value = await _repository.hasValidSession();
    isLoading.value = false;
    _navigateIfNeeded();
  }

  void _navigateIfNeeded() {
    if (isLoading.value) return;
    final current = Get.currentRoute;
    if (current.isEmpty) return;
    final redirect = RouteGuard.redirect(current);
    if (redirect != null && redirect != current) {
      Get.offAllNamed(redirect);
    }
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
    Get.offAllNamed('/login');
  }

  Future<void> refreshAuth() async {
    isAuthenticated.value = await _repository.hasValidSession();
  }
}
