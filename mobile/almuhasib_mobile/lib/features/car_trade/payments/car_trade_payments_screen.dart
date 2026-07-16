import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/car_trade_payments_controller.dart';
import '../models/car_trade_models.dart';
import '../widgets/car_trade_labels.dart';

class CarTradePaymentsScreen extends GetView<CarTradePaymentsController> {
  const CarTradePaymentsScreen({super.key});

  @override
  final String? tag = 'car_trade_payments';

  @override
  Widget build(BuildContext context) {
    return AppListPage<CarTradeTransactionListItem>(
      title: 'car_trade_payments_title'.tr(),
      isLoading: controller.isLoading,
      error: controller.error,
      items: controller.unpaid,
      onRefresh: controller.load,
      onRetry: controller.load,
      emptyMessage: 'car_trade_no_unpaid'.tr(),
      emptyIcon: Icons.payments_outlined,
      itemBuilder: (context, t, index) {
        final buy = t.isBuy;
        return AppEntityCard(
          title: t.transactionNumber,
          subtitle:
              '${carTradeTypeLabel(t.tradeType)} • ${t.buyerName.isEmpty ? t.sellerName : t.buyerName}\n${t.carName.isEmpty ? t.plateNumber : t.carName}',
          leading: Container(
            width: 46,
            height: 46,
            decoration: BoxDecoration(
              color: AppColors.warning.withValues(alpha: 0.14),
              shape: BoxShape.circle,
            ),
            child: Icon(
              buy ? Icons.shopping_cart_outlined : Icons.sell_outlined,
              color: AppColors.warning,
            ),
          ),
          trailing: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                formatCurrency(t.remainingAmount),
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w800,
                      color: Theme.of(context).colorScheme.error,
                    ),
              ),
              Text(
                'car_trade_remaining'.tr(),
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ),
          onTap: () =>
              Get.toNamed(AppRoutes.carTradeTransactionDetailPath(t.syncId)),
        ).fadeSlideIn(delayMs: index * 40);
      },
    );
  }
}
