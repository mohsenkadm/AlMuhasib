import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/storage/preferences_service.dart';
import '../../../core/storage/secure_storage_service.dart';
import '../../../core/services/notification_service.dart';
import '../../../shared/models/auth_models.dart';

class AuthRepository {
  AuthRepository({
    required ApiClient apiClient,
    required SecureStorageService secureStorage,
    required PreferencesService preferences,
    required NotificationService notificationService,
  })  : _apiClient = apiClient,
        _secureStorage = secureStorage,
        _preferences = preferences,
        _notificationService = notificationService;

  final ApiClient _apiClient;
  final SecureStorageService _secureStorage;
  final PreferencesService _preferences;
  final NotificationService _notificationService;

  /// Returns login response so UI can route to branch picker when needed.
  Future<TenantLoginResponse> login(String username, String password) async {
    _apiClient.updateBaseUrl();
    final response = await _apiClient.post(
      '/api/auth/login',
      data: TenantLoginRequest(username: username, password: password).toJson(),
      parser: (data) =>
          TenantLoginResponse.fromJson(data as Map<String, dynamic>),
    );

    if (!response.isMobileEnabled) {
      throw ApiException(
        message: 'Mobile access is not enabled',
        code: 'SYNC_NOT_ENABLED',
      );
    }

    await _secureStorage.saveTokens(
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      expiresAt: response.accessTokenExpiresAt.toIso8601String(),
    );
    await _preferences.setCompanyName(response.companyName);
    await _preferences.setTenantId(response.tenantId);
    await _preferences.setUsername(username);
    await _preferences.setApplicationSystemType(response.applicationSystemType);
    if (response.tenantName != null && response.tenantName!.isNotEmpty) {
      await _preferences.setTenantName(response.tenantName!);
    }
    await _preferences.setAllowedBranches(response.allowedBranches);

    if (!response.requiresBranchSelection &&
        response.allowedBranches.length == 1) {
      await _preferences.setCurrentBranch(response.allowedBranches.first);
    } else if (response.currentBranchId != null) {
      final match = response.allowedBranches
          .where((b) => b.branchId == response.currentBranchId)
          .firstOrNull;
      if (match != null) await _preferences.setCurrentBranch(match);
    }

    await _notificationService.initialize();
    await _notificationService.registerDeviceWithApi();
    await getLicenseStatus();
    return response;
  }

  Future<SelectBranchResponse> selectBranch(int branchId) async {
    final response = await _apiClient.post(
      '/api/auth/select-branch',
      data: {'branchId': branchId, 'allBranches': false},
      parser: (data) =>
          SelectBranchResponse.fromJson(data as Map<String, dynamic>),
    );

    final existingRefresh = await _secureStorage.getRefreshToken();
    await _secureStorage.saveTokens(
      accessToken: response.accessToken,
      refreshToken: existingRefresh ?? '',
      expiresAt: response.accessTokenExpiresAt.toIso8601String(),
    );

    if (response.branch != null) {
      await _preferences.setCurrentBranch(response.branch!);
    } else {
      final match = _preferences.allowedBranches
          .where((b) => b.branchId == branchId)
          .firstOrNull;
      if (match != null) await _preferences.setCurrentBranch(match);
    }

    return response;
  }

  Future<LicenseStatusResponse> getLicenseStatus() {
    return _apiClient.get(
      '/api/auth/license-status',
      parser: (data) =>
          LicenseStatusResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<bool> hasValidSession() async {
    final token = await _secureStorage.getAccessToken();
    return token != null && token.isNotEmpty;
  }

  Future<void> logout() async {
    await _notificationService.logoutExternalUser();
    await _secureStorage.clearTokens();
    await _preferences.clearSession();
  }
}
