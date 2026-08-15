import 'package:almuhasib_mobile/core/theme/system_themes.dart';
import 'package:almuhasib_mobile/features/gold_shop/controllers/gold_notifications_controller.dart';
import 'package:almuhasib_mobile/shared/utils/formatters.dart';
import 'package:almuhasib_mobile/shared/widgets/common_widgets.dart';
import 'package:almuhasib_mobile/shared/widgets/design_system/design_system.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

class GoldNotificationsScreen extends GetView<GoldNotificationsController> {
  const GoldNotificationsScreen({super.key});

  @override
  final String? tag = 'gold_notifications';

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('gold_notifications_title'.tr())),
      body: Obx(() {
        if (controller.isLoading.value && controller.items.isEmpty) {
          return const Center(child: CircularProgressIndicator());
        }
        if (controller.error.value != null && controller.items.isEmpty) {
          return ErrorStateWidget(
            message:
                AppExceptionHandler.messageFor(controller.error.value),
            onRetry: controller.load,
          );
        }
        if (controller.items.isEmpty) {
          return EmptyStateWidget(
            message: 'gold_no_notifications'.tr(),
            icon: Icons.notifications_none_rounded,
            onRetry: controller.load,
          );
        }

        return RefreshIndicator(
          onRefresh: controller.load,
          child: ListView.separated(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 40),
            itemCount: controller.items.length,
            separatorBuilder: (_, __) => const SizedBox(height: 10),
            itemBuilder: (context, index) {
              final n = controller.items[index];
              return Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(16),
                  color: Theme.of(context).brightness == Brightness.dark
                      ? SystemThemes.goldDarkCard
                      : Colors.white,
                  border: Border.all(
                    color: n.isRead
                        ? Colors.transparent
                        : SystemThemes.goldPrimary.withValues(alpha: 0.35),
                  ),
                  boxShadow: [
                    BoxShadow(
                      color: SystemThemes.goldPrimary.withValues(alpha: 0.08),
                      blurRadius: 12,
                      offset: const Offset(0, 4),
                    ),
                  ],
                ),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Container(
                      width: 42,
                      height: 42,
                      decoration: BoxDecoration(
                        color: SystemThemes.goldPrimary.withValues(alpha: 0.14),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        n.isRead
                            ? Icons.notifications_outlined
                            : Icons.notifications_active_rounded,
                        color: SystemThemes.goldPrimary,
                        size: 22,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            n.title,
                            style: TextStyle(
                              fontWeight:
                                  n.isRead ? FontWeight.w600 : FontWeight.w800,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            n.message,
                            style: Theme.of(context).textTheme.bodyMedium,
                          ),
                          const SizedBox(height: 6),
                          Text(
                            formatDate(n.createdAt),
                            style: Theme.of(context).textTheme.bodySmall,
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              );
            },
          ),
        );
      }),
    );
  }
}
