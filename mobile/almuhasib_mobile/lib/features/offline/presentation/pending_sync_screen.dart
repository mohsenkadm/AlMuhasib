import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/offline/offline_write_queue.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/design_system/design_system.dart';

class PendingSyncController extends GetxController {
  OfflineWriteService get queue => AppServices.offlineQueue;
  final flushing = false.obs;

  Future<void> flush() async {
    flushing.value = true;
    try {
      final n = await queue.flush();
      if (n > 0) {
        AppExceptionHandler.showSuccess('offline_synced_count'.tr(args: ['$n']));
      } else if (queue.pending.isEmpty) {
        AppExceptionHandler.showSuccess('offline_queue_empty'.tr());
      }
    } catch (e) {
      AppExceptionHandler.showError(e);
    } finally {
      flushing.value = false;
    }
  }

  Future<void> remove(String id) async {
    await queue.remove(id);
  }
}

class PendingSyncScreen extends GetView<PendingSyncController> {
  const PendingSyncScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: 'pending_sync'.tr(),
      subtitle: 'pending_sync_subtitle'.tr(),
      actions: [
        Obx(
          () => IconButton(
            tooltip: 'retry_all'.tr(),
            onPressed: controller.flushing.value ? null : controller.flush,
            icon: controller.flushing.value
                ? const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const Icon(Icons.cloud_upload_outlined),
          ),
        ),
      ],
      body: Obx(() {
        final items = controller.queue.pending.toList();
        if (items.isEmpty) {
          return Center(child: Text('offline_queue_empty'.tr()));
        }
        return ListView.separated(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 120),
          itemCount: items.length,
          separatorBuilder: (_, __) => const SizedBox(height: 10),
          itemBuilder: (context, index) {
            final w = items[index];
            return AppEntityCard(
              title: w.operationType,
              subtitle:
                  '${formatDate(w.createdAt)} • ${w.status.name}${w.lastError != null ? '\n${w.lastError}' : ''}',
              leading: Icon(
                w.status == PendingWriteStatus.failed
                    ? Icons.error_outline
                    : Icons.cloud_queue_outlined,
                color: w.status == PendingWriteStatus.failed
                    ? AppColors.error
                    : AppColors.primary,
              ),
              trailing: IconButton(
                icon: const Icon(Icons.delete_outline),
                onPressed: () => controller.remove(w.id),
              ),
            );
          },
        );
      }),
    );
  }
}

class QuickActionsScreen extends StatelessWidget {
  const QuickActionsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final actions = [
      _Action('new_invoice'.tr(), Icons.receipt_long_rounded, AppColors.moduleOrange,
          AppRoutes.invoiceNew),
      _Action('new_voucher'.tr(), Icons.payments_outlined, AppColors.primary,
          AppRoutes.voucherNew),
      _Action('new_expense'.tr(), Icons.money_off_outlined, AppColors.error,
          AppRoutes.expenseNew),
      _Action(
          'new_warehouse_transfer'.tr(),
          Icons.move_up_rounded,
          AppColors.moduleIndigo,
          AppRoutes.warehouseTransferNew),
      _Action('pay_installment'.tr(), Icons.event_available_outlined,
          AppColors.warning, AppRoutes.installments),
      _Action('new_transfer'.tr(), Icons.swap_horiz_rounded, AppColors.moduleCyan,
          AppRoutes.transferNew),
    ];

    return AppPageScaffold(
      title: 'quick_actions'.tr(),
      subtitle: 'quick_actions_subtitle'.tr(),
      body: ListView.separated(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 120),
        itemCount: actions.length,
        separatorBuilder: (_, __) => const SizedBox(height: 10),
        itemBuilder: (context, index) {
          final a = actions[index];
          return AppEntityCard(
            title: a.title,
            leading: Container(
              width: 44,
              height: 44,
              decoration: BoxDecoration(
                color: a.color.withValues(alpha: 0.14),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(a.icon, color: a.color),
            ),
            onTap: () => Get.toNamed(a.route),
          );
        },
      ),
    );
  }
}

class _Action {
  const _Action(this.title, this.icon, this.color, this.route);
  final String title;
  final IconData icon;
  final Color color;
  final String route;
}
