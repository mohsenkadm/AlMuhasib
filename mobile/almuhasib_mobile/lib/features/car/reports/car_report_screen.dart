import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/car_report_controller.dart';
import '../models/car_models.dart';

class CarReportScreen extends StatelessWidget {
  const CarReportScreen({super.key});

  Future<void> _pickFrom(
    BuildContext context,
    CarReportController controller,
  ) async {
    final d = await showDatePicker(
      context: context,
      initialDate: controller.from.value,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (d != null) controller.setFrom(d);
  }

  Future<void> _pickTo(
    BuildContext context,
    CarReportController controller,
  ) async {
    final d = await showDatePicker(
      context: context,
      initialDate: controller.to.value,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (d != null) controller.setTo(d);
  }

  @override
  Widget build(BuildContext context) {
    final controller = Get.put(CarReportController(), tag: 'car_report');

    return AppPageScaffold(
      title: 'car_report_title'.tr(),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(20),
            child: Obx(
              () => Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () => _pickFrom(context, controller),
                      icon: const Icon(Icons.date_range_rounded),
                      label: Text(formatDate(controller.from.value)),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () => _pickTo(context, controller),
                      icon: const Icon(Icons.event_rounded),
                      label: Text(formatDate(controller.to.value)),
                    ),
                  ),
                ],
              ),
            ),
          ),
          Obx(() {
            if (controller.isLoading.value) {
              return const LinearProgressIndicator(minHeight: 3);
            }
            return Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20),
              child: AppEntityCard(
                title: 'car_report_total'.tr(),
                trailing: Text(
                  formatCurrency(controller.total),
                  style: Theme.of(context).textTheme.titleLarge?.copyWith(
                        fontWeight: FontWeight.w800,
                      ),
                ),
              ),
            );
          }),
          Expanded(
            child: Obx(() {
              return AppAsyncBody<List<CarContractListItem>>(
                isLoading: controller.isLoading.value,
                error: controller.error.value,
                data: controller.rows,
                onRetry: controller.load,
                showEmptyWhenNull: false,
                builder: (context, rows) {
                  if (rows.isEmpty) {
                    return ListView(
                      children: [
                        SizedBox(
                          height: MediaQuery.sizeOf(context).height * 0.35,
                          child: EmptyStateWidget(
                            message: 'no_data'.tr(),
                            onRetry: controller.load,
                          ),
                        ),
                      ],
                    );
                  }
                  return ListView.builder(
                    padding: const EdgeInsets.fromLTRB(20, 12, 20, 120),
                    itemCount: rows.length,
                    itemBuilder: (context, i) {
                      final r = rows[i];
                      return AppEntityCard(
                        title: r.contractNumber,
                        subtitle: formatDate(r.contractDate),
                        trailing: Text(formatCurrency(r.carPrice)),
                      ).fadeSlideIn(delayMs: i * 30);
                    },
                  );
                },
              );
            }),
          ),
        ],
      ),
    );
  }
}
