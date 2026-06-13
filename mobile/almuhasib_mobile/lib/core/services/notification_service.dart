import 'dart:io';

import 'package:device_info_plus/device_info_plus.dart';
import 'package:flutter/foundation.dart';
import 'package:onesignal_flutter/onesignal_flutter.dart';

import '../config/env_config.dart';
import '../network/api_client.dart';
import '../../shared/models/auth_models.dart';

class NotificationService {
  NotificationService(this._apiClient);

  final ApiClient _apiClient;
  bool _initialized = false;

  Future<void> initialize() async {
    if (!EnvConfig.isOneSignalConfigured || kIsWeb) return;
    if (_initialized) return;

    OneSignal.Debug.setLogLevel(OSLogLevel.warn);
    OneSignal.initialize(EnvConfig.oneSignalAppId);
    await OneSignal.Notifications.requestPermission(true);
    _initialized = true;
  }

  Future<void> registerDeviceWithApi() async {
    if (!EnvConfig.isOneSignalConfigured || kIsWeb) return;

    final playerId = OneSignal.User.pushSubscription.id;
    if (playerId == null || playerId.isEmpty) return;

    final deviceInfo = DeviceInfoPlugin();
    String? deviceName;
    String platform;

    if (Platform.isAndroid) {
      final info = await deviceInfo.androidInfo;
      deviceName = '${info.brand} ${info.model}';
      platform = 'android';
    } else if (Platform.isIOS) {
      final info = await deviceInfo.iosInfo;
      deviceName = info.utsname.machine;
      platform = 'ios';
    } else {
      platform = 'unknown';
    }

    await _apiClient.postVoid(
      '/api/devices/register',
      data: RegisterDeviceRequest(
        playerId: playerId,
        deviceName: deviceName,
        platform: platform,
      ).toJson(),
    );
  }

  void setNotificationOpenedHandler(void Function(String? route) handler) {
    if (!EnvConfig.isOneSignalConfigured || kIsWeb) return;
    OneSignal.Notifications.addClickListener((event) {
      final data = event.notification.additionalData;
      handler(data?['route'] as String?);
    });
  }
}
