import 'package:flutter/material.dart';

abstract final class AppColors {
  static const primary = Color(0xFF0D47A1);
  static const primaryLight = Color(0xFF1565C0);
  static const accent = Color(0xFF00ACC1);
  static const accentGlow = Color(0x5900ACC1);

  static const surfaceDark = Color(0xFF070B14);
  static const surfaceDarkCard = Color(0xFF111827);
  static const surfaceLight = Color(0xFFF8FAFC);
  static const surfaceLightCard = Color(0xFFFFFFFF);

  static const textPrimary = Color(0xFFF0F4FC);
  static const textMuted = Color(0xFF94A3B8);
  static const textDark = Color(0xFF0F172A);
  static const textDarkMuted = Color(0xFF64748B);

  static const success = Color(0xFF10B981);
  static const warning = Color(0xFFF59E0B);
  static const error = Color(0xFFEF4444);

  static const cardRadius = 16.0;

  static const primaryGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [primaryLight, accent],
  );

  static const splashGradient = LinearGradient(
    begin: Alignment.topCenter,
    end: Alignment.bottomCenter,
    colors: [primary, accent],
  );
}
