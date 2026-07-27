import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../storage/preferences_service.dart';
import '../../theme/system_themes.dart';

class ThemeController extends GetxController {
  ThemeController(this._prefs);

  final PreferencesService _prefs;

  final themeMode = ThemeMode.dark.obs;

  (ThemeData light, ThemeData dark) get themes =>
      SystemThemes.forSystem(_prefs.systemType);

  @override
  void onInit() {
    super.onInit();
    themeMode.value =
        _prefs.themeMode == 'light' ? ThemeMode.light : ThemeMode.dark;
  }

  Future<void> setThemeMode(ThemeMode mode) async {
    themeMode.value = mode;
    Get.changeThemeMode(mode);
    await _prefs.setThemeMode(mode == ThemeMode.light ? 'light' : 'dark');
  }

  Future<void> toggle() {
    return setThemeMode(
      themeMode.value == ThemeMode.dark ? ThemeMode.light : ThemeMode.dark,
    );
  }

  void refreshFromPrefs() {
    themeMode.value =
        _prefs.themeMode == 'light' ? ThemeMode.light : ThemeMode.dark;
  }
}
