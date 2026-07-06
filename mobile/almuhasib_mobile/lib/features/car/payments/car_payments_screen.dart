import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/car_payments_controller.dart';
import '../models/car_models.dart';

class CarPaymentsScreen extends GetView<CarPaymentsController> {
  const CarPaymentsScreen({super.key}) : super(tag: 'car_payments');

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => AppListPage<CarContractListItem>(
        title: 'car_payments_title'.tr(),
        isLoading: controller.isLoading,
        error: controller.error,
        items: controller.unpaid,
        onRefresh: controller.load,
        onRetry: controller.load,
        emptyMessage: 'car_no_unpaid'.tr(),
        emptyIcon: Icons.payments_outlined,
        itemBuilder: (context, c, index) => AppEntityCard(
          title: c.contractNumber,
          subtitle: c.buyerName,
          trailing: Text(
            formatCurrency(c.remainingAmount),
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w800,
                  color: Theme.of(context).colorScheme.error,
                ),
          ),
          onTap: () => Get.toNamed(AppRoutes.carContractDetailPath(c.syncId)),
        ).fadeSlideIn(delayMs: index * 40),
      ),
    );
  }
}
