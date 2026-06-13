import 'package:shared_preferences/shared_preferences.dart';

import '../config/env_config.dart';
import '../constants/storage_keys.dart';

class PreferencesService {
  PreferencesService(this._prefs);

  final SharedPreferences _prefs;

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

  String get themeMode => _prefs.getString(StorageKeys.themeMode) ?? 'dark';

  Future<void> setThemeMode(String mode) =>
      _prefs.setString(StorageKeys.themeMode, mode);

  Future<void> clearSession() async {
    await _prefs.remove(StorageKeys.companyName);
    await _prefs.remove(StorageKeys.username);
    await _prefs.remove(StorageKeys.tenantId);
  }
}
