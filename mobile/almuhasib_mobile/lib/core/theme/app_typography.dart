import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

abstract final class AppTypography {
  static TextTheme cairo(TextTheme base) {
    return GoogleFonts.cairoTextTheme(base);
  }

  static TextStyle display(BuildContext context) =>
      Theme.of(context).textTheme.headlineMedium!.copyWith(
            fontWeight: FontWeight.w800,
            letterSpacing: -0.5,
          );

  static TextStyle pageTitle(BuildContext context) =>
      Theme.of(context).textTheme.titleLarge!.copyWith(
            fontWeight: FontWeight.w800,
            fontSize: 22,
          );

  static TextStyle sectionTitle(BuildContext context) =>
      Theme.of(context).textTheme.titleLarge!.copyWith(
            fontWeight: FontWeight.w700,
          );

  static TextStyle cardTitle(BuildContext context) =>
      Theme.of(context).textTheme.titleMedium!.copyWith(
            fontWeight: FontWeight.w700,
          );

  static TextStyle bodyMuted(BuildContext context) =>
      Theme.of(context).textTheme.bodyMedium!.copyWith(
            height: 1.45,
          );

  static TextStyle caption(BuildContext context) =>
      Theme.of(context).textTheme.bodySmall!.copyWith(
            fontSize: 12,
            fontWeight: FontWeight.w500,
          );

  static TextStyle kpiValue(BuildContext context) =>
      Theme.of(context).textTheme.titleMedium!.copyWith(
            fontWeight: FontWeight.w800,
            fontSize: 18,
          );

  static TextStyle kpiLabel(BuildContext context) =>
      Theme.of(context).textTheme.bodySmall!.copyWith(
            fontWeight: FontWeight.w600,
            fontSize: 12.5,
          );
}
