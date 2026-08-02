import 'package:flutter/material.dart';

import '../config/application_system_type.dart';
import '../config/system_profile.dart';
import 'app_theme.dart';

abstract final class SystemThemes {
  /// Warm gold palette for the gold shop system.
  static const goldPrimary = Color(0xFFB8860B);
  static const goldSecondary = Color(0xFFD4AF37);
  static const goldAccent = Color(0xFF8B6914);
  static const goldDarkSurface = Color(0xFF2C2416);
  static const goldDarkCard = Color(0xFF3D3220);

  static (ThemeData light, ThemeData dark) forSystem(ApplicationSystemType type) {
    if (type == ApplicationSystemType.goldShop) {
      return (
        AppTheme.light(
          seedColor: goldPrimary,
          accentColor: goldSecondary,
        ),
        AppTheme.dark(
          seedColor: goldPrimary,
          accentColor: goldSecondary,
        ),
      );
    }

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
