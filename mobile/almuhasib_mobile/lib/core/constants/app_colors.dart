import 'package:flutter/material.dart';

abstract final class AppColors {
  /// Deep professional blue matching the reference UI.
  static const primary = Color(0xFF0448A4);
  static const primaryLight = Color(0xFF1565C0);
  static const primaryDark = Color(0xFF033A86);
  static const accent = Color(0xFF00ACC1);
  static const accentGlow = Color(0x5900ACC1);

  static const surfaceDark = Color(0xFF070B14);
  static const surfaceDarkCard = Color(0xFF111827);
  static const surfaceLight = Color(0xFFF5F7FB);
  static const surfaceLightCard = Color(0xFFFFFFFF);

  static const textPrimary = Color(0xFFF0F4FC);
  static const textMuted = Color(0xFF94A3B8);
  static const textDark = Color(0xFF0F172A);
  static const textDarkMuted = Color(0xFF64748B);

  static const success = Color(0xFF10B981);
  static const warning = Color(0xFFF59E0B);
  static const error = Color(0xFFEF4444);

  /// Module / icon accent palette from the reference.
  static const moduleGreen = Color(0xFF22C55E);
  static const modulePurple = Color(0xFF8B5CF6);
  static const moduleOrange = Color(0xFFF97316);
  static const moduleCyan = Color(0xFF06B6D4);
  static const modulePink = Color(0xFFEC4899);
  static const moduleIndigo = Color(0xFF6366F1);

  static const cardRadius = 16.0;

  static List<BoxShadow> cardShadow({bool dark = false}) => [
        BoxShadow(
          color: (dark ? Colors.black : primary).withValues(alpha: dark ? 0.35 : 0.08),
          blurRadius: 18,
          offset: const Offset(0, 8),
        ),
      ];

  static const brandNavy = Color(0xFF051020);
  static const brandNavyMid = Color(0xFF0A1A35);
  static const brandAccent = Color(0xFF1E5BB6);

  static const primaryGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [primaryLight, primary],
  );

  static const heroGradient = LinearGradient(
    begin: Alignment.topRight,
    end: Alignment.bottomLeft,
    colors: [Color(0xFF1565C0), Color(0xFF0448A4), Color(0xFF033A86)],
  );

  /// Matches Qayd icon background atmosphere.
  static const splashGradient = RadialGradient(
    center: Alignment(0, -0.15),
    radius: 1.15,
    colors: [
      Color(0xFF122445),
      brandNavyMid,
      brandNavy,
      Color(0xFF02060F),
    ],
    stops: [0.0, 0.35, 0.72, 1.0],
  );
}
