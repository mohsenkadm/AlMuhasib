import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:flutter_native_splash/flutter_native_splash.dart';
import 'package:get/get.dart';
import 'app.dart';
import 'core/config/env_config.dart';
import 'core/getx/app_services.dart';
import 'core/storage/preferences_service.dart';

Future<void> main() async {
  final widgetsBinding = WidgetsFlutterBinding.ensureInitialized();
  FlutterNativeSplash.preserve(widgetsBinding: widgetsBinding);

  await EasyLocalization.ensureInitialized();
  await EnvConfig.load();

  final prefs = await PreferencesService.create();
  await AppServices.init(prefs);

  await AppServices.notifications.initialize();
  AppServices.notifications.setNotificationOpenedHandler((route) {
    if (route != null && route.isNotEmpty) Get.toNamed(route);
  });

  runApp(
    EasyLocalization(
      supportedLocales: const [Locale('ar'), Locale('en')],
      path: 'assets/translations',
      fallbackLocale: const Locale('ar'),
      startLocale: const Locale('ar'),
      child: const AlMuhasibApp(),
    ),
  );
}
