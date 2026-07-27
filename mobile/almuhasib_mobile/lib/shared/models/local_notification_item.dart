import 'dart:convert';

class LocalNotificationItem {
  LocalNotificationItem({
    required this.id,
    required this.title,
    required this.body,
    required this.receivedAt,
    this.route,
    this.read = false,
  });

  factory LocalNotificationItem.fromJson(Map<String, dynamic> json) {
    return LocalNotificationItem(
      id: json['id'] as String? ?? '',
      title: json['title'] as String? ?? '',
      body: json['body'] as String? ?? '',
      receivedAt: DateTime.tryParse(json['receivedAt'] as String? ?? '') ??
          DateTime.now(),
      route: json['route'] as String?,
      read: json['read'] as bool? ?? false,
    );
  }

  final String id;
  final String title;
  final String body;
  final DateTime receivedAt;
  final String? route;
  final bool read;

  LocalNotificationItem copyWith({bool? read}) {
    return LocalNotificationItem(
      id: id,
      title: title,
      body: body,
      receivedAt: receivedAt,
      route: route,
      read: read ?? this.read,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'title': title,
        'body': body,
        'receivedAt': receivedAt.toIso8601String(),
        'route': route,
        'read': read,
      };

  String encode() => jsonEncode(toJson());

  static LocalNotificationItem? tryDecode(String raw) {
    try {
      return LocalNotificationItem.fromJson(
        jsonDecode(raw) as Map<String, dynamic>,
      );
    } catch (_) {
      return null;
    }
  }
}
