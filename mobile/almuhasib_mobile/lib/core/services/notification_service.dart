import 'dart:io';

import 'package:device_info_plus/device_info_plus.dart';
import 'package:flutter/foundation.dart';
import 'package:get/get.dart';
import 'package:onesignal_flutter/onesignal_flutter.dart';
import 'package:uuid/uuid.dart';

import '../config/env_config.dart';
import '../network/api_client.dart';
import '../storage/preferences_service.dart';
import '../../shared/models/auth_models.dart';
import '../../shared/models/local_notification_item.dart';

/// OneSignal push + local inbox. Follows
/// https://documentation.onesignal.com/docs/en/flutter-sdk-setup
class NotificationService extends GetxController {
  NotificationService(this._apiClient, this._prefs);

  final ApiClient _apiClient;
  final PreferencesService _prefs;
  bool _initialized = false;

  final items = <LocalNotificationItem>[].obs;

  int get unreadCount => items.where((e) => !e.read).length;

  @override
  void onInit() {
    super.onInit();
    _loadInbox();
  }

  void _loadInbox() {
    items.assignAll(
      _prefs.notificationInboxJson
          .map(LocalNotificationItem.tryDecode)
          .whereType<LocalNotificationItem>()
          .toList(),
    );
  }

  Future<void> _persistInbox() async {
    await _prefs.setNotificationInboxJson(
      items.take(50).map((e) => e.encode()).toList(),
    );
  }

  Future<void> saveIncoming({
    required String title,
    required String body,
    String? route,
  }) async {
    final item = LocalNotificationItem(
      id: const Uuid().v4(),
      title: title.isEmpty ? 'notifications_title' : title,
      body: body,
      receivedAt: DateTime.now(),
      route: route,
    );
    items.insert(0, item);
    if (items.length > 50) {
      items.removeRange(50, items.length);
    }
    await _persistInbox();
  }

  Future<void> markAllRead() async {
    items.assignAll(items.map((e) => e.copyWith(read: true)));
    await _persistInbox();
  }

  Future<void> markRead(String id) async {
    final index = items.indexWhere((e) => e.id == id);
    if (index < 0) return;
    items[index] = items[index].copyWith(read: true);
    items.refresh();
    await _persistInbox();
  }

  Future<void> clearAll() async {
    items.clear();
    await _persistInbox();
  }

  Future<void> initialize() async {
    if (!EnvConfig.isOneSignalConfigured || kIsWeb) return;
    if (_initialized) return;

    // Verbose in debug helps verify FCM/APNs; warn in release.
    OneSignal.Debug.setLogLevel(
      kDebugMode ? OSLogLevel.verbose : OSLogLevel.warn,
    );

    OneSignal.initialize(EnvConfig.oneSignalAppId);

    // Docs recommend `false` so In-App Messages can prompt later;
    // we still request once so the device can register for push.
    await OneSignal.Notifications.requestPermission(false);

    OneSignal.Notifications.addForegroundWillDisplayListener((event) {
      final n = event.notification;
      saveIncoming(
        title: n.title ?? '',
        body: n.body ?? '',
        route: n.additionalData?['route'] as String?,
      );
      event.notification.display();
    });

    _initialized = true;

    final username = _prefs.username;
    final tenantId = _prefs.tenantId;
    if (username != null && username.isNotEmpty && tenantId != null) {
      await loginExternalUser('$tenantId:$username');
    }
  }

  /// Unifies the same user across devices (OneSignal External ID).
  Future<void> loginExternalUser(String externalId) async {
    if (!EnvConfig.isOneSignalConfigured || kIsWeb || externalId.isEmpty) {
      return;
    }
    await OneSignal.login(externalId);
  }

  Future<void> logoutExternalUser() async {
    if (!EnvConfig.isOneSignalConfigured || kIsWeb) return;
    await OneSignal.logout();
  }

  Future<void> requestPermission() async {
    if (!EnvConfig.isOneSignalConfigured || kIsWeb) return;
    await OneSignal.Notifications.requestPermission(true);
  }

  Future<void> registerDeviceWithApi() async {
    if (!EnvConfig.isOneSignalConfigured || kIsWeb) return;

    final username = _prefs.username;
    final tenantId = _prefs.tenantId;
    if (username != null && username.isNotEmpty && tenantId != null) {
      await loginExternalUser('$tenantId:$username');
    }

    // Subscription id can arrive slightly after permission grant.
    String? playerId = OneSignal.User.pushSubscription.id;
    if (playerId == null || playerId.isEmpty) {
      await Future<void>.delayed(const Duration(milliseconds: 800));
      playerId = OneSignal.User.pushSubscription.id;
    }
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
      final n = event.notification;
      final route = n.additionalData?['route'] as String?;
      saveIncoming(
        title: n.title ?? '',
        body: n.body ?? '',
        route: route,
      );
      handler(route);
    });
  }
}
