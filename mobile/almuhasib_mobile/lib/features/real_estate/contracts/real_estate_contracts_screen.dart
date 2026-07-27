import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../controllers/real_estate_contracts_controller.dart';
import '../models/real_estate_models.dart';
import '../widgets/real_estate_labels.dart';

class RealEstateContractsScreen extends GetView<RealEstateContractsController> {
  const RealEstateContractsScreen({super.key});

  @override
  final String? tag = 'real_estate_contracts';

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => AppListPage<RealEstateContractListItem>(
        title: 'real_estate_contracts_title'.tr(),
        isLoading: controller.isLoading,
        error: controller.error,
        items: controller.items,
        onRefresh: controller.load,
        onRetry: controller.load,
        fabLabel: 'real_estate_new_contract'.tr(),
        onFab: () => Get.toNamed(AppRoutes.realEstateContractNew),
        emptyMessage: 'real_estate_no_contracts'.tr(),
        emptyIcon: Icons.home_work_outlined,
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
          subtitle:
              '${c.buyerName} • ${c.propertySummary.isEmpty ? realEstatePaymentStatusLabel(c.propertyType) : c.propertySummary}\n${formatDate(c.contractDate)}',
          leading: Container(
            width: 46,
            height: 46,
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.12),
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.home_work_outlined,
              color: AppColors.primary,
            ),
          ),
          trailing: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                formatCurrency(c.totalPrice),
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
              ),
              if (c.remainingAmount > 0)
                Text(
                  formatCurrency(c.remainingAmount),
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: Theme.of(context).colorScheme.error,
                        fontWeight: FontWeight.w600,
                      ),
                ),
            ],
          ),
          onTap: () => Get.toNamed(
            AppRoutes.realEstateContractDetailPath(c.syncId),
          ),
        ),
      ),
    );
  }
}
