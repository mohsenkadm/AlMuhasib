import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app.dart';
import 'core/config/env_config.dart';
import 'core/providers/core_providers.dart';
import 'core/services/notification_service.dart';
import 'core/storage/preferences_service.dart';
import 'core/theme/theme_provider.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await EasyLocalization.ensureInitialized();
  await EnvConfig.load();

  final prefs = await PreferencesService.create();
  final container = ProviderContainer(
    overrides: [
      preferencesServiceProvider.overrideWithValue(prefs),
    ],
  );

  final notificationService = container.read(notificationServiceProvider);
  await notificationService.initialize();
  notificationService.setNotificationOpenedHandler((route) {
    // Deep link handling can be extended here.
  });

  runApp(
    UncontrolledProviderScope(
      container: container,
      child: EasyLocalization(
        supportedLocales: const [Locale('ar'), Locale('en')],
        path: 'assets/translations',
        fallbackLocale: const Locale('ar'),
        startLocale: const Locale('ar'),
        child: const AlMuhasibApp(),
      ),
    ),
  );
}
