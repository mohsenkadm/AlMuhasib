import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../controllers/car_contracts_controller.dart';
import '../models/car_models.dart';

class CarContractsScreen extends GetView<CarContractsController> {
  const CarContractsScreen({super.key}) : super(tag: 'car_contracts');

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => AppListPage<CarContractListItem>(
        title: 'car_contracts_title'.tr(),
        isLoading: controller.isLoading,
        error: controller.error,
        items: controller.items,
        onRefresh: controller.load,
        onRetry: controller.load,
        fabLabel: 'car_new_contract'.tr(),
        onFab: () => Get.toNamed(AppRoutes.carContractNew),
        emptyMessage: 'car_no_contracts'.tr(),
        emptyIcon: Icons.description_outlined,
        filterPanel: AppFilterBar(
          onSearchChanged: controller.updateSearch,
          showDateRange: true,
          from: controller.from.value,
          to: controller.to.value,
          onPickFrom: controller.pickFromDate,
          onPickTo: controller.pickToDate,
          filterChips: [
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
          onFilterSelected: controller.updateStatusFilter,
          onClear: controller.clearFilters,
        ),
        itemBuilder: (context, c, index) => AppEntityCard(
          title: c.contractNumber,
          subtitle: '${c.buyerName} • ${c.plateNumber}',
          trailing: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                formatCurrency(c.carPrice),
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
              ),
              Text(
                formatCurrency(c.remainingAmount),
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ),
          onTap: () => Get.toNamed(
            AppRoutes.carContractDetailPath(c.syncId),
          ),
        ).fadeSlideIn(delayMs: index * 40),
      ),
    );
  }
}
