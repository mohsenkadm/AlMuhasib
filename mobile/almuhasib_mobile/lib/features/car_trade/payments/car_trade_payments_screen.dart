import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/car_trade_payments_controller.dart';
import '../models/car_trade_models.dart';

class CarTradePaymentsScreen extends GetView<CarTradePaymentsController> {
  const CarTradePaymentsScreen({super.key});

  @override
  final String? tag = 'car_trade_payments';

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => AppListPage<CarTradeTransactionListItem>(
        title: 'car_trade_payments_title'.tr(),
        isLoading: controller.isLoading,
        error: controller.error,
        items: controller.unpaid,
        onRefresh: controller.load,
        onRetry: controller.load,
        emptyMessage: 'car_trade_no_unpaid'.tr(),
        emptyIcon: Icons.payments_outlined,
        itemBuilder: (context, t, index) => AppEntityCard(
          title: t.transactionNumber,
          subtitle: t.buyerName,
          trailing: Text(
            formatCurrency(t.remainingAmount),
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w800,
                  color: Theme.of(context).colorScheme.error,
                ),
          ),
          onTap: () =>
              Get.toNamed(AppRoutes.carTradeTransactionDetailPath(t.syncId)),
        ).fadeSlideIn(delayMs: index * 40),
      ),
    );
  }
}
