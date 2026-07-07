import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../controllers/car_trade_report_controller.dart';
import '../models/car_trade_models.dart';

class CarTradeReportScreen extends GetView<CarTradeReportController> {
  const CarTradeReportScreen({super.key});

  @override
  final String? tag = 'car_trade_report';

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: 'car_trade_report_title'.tr(),
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
                FilterChipOption(id: 'Buy', label: 'car_trade_type_buy'.tr()),
                FilterChipOption(id: 'Sell', label: 'car_trade_type_sell'.tr()),
                FilterChipOption(
                  id: 'Active',
                  label: 'filter_status_active'.tr(),
                ),
                FilterChipOption(
                  id: 'Completed',
                  label: 'filter_status_completed'.tr(),
                ),
              ],
              onFilterSelected: (id) {
                if (id == 'Buy' || id == 'Sell') {
                  controller.updateTradeTypeFilter(id);
                } else {
                  controller.updateStatusFilter(id);
                }
              },
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
                title: 'car_trade_report_total'.tr(),
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
              return AppAsyncBody<List<CarTradeTransactionListItem>>(
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
                        title: r.transactionNumber,
                        subtitle: formatDate(r.transactionDate),
                        trailing: Text(formatCurrency(r.totalAmount)),
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
