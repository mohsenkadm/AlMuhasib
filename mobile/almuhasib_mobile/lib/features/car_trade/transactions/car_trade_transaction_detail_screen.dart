import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../controllers/car_trade_transaction_detail_controller.dart';

class CarTradeTransactionDetailScreen
    extends GetView<CarTradeTransactionDetailController> {
  const CarTradeTransactionDetailScreen({super.key, required this.syncId});

  @override
  final String? tag = 'car_trade_transaction_detail';

  final String syncId;

  Future<void> _pay(BuildContext context) async {
    final amountCtrl = TextEditingController();
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('car_trade_record_payment'.tr()),
        content: TextField(
          controller: amountCtrl,
          keyboardType: TextInputType.number,
          decoration: InputDecoration(labelText: 'amount'.tr()),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: Text('cancel'.tr()),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: Text('save'.tr()),
          ),
        ],
      ),
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
    return Scaffold(
      appBar: AppBar(title: Text('car_trade_transaction_detail'.tr())),
      floatingActionButton: Obx(() {
        if (controller.transaction.value == null) {
          return const SizedBox.shrink();
        }
        return FloatingActionButton.extended(
          onPressed: () => _pay(context),
          icon: const Icon(Icons.payments),
          label: Text('car_trade_record_payment'.tr()),
        );
      }),
      body: Obx(() {
        if (controller.isLoading.value) {
          return const Center(child: CircularProgressIndicator());
        }
        if (controller.error.value != null) {
          return ErrorStateWidget(
            message: 'error_load'.tr(),
            onRetry: controller.load,
          );
        }
        final transaction = controller.transaction.value;
        if (transaction == null) return const SizedBox.shrink();
        return ListView(
          padding: const EdgeInsets.all(20),
          children: [
            GradientCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    transaction.transactionNumber,
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 8),
                  Text('${transaction.tradeType} • ${transaction.carName}'),
                  Text(transaction.plateNumber),
                  const Divider(height: 24),
                  _row('car_trade_buyer'.tr(), transaction.buyerName),
                  _row('car_trade_seller'.tr(), transaction.sellerName),
                  _row(
                    'car_trade_total_amount'.tr(),
                    formatCurrency(transaction.totalAmount),
                  ),
                  _row(
                    'car_trade_amount_paid'.tr(),
                    formatCurrency(transaction.amountPaid),
                  ),
                  _row(
                    'car_trade_remaining'.tr(),
                    formatCurrency(transaction.remainingAmount),
                  ),
                ],
              ),
            ),
          ],
        );
      }),
    );
  }

  Widget _row(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label),
          Flexible(child: Text(value, textAlign: TextAlign.end)),
        ],
      ),
    );
  }
}
