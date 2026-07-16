import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/car_trade_transaction_detail_controller.dart';
import '../widgets/car_trade_labels.dart';

class CarTradeTransactionDetailScreen
    extends GetView<CarTradeTransactionDetailController> {
  const CarTradeTransactionDetailScreen({super.key, required this.syncId});

  @override
  final String? tag = 'car_trade_transaction_detail';

  final String syncId;

  Future<void> _pay(BuildContext context) async {
    final amountCtrl = TextEditingController();
    final ok = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) {
        return Padding(
          padding: EdgeInsets.fromLTRB(
            20,
            20,
            20,
            MediaQuery.viewInsetsOf(ctx).bottom + 20,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'car_trade_record_payment'.tr(),
                style: Theme.of(ctx).textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
              ),
              const SizedBox(height: 16),
              TextField(
                controller: amountCtrl,
                autofocus: true,
                keyboardType:
                    const TextInputType.numberWithOptions(decimal: true),
                decoration: InputDecoration(
                  labelText: 'amount'.tr(),
                  border: const OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 16),
              FilledButton(
                onPressed: () => Navigator.pop(ctx, true),
                child: Text('save'.tr()),
              ),
              TextButton(
                onPressed: () => Navigator.pop(ctx, false),
                child: Text('cancel'.tr()),
              ),
            ],
          ),
        );
      },
    );
    if (ok != true) {
      amountCtrl.dispose();
      return;
    }
    final amount = double.tryParse(amountCtrl.text) ?? 0;
    amountCtrl.dispose();
    if (amount <= 0) return;
    await AppServices.carTrade.recordPayment(
      transactionSyncId: syncId,
      amount: amount,
      paymentDate: DateTime.now(),
    );
    controller.load();
  }

  @override
  Widget build(BuildContext context) {
    return Obx(() {
      if (controller.isLoading.value && controller.transaction.value == null) {
        return const Scaffold(
          body: Center(child: CircularProgressIndicator()),
        );
      }
      if (controller.error.value != null &&
          controller.transaction.value == null) {
        return Scaffold(
          appBar: AppBar(title: Text('car_trade_transaction_detail'.tr())),
          body: ErrorStateWidget(
            message: AppExceptionHandler.messageFor(controller.error.value),
            onRetry: controller.load,
          ),
        );
      }

      final transaction = controller.transaction.value;
      if (transaction == null) {
        return Scaffold(
          appBar: AppBar(title: Text('car_trade_transaction_detail'.tr())),
          body: EmptyStateWidget(message: 'no_data'.tr()),
        );
      }

      final buy = transaction.isBuy;

      return AppDetailPage(
        title: 'car_trade_transaction_detail'.tr(),
        subtitle: transaction.transactionNumber,
        onRefresh: controller.load,
        floatingActionButton: FloatingActionButton.extended(
          onPressed: () => _pay(context),
          icon: const Icon(Icons.payments_rounded),
          label: Text('car_trade_record_payment'.tr()),
        ),
        header: Column(
          children: [
            AppBalanceHeroCard(
              title: 'car_trade_total_amount'.tr(),
              value: formatCurrency(transaction.totalAmount),
              subtitle: 'car_trade_remaining'.tr(),
              trendLabel: formatCurrency(transaction.remainingAmount),
              trendPositive: transaction.remainingAmount <= 0,
            ).fadeSlideIn(),
            const SizedBox(height: 12),
            AppKpiGrid(
              childAspectRatio: 1.5,
              items: [
                AppKpiItem(
                  title: 'car_trade_amount_paid'.tr(),
                  value: formatCurrency(transaction.amountPaid),
                  icon: Icons.payments_rounded,
                  color: AppColors.success,
                  compact: true,
                ),
                AppKpiItem(
                  title: carTradeTypeLabel(transaction.tradeType),
                  value: formatDate(transaction.transactionDate),
                  icon: buy
                      ? Icons.shopping_cart_outlined
                      : Icons.sell_outlined,
                  color: buy ? AppColors.moduleOrange : AppColors.moduleCyan,
                  compact: true,
                ),
              ],
            ),
          ],
        ),
        sections: [
          AppDetailSection(
            title: 'car_trade_transaction_details'.tr(),
            children: [
              _DetailRow(
                label: 'car_trade_car_name'.tr(),
                value: transaction.carName,
              ),
              _DetailRow(
                label: 'car_trade_plate'.tr(),
                value: transaction.plateNumber,
              ),
              _DetailRow(
                label: 'car_trade_car_type'.tr(),
                value: transaction.carType,
              ),
              if (transaction.carColor.isNotEmpty)
                _DetailRow(
                  label: 'car_trade_car_color'.tr(),
                  value: transaction.carColor,
                ),
              if (transaction.chassisNumber.isNotEmpty)
                _DetailRow(
                  label: 'car_trade_chassis'.tr(),
                  value: transaction.chassisNumber,
                ),
                  _DetailRow(
                    label: 'car_trade_status'.tr(),
                    value: transaction.status,
                  ),
            ],
          ),
          AppDetailSection(
            title: 'car_trade_parties'.tr(),
            children: [
              _DetailRow(
                label: 'car_trade_seller'.tr(),
                value: transaction.sellerName,
              ),
              if (transaction.sellerPhone.isNotEmpty)
                _DetailRow(
                  label: 'car_trade_seller_phone'.tr(),
                  value: transaction.sellerPhone,
                ),
              _DetailRow(
                label: 'car_trade_buyer'.tr(),
                value: transaction.buyerName,
              ),
              if (transaction.buyerPhone.isNotEmpty)
                _DetailRow(
                  label: 'car_trade_buyer_phone'.tr(),
                  value: transaction.buyerPhone,
                ),
            ],
          ),
          if (transaction.payments.isNotEmpty)
            AppDetailSection(
              title: 'car_trade_payment_history'.tr(),
              children: [
                for (final payment in transaction.payments)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 10),
                    child: AppEntityCard(
                      title: formatCurrency(payment.amount),
                      subtitle: formatDate(payment.paymentDate),
                      leading: Container(
                        width: 42,
                        height: 42,
                        decoration: BoxDecoration(
                          color: AppColors.success.withValues(alpha: 0.14),
                          shape: BoxShape.circle,
                        ),
                        child: const Icon(
                          Icons.payments_rounded,
                          color: AppColors.success,
                          size: 20,
                        ),
                      ),
                      trailing: payment.notes.isEmpty
                          ? null
                          : Text(
                              payment.notes,
                              style: Theme.of(context).textTheme.bodySmall,
                            ),
                    ),
                  ),
              ],
            ),
          if (transaction.notes.isNotEmpty)
            AppDetailSection(
              title: 'notes'.tr(),
              children: [Text(transaction.notes)],
            ),
        ],
      );
    });
  }
}

class _DetailRow extends StatelessWidget {
  const _DetailRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    if (value.isEmpty) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Text(
              label,
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: Theme.of(context)
                        .colorScheme
                        .onSurface
                        .withValues(alpha: 0.6),
                  ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              textAlign: TextAlign.end,
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    fontWeight: FontWeight.w700,
                  ),
            ),
          ),
        ],
      ),
    );
  }
}
