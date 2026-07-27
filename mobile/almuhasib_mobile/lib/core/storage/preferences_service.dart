import 'package:shared_preferences/shared_preferences.dart';

import '../config/application_system_type.dart';
import '../config/env_config.dart';
import '../config/system_profile.dart';
import '../constants/storage_keys.dart';

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

  Future<void> clearSession() async {
    await _prefs.remove(StorageKeys.companyName);
    await _prefs.remove(StorageKeys.username);
    await _prefs.remove(StorageKeys.tenantId);
    await _prefs.remove(StorageKeys.applicationSystemType);
    await _prefs.remove(StorageKeys.tenantName);
  }
}
