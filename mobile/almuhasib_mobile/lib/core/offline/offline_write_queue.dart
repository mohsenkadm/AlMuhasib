import 'dart:convert';

import 'package:easy_localization/easy_localization.dart';
import 'package:get/get.dart' hide Trans;
import 'package:shared_preferences/shared_preferences.dart';
import 'package:uuid/uuid.dart';

import '../getx/app_services.dart';
import '../network/api_client.dart';
import '../network/api_exception.dart';
import '../../shared/models/mobile_models.dart';

const _queueKey = 'pending_mobile_writes';

enum PendingWriteStatus { pending, syncing, failed }

class PendingWrite {
  PendingWrite({
    required this.id,
    required this.operationType,
    required this.path,
    required this.method,
    required this.bodyJson,
    required this.clientSyncId,
    required this.createdAt,
    this.status = PendingWriteStatus.pending,
    this.retryCount = 0,
    this.lastError,
  });

  factory PendingWrite.fromJson(Map<String, dynamic> json) {
    return PendingWrite(
      id: json['id'] as String? ?? '',
      operationType: json['operationType'] as String? ?? '',
      path: json['path'] as String? ?? '',
      method: json['method'] as String? ?? 'POST',
      bodyJson: Map<String, dynamic>.from(json['bodyJson'] as Map? ?? {}),
      clientSyncId: json['clientSyncId'] as String? ?? '',
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ??
          DateTime.now(),
      status: PendingWriteStatus.values.firstWhere(
        (e) => e.name == (json['status'] as String? ?? 'pending'),
        orElse: () => PendingWriteStatus.pending,
      ),
      retryCount: json['retryCount'] as int? ?? 0,
      lastError: json['lastError'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'operationType': operationType,
        'path': path,
        'method': method,
        'bodyJson': bodyJson,
        'clientSyncId': clientSyncId,
        'createdAt': createdAt.toIso8601String(),
        'status': status.name,
        'retryCount': retryCount,
        if (lastError != null) 'lastError': lastError,
      };

  final String id;
  final String operationType;
  final String path;
  final String method;
  final Map<String, dynamic> bodyJson;
  final String clientSyncId;
  final DateTime createdAt;
  PendingWriteStatus status;
  int retryCount;
  String? lastError;
}

class OfflineWriteService extends GetxService {
  OfflineWriteService(this._prefs);

  final SharedPreferences _prefs;
  final _uuid = const Uuid();
  final pending = <PendingWrite>[].obs;
  final isFlushing = false.obs;
  Worker? _connectivityWorker;

  int get pendingCount =>
      pending.where((w) => w.status != PendingWriteStatus.syncing).length;

  @override
  void onInit() {
    super.onInit();
    _load();
    _connectivityWorker = ever<bool>(
      AppServices.connectivity.isOffline,
      (offline) {
        if (!offline) flush();
      },
    );
  }

  @override
  void onClose() {
    _connectivityWorker?.dispose();
    super.onClose();
  }

  void _load() {
    final raw = _prefs.getStringList(_queueKey) ?? const [];
    pending.assignAll(
      raw
          .map((e) {
            try {
              return PendingWrite.fromJson(
                jsonDecode(e) as Map<String, dynamic>,
              );
            } catch (_) {
              return null;
            }
          })
          .whereType<PendingWrite>()
          .toList(),
    );
  }

  Future<void> _persist() async {
    await _prefs.setStringList(
      _queueKey,
      pending.map((e) => jsonEncode(e.toJson())).toList(),
    );
  }

  Future<MobileWriteResponse> enqueue({
    required String operationType,
    required String path,
    required Map<String, dynamic> body,
    String method = 'POST',
    String? clientSyncId,
  }) async {
    final syncId = clientSyncId ??
        (body['syncId'] as String?) ??
        _uuid.v4();
    final payload = Map<String, dynamic>.from(body);
    payload['syncId'] = syncId;

    final write = PendingWrite(
      id: _uuid.v4(),
      operationType: operationType,
      path: path,
      method: method,
      bodyJson: payload,
      clientSyncId: syncId,
      createdAt: DateTime.now(),
    );
    pending.add(write);
    await _persist();

    return MobileWriteResponse(
      syncId: syncId,
      message: 'offline_queued'.tr(),
    );
  }

  Future<void> remove(String id) async {
    pending.removeWhere((w) => w.id == id);
    await _persist();
  }

  Future<void> clearFailed() async {
    pending.removeWhere((w) => w.status == PendingWriteStatus.failed);
    await _persist();
  }

  Future<int> flush() async {
    if (isFlushing.value) return 0;
    if (AppServices.connectivity.isOffline.value) return 0;

    final items = pending
        .where((w) => w.status != PendingWriteStatus.syncing)
        .toList();
    if (items.isEmpty) return 0;

    isFlushing.value = true;
    var synced = 0;
    final api = Get.find<ApiClient>();
    api.updateBaseUrl();

    try {
      for (final write in List<PendingWrite>.from(items)) {
        write.status = PendingWriteStatus.syncing;
        pending.refresh();
        try {
          if (write.method.toUpperCase() == 'PUT') {
            await api.put(
              write.path,
              data: write.bodyJson,
              parser: (data) =>
                  MobileWriteResponse.fromJson(data as Map<String, dynamic>),
            );
          } else if (write.method.toUpperCase() == 'DELETE') {
            await api.delete(
              write.path,
              parser: (data) =>
                  MobileWriteResponse.fromJson(data as Map<String, dynamic>),
            );
          } else {
            await api.post(
              write.path,
              data: write.bodyJson,
              parser: (data) =>
                  MobileWriteResponse.fromJson(data as Map<String, dynamic>),
            );
          }
          pending.removeWhere((w) => w.id == write.id);
          synced++;
          await _persist();
        } catch (e) {
          write.status = PendingWriteStatus.failed;
          write.retryCount += 1;
          write.lastError = e is ApiException ? e.message : e.toString();
          pending.refresh();
          await _persist();
        }
      }
    } finally {
      isFlushing.value = false;
    }
    return synced;
  }

  bool get isOnline => !AppServices.connectivity.isOffline.value;

  bool isNetworkError(Object e) {
    if (e is ApiException) {
      final msg = e.message.toLowerCase();
      return e.statusCode == null ||
          msg.contains('socket') ||
          msg.contains('connection') ||
          msg.contains('network') ||
          msg.contains('timed out') ||
          msg.contains('timeout');
    }
    final text = e.toString().toLowerCase();
    return text.contains('socket') ||
        text.contains('connection') ||
        text.contains('network') ||
        text.contains('timeout');
  }
}

extension OfflineWriteHelpers on OfflineWriteService {
  static OfflineWriteService get instance => Get.find<OfflineWriteService>();
}
