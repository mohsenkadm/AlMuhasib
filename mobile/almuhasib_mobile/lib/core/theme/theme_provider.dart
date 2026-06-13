import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../storage/preferences_service.dart';
import 'app_theme.dart';

final themeModeProvider =
    StateNotifierProvider<ThemeModeNotifier, ThemeMode>((ref) {
  final prefs = ref.watch(preferencesServiceProvider);
  return ThemeModeNotifier(prefs);
});

class ThemeModeNotifier extends StateNotifier<ThemeMode> {
  ThemeModeNotifier(this._prefs)
      : super(
          _prefs.themeMode == 'light' ? ThemeMode.light : ThemeMode.dark,
        );

  final PreferencesService _prefs;

  Future<void> setThemeMode(ThemeMode mode) async {
    state = mode;
    await _prefs.setThemeMode(mode == ThemeMode.light ? 'light' : 'dark');
  }

  Future<void> toggle() {
    return setThemeMode(
      state == ThemeMode.dark ? ThemeMode.light : ThemeMode.dark,
    );
  }
}

final preferencesServiceProvider = Provider<PreferencesService>((ref) {
  throw UnimplementedError('PreferencesService must be overridden in main');
});

final appThemeProvider = Provider<(ThemeData light, ThemeData dark)>((ref) {
  return (AppTheme.light(), AppTheme.dark());
});
