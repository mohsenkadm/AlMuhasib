import 'package:get/get.dart';

import '../../../core/network/api_client.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/offline/offline_write_queue.dart';
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

  Future<SelectBranchResponse> selectBranch(
    int branchId, {
    bool clearOtherBranchQueue = true,
  }) async {
    final previousBranchId = _preferences.branchId;
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

    // Drop pending writes stamped for another branch so they never flush under the new context.
    if (clearOtherBranchQueue &&
        previousBranchId != null &&
        previousBranchId != branchId) {
      try {
        if (Get.isRegistered<OfflineWriteService>()) {
          final queue = Get.find<OfflineWriteService>();
          final foreignIds = queue.pending
              .where((w) => w.branchId != null && w.branchId != branchId)
              .map((w) => w.id)
              .toList();
          for (final id in foreignIds) {
            await queue.remove(id);
          }
        }
      } catch (_) {}
    }

    return response;
  }

  /// Refresh allowed branches after Desktop creates a new branch + sync.
  Future<AllowedBranchesResponse> refreshBranches() async {
    final response = await _apiClient.get(
      '/api/auth/branches',
      parser: (data) =>
          AllowedBranchesResponse.fromJson(data as Map<String, dynamic>),
    );
    await _preferences.setAllowedBranches(response.allowedBranches);

    final currentId = _preferences.branchId;
    if (currentId != null) {
      final match = response.allowedBranches
          .where((b) => b.branchId == currentId)
          .firstOrNull;
      if (match != null) {
        await _preferences.setCurrentBranch(match);
      }
    }
    return response;
  }

  Future<LatestExchangeRateResponse?> getLatestExchangeRate() async {
    try {
      return await _apiClient.get(
        '/api/exchange-rates/latest',
        parser: (data) =>
            LatestExchangeRateResponse.fromJson(data as Map<String, dynamic>),
      );
    } catch (_) {
      return null;
    }
  }

  Future<List<LatestExchangeRateResponse>> listExchangeRates({int take = 30}) {
    return _apiClient.get(
      '/api/exchange-rates',
      queryParameters: {'take': take},
      parser: (data) {
        final list = data as List<dynamic>? ?? const [];
        return list
            .whereType<Map<String, dynamic>>()
            .map(LatestExchangeRateResponse.fromJson)
            .toList();
      },
    );
  }

  Future<LatestExchangeRateResponse> createExchangeRate(
    CreateExchangeRateRequest request,
  ) {
    return _apiClient.post(
      '/api/exchange-rates',
      data: request.toJson(),
      parser: (data) =>
          LatestExchangeRateResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<List<TenantAccountListItem>> listTenantAccounts() {
    return _apiClient.get(
      '/api/tenant-accounts',
      parser: (data) {
        final list = data as List<dynamic>? ?? const [];
        return list
            .whereType<Map<String, dynamic>>()
            .map(TenantAccountListItem.fromJson)
            .toList();
      },
    );
  }

  Future<List<BranchInfo>> listAllTenantBranches() {
    return _apiClient.get(
      '/api/tenant-accounts/all-branches',
      parser: (data) {
        final list = data as List<dynamic>? ?? const [];
        return list
            .whereType<Map<String, dynamic>>()
            .map(BranchInfo.fromJson)
            .toList();
      },
    );
  }

  Future<TenantAccountBranchesResponse> getTenantAccountBranches(int accountId) {
    return _apiClient.get(
      '/api/tenant-accounts/$accountId/branches',
      parser: (data) =>
          TenantAccountBranchesResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<TenantAccountBranchesResponse> setTenantAccountBranches({
    required int accountId,
    required List<int> branchIds,
    int? defaultBranchId,
  }) {
    return _apiClient.put(
      '/api/tenant-accounts/$accountId/branches',
      data: {
        'branchIds': branchIds,
        if (defaultBranchId != null) 'defaultBranchId': defaultBranchId,
      },
      parser: (data) =>
          TenantAccountBranchesResponse.fromJson(data as Map<String, dynamic>),
    );
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
    // Prevent User A offline ops from flushing under User B after re-login.
    try {
      if (Get.isRegistered<OfflineWriteService>()) {
        await Get.find<OfflineWriteService>().clearAll();
      }
    } catch (_) {}
    await _secureStorage.clearTokens();
    await _preferences.clearSession();
  }
}
