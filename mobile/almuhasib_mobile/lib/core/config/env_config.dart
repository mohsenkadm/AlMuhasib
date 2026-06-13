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

  static String defaultApiUrl() {
    if (kIsWeb) {
      return dotenv.env['DEFAULT_API_URL_OTHER'] ?? 'http://localhost:5265';
    }
    if (Platform.isAndroid) {
      return dotenv.env['DEFAULT_API_URL_ANDROID'] ?? 'http://10.0.2.2:5265';
    }
    if (Platform.isIOS) {
      return dotenv.env['DEFAULT_API_URL_IOS'] ?? 'http://127.0.0.1:5265';
    }
    return dotenv.env['DEFAULT_API_URL_OTHER'] ?? 'http://localhost:5265';
  }
}
