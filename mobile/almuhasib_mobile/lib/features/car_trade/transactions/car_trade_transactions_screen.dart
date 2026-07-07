import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../controllers/car_trade_transactions_controller.dart';
import '../models/car_trade_models.dart';

class CarTradeTransactionsScreen extends GetView<CarTradeTransactionsController> {
  const CarTradeTransactionsScreen({super.key});

  @override
  final String? tag = 'car_trade_transactions';

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => AppListPage<CarTradeTransactionListItem>(
        title: 'car_trade_transactions_title'.tr(),
        isLoading: controller.isLoading,
        error: controller.error,
        items: controller.items,
        onRefresh: controller.load,
        onRetry: controller.load,
        fabLabel: 'car_trade_new_transaction'.tr(),
        onFab: () => Get.toNamed(AppRoutes.carTradeTransactionNew),
        emptyMessage: 'car_trade_no_transactions'.tr(),
        emptyIcon: Icons.swap_horiz_outlined,
        filterPanel: AppFilterBar(
          onSearchChanged: controller.updateSearch,
          showDateRange: true,
          from: controller.from.value,
          to: controller.to.value,
          onPickFrom: controller.pickFromDate,
          onPickTo: controller.pickToDate,
          filterChips: [
            FilterChipOption(id: 'Buy', label: 'car_trade_type_buy'.tr()),
            FilterChipOption(id: 'Sell', label: 'car_trade_type_sell'.tr()),
            FilterChipOption(id: 'Active', label: 'filter_status_active'.tr()),
            FilterChipOption(
              id: 'Completed',
              label: 'filter_status_completed'.tr(),
            ),
            FilterChipOption(
              id: 'Cancelled',
              label: 'filter_status_cancelled'.tr(),
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
        itemBuilder: (context, t, index) => AppEntityCard(
          title: t.transactionNumber,
          subtitle: '${t.tradeType} • ${t.plateNumber}',
          trailing: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                formatCurrency(t.totalAmount),
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
              ),
              Text(
                formatCurrency(t.remainingAmount),
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ),
          onTap: () => Get.toNamed(
            AppRoutes.carTradeTransactionDetailPath(t.syncId),
          ),
        ).fadeSlideIn(delayMs: index * 40),
      ),
    );
  }
}
