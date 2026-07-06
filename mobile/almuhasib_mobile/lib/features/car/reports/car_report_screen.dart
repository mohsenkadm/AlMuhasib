import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../controllers/car_report_controller.dart';
import '../models/car_models.dart';

class CarReportScreen extends GetView<CarReportController> {
  const CarReportScreen({super.key});

  @override
  final String? tag = 'car_report';

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: 'car_report_title'.tr(),
      body: Column(
        children: [
          Obx(
            () => AppFilterBar(
              showDateRange: true,
              from: controller.from.value,
              to: controller.to.value,
              onPickFrom: controller.pickFromDate,
              onPickTo: controller.pickToDate,
              filterChips: [
                FilterChipOption(
                  id: 'Active',
                  label: 'filter_status_active'.tr(),
                ),
                FilterChipOption(
                  id: 'Completed',
                  label: 'filter_status_completed'.tr(),
                ),
              ],
              onFilterSelected: controller.updateStatusFilter,
              onClear: controller.clearFilters,
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
