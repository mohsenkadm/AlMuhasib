import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../core/config/env_config.dart';
import '../../core/constants/app_colors.dart';
import '../../core/getx/app_services.dart';
import '../../shared/utils/formatters.dart';
import '../../shared/widgets/app_animations.dart';
import '../../shared/widgets/common_widgets.dart';
import '../../shared/widgets/design_system/design_system.dart';

class NotificationsScreen extends StatelessWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final service = AppServices.notifications;

    return Scaffold(
      appBar: AppBar(
        title: Text('notifications_title'.tr()),
        actions: [
          IconButton(
            tooltip: 'mark_all_read'.tr(),
            onPressed: service.markAllRead,
            icon: const Icon(Icons.done_all_rounded),
          ),
          IconButton(
            tooltip: 'clear'.tr(),
            onPressed: () async {
              final ok = await Get.dialog<bool>(
                AlertDialog(
                  title: Text('clear_notifications'.tr()),
                  content: Text('clear_notifications_confirm'.tr()),
                  actions: [
                    TextButton(
                      onPressed: () => Get.back(result: false),
                      child: Text('cancel'.tr()),
                    ),
                    FilledButton(
                      onPressed: () => Get.back(result: true),
                      child: Text('clear'.tr()),
                    ),
                  ],
                ),
              );
              if (ok == true) await service.clearAll();
            },
            icon: const Icon(Icons.delete_outline_rounded),
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
            child: AppEntityCard(
              title: EnvConfig.isOneSignalConfigured
                  ? 'notifications_enabled'.tr()
                  : 'onesignal_not_configured'.tr(),
              subtitle: 'notifications_hint'.tr(),
              leading: Container(
                width: 46,
                height: 46,
                decoration: BoxDecoration(
                  color: (EnvConfig.isOneSignalConfigured
                          ? AppColors.success
                          : AppColors.warning)
                      .withValues(alpha: 0.14),
                  shape: BoxShape.circle,
                ),
                child: Icon(
                  EnvConfig.isOneSignalConfigured
                      ? Icons.notifications_active_rounded
                      : Icons.notifications_off_outlined,
                  color: EnvConfig.isOneSignalConfigured
                      ? AppColors.success
                      : AppColors.warning,
                ),
              ),
              trailing: EnvConfig.isOneSignalConfigured
                  ? TextButton(
                      onPressed: service.requestPermission,
                      child: Text('enable'.tr()),
                    )
                  : null,
            ).fadeSlideIn(),
          ),
          Expanded(
            child: Obx(() {
              final items = service.items.toList(growable: false);
              if (items.isEmpty) {
                return EmptyStateWidget(
                  message: 'no_notifications'.tr(),
                  icon: Icons.notifications_none_rounded,
                );
              }
              return ListView.separated(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
                itemCount: items.length,
                separatorBuilder: (_, __) => const SizedBox(height: 10),
                itemBuilder: (context, index) {
                  final item = items[index];
                  return AppEntityCard(
                    title: item.title == 'notifications_title'
                        ? 'notifications_title'.tr()
                        : item.title,
                    subtitle:
                        '${item.body}\n${formatDate(item.receivedAt)}',
                    status: item.read ? null : 'new'.tr(),
                    statusTone: AppColors.primary,
                    leading: Container(
                      width: 46,
                      height: 46,
                      decoration: BoxDecoration(
                        color: AppColors.primary.withValues(alpha: 0.12),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        item.read
                            ? Icons.notifications_none_rounded
                            : Icons.notifications_rounded,
                        color: AppColors.primary,
                      ),
                    ),
                    onTap: () async {
                      await service.markRead(item.id);
                      final route = item.route;
                      if (route != null && route.isNotEmpty) {
                        Get.toNamed(route);
                      }
                    },
                  ).fadeSlideInList(index: index.clamp(0, 12));
                },
              );
            }),
          ),
        ],
      ),
    );
  }
}
