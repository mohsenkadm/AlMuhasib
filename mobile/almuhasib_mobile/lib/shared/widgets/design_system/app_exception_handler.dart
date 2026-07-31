import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/network/api_exception.dart';
import '../../../shared/models/mobile_models.dart';

/// Centralized API error and conflict presentation.
/// Offline writes are queued by [OfflineWriteService]; reads still require network.
abstract final class AppExceptionHandler {
  static String messageFor(Object? error) {
    if (error == null) return 'error_network'.tr();
    if (error is ApiException) {
      if (error.code != null) return mapApiErrorCode(error.code).tr();
      return error.message;
    }
    final text = error.toString();
    if (text.contains('SocketException') || text.contains('Connection')) {
      return 'offline_action_blocked'.tr();
    }
    return 'error_load'.tr();
  }

  static void showError(Object? error) {
    Get.snackbar(
      'error'.tr(),
      messageFor(error),
      snackPosition: SnackPosition.BOTTOM,
      backgroundColor: AppColors.error,
      colorText: Colors.white,
      margin: const EdgeInsets.all(16),
      borderRadius: 12,
      icon: const Icon(Icons.error_outline, color: Colors.white),
    );
  }

  static void showSuccess(String message) {
    Get.snackbar(
      'success'.tr(),
      message,
      snackPosition: SnackPosition.BOTTOM,
      backgroundColor: AppColors.success,
      colorText: Colors.white,
      margin: const EdgeInsets.all(16),
      borderRadius: 12,
      icon: const Icon(Icons.check_circle_outline, color: Colors.white),
    );
  }

  static void showConflicts(List<String> conflicts, {String? title}) {
    final body = conflicts.where((c) => c.trim().isNotEmpty).join('\n');
    Get.snackbar(
      title ?? 'conflict_title'.tr(),
      body.isEmpty ? 'conflict_generic'.tr() : body,
      snackPosition: SnackPosition.BOTTOM,
      backgroundColor: AppColors.warning,
      colorText: Colors.white,
      margin: const EdgeInsets.all(16),
      borderRadius: 12,
      duration: const Duration(seconds: 6),
      icon: const Icon(Icons.warning_amber_rounded, color: Colors.white),
    );
  }

  static Future<bool> showConfirmDialog({
    required String title,
    required String message,
    String? confirmLabel,
    bool destructive = false,
  }) async {
    final result = await Get.dialog<bool>(
      AlertDialog(
        title: Text(title),
        content: Text(message),
        actions: [
          TextButton(
            onPressed: () => Get.back(result: false),
            child: Text('cancel'.tr()),
          ),
          FilledButton(
            onPressed: () => Get.back(result: true),
            style: destructive
                ? FilledButton.styleFrom(backgroundColor: AppColors.error)
                : null,
            child: Text(confirmLabel ?? 'confirm'.tr()),
          ),
        ],
      ),
    );
    return result ?? false;
  }
}
