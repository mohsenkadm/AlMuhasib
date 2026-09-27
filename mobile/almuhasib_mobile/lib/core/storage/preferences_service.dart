import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

import '../config/application_system_type.dart';
import '../config/env_config.dart';
import '../config/system_profile.dart';
import '../constants/storage_keys.dart';
import '../../shared/models/auth_models.dart';

class PreferencesService {
  PreferencesService(this._prefs);

  final SharedPreferences _prefs;

  SharedPreferences get rawPrefs => _prefs;

  static Future<PreferencesService> create() async {
    final prefs = await SharedPreferences.getInstance();
    return PreferencesService(prefs);
  }

  bool get onboardingCompleted =>
      _prefs.getBool(StorageKeys.onboardingCompleted) ?? false;

  Future<void> setOnboardingCompleted(bool value) =>
      _prefs.setBool(StorageKeys.onboardingCompleted, value);

  String get apiBaseUrl =>
      _prefs.getString(StorageKeys.apiBaseUrl) ?? EnvConfig.defaultApiUrl();

  Future<void> setApiBaseUrl(String url) =>
      _prefs.setString(StorageKeys.apiBaseUrl, url);

  String? get companyName => _prefs.getString(StorageKeys.companyName);

  Future<void> setCompanyName(String name) =>
      _prefs.setString(StorageKeys.companyName, name);

  String? get username => _prefs.getString(StorageKeys.username);

  Future<void> setUsername(String name) =>
      _prefs.setString(StorageKeys.username, name);

  int? get tenantId => _prefs.getInt(StorageKeys.tenantId);

  Future<void> setTenantId(int id) => _prefs.setInt(StorageKeys.tenantId, id);

  int get applicationSystemType =>
      _prefs.getInt(StorageKeys.applicationSystemType) ?? 0;

  Future<void> setApplicationSystemType(int type) =>
      _prefs.setInt(StorageKeys.applicationSystemType, type);

  String? get tenantName => _prefs.getString(StorageKeys.tenantName);

  Future<void> setTenantName(String name) =>
      _prefs.setString(StorageKeys.tenantName, name);

  ApplicationSystemType get systemType =>
      ApplicationSystemType.fromInt(applicationSystemType);

  bool get isAccountingTenant =>
      systemType == ApplicationSystemType.accounting;

  bool get isCarTenant => systemType == ApplicationSystemType.carContracts;

  bool get isCarTradeTenant => systemType == ApplicationSystemType.carTrading;

  bool get isHotelTenant =>
      systemType == ApplicationSystemType.hotelManagement;

  bool get isRealEstateTenant =>
      systemType == ApplicationSystemType.realEstateContracts;

  bool get isGoldShopTenant => systemType == ApplicationSystemType.goldShop;

  SystemProfile get systemProfile => SystemProfile.of(systemType);

  String get homeRoute => systemProfile.homeRoute;

  String get launchRoute => systemProfile.launchRoute;

  String get themeMode => _prefs.getString(StorageKeys.themeMode) ?? 'dark';

  Future<void> setThemeMode(String mode) =>
      _prefs.setString(StorageKeys.themeMode, mode);

  List<String> get notificationInboxJson =>
      _prefs.getStringList(StorageKeys.notificationInbox) ?? const [];

  Future<void> setNotificationInboxJson(List<String> items) =>
      _prefs.setStringList(StorageKeys.notificationInbox, items);

  List<String> get reportFavorites =>
      _prefs.getStringList(StorageKeys.reportFavorites) ??
      const ['sales', 'profit', 'balance_sheet'];

  Future<void> setReportFavorites(List<String> ids) =>
      _prefs.setStringList(StorageKeys.reportFavorites, ids);

  int? get branchId => _prefs.getInt(StorageKeys.branchId);

  Future<void> setBranchId(int id) => _prefs.setInt(StorageKeys.branchId, id);

  String? get branchName => _prefs.getString(StorageKeys.branchName);

  Future<void> setBranchName(String name) =>
      _prefs.setString(StorageKeys.branchName, name);

  String? get branchCode => _prefs.getString(StorageKeys.branchCode);

  Future<void> setBranchCode(String code) =>
      _prefs.setString(StorageKeys.branchCode, code);

  List<BranchInfo> get allowedBranches {
    final raw = _prefs.getString(StorageKeys.allowedBranchesJson);
    if (raw == null || raw.isEmpty) return const [];
    final list = jsonDecode(raw) as List<dynamic>;
    return list
        .whereType<Map<String, dynamic>>()
        .map(BranchInfo.fromJson)
        .toList();
  }

  Future<void> setAllowedBranches(List<BranchInfo> branches) async {
    final encoded = jsonEncode(branches
        .map((b) => {
              'branchId': b.branchId,
              'syncId': b.syncId,
              'name': b.name,
              'code': b.code,
              'isMain': b.isMain,
              'isDefault': b.isDefault,
            })
        .toList());
    await _prefs.setString(StorageKeys.allowedBranchesJson, encoded);
  }

  Future<void> setCurrentBranch(BranchInfo branch) async {
    await setBranchId(branch.branchId);
    await setBranchName(branch.name);
    await setBranchCode(branch.code);
  }

  Future<void> clearBranch() async {
    await _prefs.remove(StorageKeys.branchId);
    await _prefs.remove(StorageKeys.branchName);
    await _prefs.remove(StorageKeys.branchCode);
  }

  Future<void> clearSession() async {
    await _prefs.remove(StorageKeys.companyName);
    await _prefs.remove(StorageKeys.username);
    await _prefs.remove(StorageKeys.tenantId);
    await _prefs.remove(StorageKeys.applicationSystemType);
    await _prefs.remove(StorageKeys.tenantName);
    await clearBranch();
    await _prefs.remove(StorageKeys.allowedBranchesJson);
  }
}
