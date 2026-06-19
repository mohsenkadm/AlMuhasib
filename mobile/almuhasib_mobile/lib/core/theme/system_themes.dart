import 'package:flutter/material.dart';

import '../config/application_system_type.dart';
import '../config/system_profile.dart';
import 'app_theme.dart';

abstract final class SystemThemes {
  static (ThemeData light, ThemeData dark) forSystem(ApplicationSystemType type) {
    final profile = SystemProfile.of(type);
    return (
      AppTheme.light(
        seedColor: profile.primary,
        accentColor: profile.accent,
      ),
      AppTheme.dark(
        seedColor: profile.primary,
        accentColor: profile.accent,
      ),
    );
  }
}
