import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/real_estate_payments_controller.dart';
import '../models/real_estate_models.dart';

class RealEstatePaymentsScreen extends GetView<RealEstatePaymentsController> {
  const RealEstatePaymentsScreen({super.key});

  @override
  final String? tag = 'real_estate_payments';

  @override
  Widget build(BuildContext context) {
    return AppListPage<RealEstateContractListItem>(
      title: 'real_estate_payments_title'.tr(),
      isLoading: controller.isLoading,
      error: controller.error,
      items: controller.unpaid,
      onRefresh: controller.load,
      onRetry: controller.load,
      emptyMessage: 'real_estate_no_unpaid'.tr(),
      emptyIcon: Icons.payments_outlined,
      itemBuilder: (context, c, index) => AppEntityCard(
        title: c.contractNumber,
        subtitle:
            '${c.buyerName}\n${c.propertySummary.isEmpty ? c.propertyType : c.propertySummary}',
        leading: Container(
          width: 46,
          height: 46,
          decoration: BoxDecoration(
            color: AppColors.warning.withValues(alpha: 0.14),
            shape: BoxShape.circle,
          ),
          child: const Icon(
            Icons.payments_outlined,
            color: AppColors.warning,
          ),
        ),
        trailing: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              formatCurrency(c.remainingAmount),
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.w800,
                    color: Theme.of(context).colorScheme.error,
                  ),
            ),
            Text(
              'real_estate_remaining'.tr(),
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ],
        ),
        onTap: () =>
            Get.toNamed(AppRoutes.realEstateContractDetailPath(c.syncId)),
      ),
    );
  }
}
