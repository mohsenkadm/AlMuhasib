import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';

abstract final class EnvConfig {
  static Future<void> load() async {
    await dotenv.load(fileName: '.env');
  }

  static String get oneSignalAppId =>
      dotenv.env['ONESIGNAL_APP_ID'] ?? 'your-onesignal-app-id';

  static bool get isOneSignalConfigured =>
      oneSignalAppId.isNotEmpty && oneSignalAppId != 'your-onesignal-app-id';

  static const String productionApiUrl =
      'https://mohsenkadmapple-001-site1.dtempurl.com';

  static String defaultApiUrl() {
    if (kIsWeb) {
      return dotenv.env['DEFAULT_API_URL_OTHER'] ?? productionApiUrl;
    }
    if (Platform.isAndroid) {
      return dotenv.env['DEFAULT_API_URL_ANDROID'] ?? productionApiUrl;
    }
    if (Platform.isIOS) {
      return dotenv.env['DEFAULT_API_URL_IOS'] ?? productionApiUrl;
    }
    return dotenv.env['DEFAULT_API_URL_OTHER'] ?? productionApiUrl;
  }
}
