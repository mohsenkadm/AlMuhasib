import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/car_contract_detail_controller.dart';

class CarContractDetailScreen extends GetView<CarContractDetailController> {
  const CarContractDetailScreen({super.key, required this.syncId});

  @override
  final String? tag = 'car_contract_detail';

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
                'car_record_payment'.tr(),
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
    await AppServices.car.recordPayment(
      contractSyncId: syncId,
      amount: amount,
      paymentDate: DateTime.now(),
    );
    controller.load();
  }

  @override
  Widget build(BuildContext context) {
    return Obx(() {
      if (controller.isLoading.value && controller.contract.value == null) {
        return const Scaffold(
          body: Center(child: CircularProgressIndicator()),
        );
      }
      if (controller.error.value != null && controller.contract.value == null) {
        return Scaffold(
          appBar: AppBar(title: Text('car_contract_detail'.tr())),
          body: ErrorStateWidget(
            message: AppExceptionHandler.messageFor(controller.error.value),
            onRetry: controller.load,
          ),
        );
      }

      final contract = controller.contract.value;
      if (contract == null) {
        return Scaffold(
          appBar: AppBar(title: Text('car_contract_detail'.tr())),
          body: EmptyStateWidget(message: 'no_data'.tr()),
        );
      }

      return AppDetailPage(
        title: 'car_contract_detail'.tr(),
        subtitle: contract.contractNumber,
        onRefresh: controller.load,
        floatingActionButton: FloatingActionButton.extended(
          onPressed: () => _pay(context),
          icon: const Icon(Icons.payments_rounded),
          label: Text('car_record_payment'.tr()),
        ),
        header: Column(
          children: [
            AppBalanceHeroCard(
              title: 'car_price'.tr(),
              value: formatCurrency(contract.carPrice),
              subtitle: 'car_remaining'.tr(),
              trendLabel: formatCurrency(contract.remainingAmount),
              trendPositive: contract.remainingAmount <= 0,
            ),
            const SizedBox(height: 12),
            IntrinsicHeight(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Expanded(
                    child: KpiCard(
                      title: 'car_received'.tr(),
                      value: formatCurrency(contract.amountReceived),
                      icon: Icons.payments_rounded,
                      color: AppColors.success,
                      compact: true,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: KpiCard(
                      title: 'car_contract_number'.tr(),
                      value: formatDate(contract.contractDate),
                      icon: Icons.event_outlined,
                      color: AppColors.primary,
                      compact: true,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
        sections: [
          AppDetailSection(
            title: 'car_contract_details'.tr(),
            children: [
              AppDetailRow(
                label: 'car_plate'.tr(),
                value: contract.plateNumber,
              ),
              AppDetailRow(label: 'car_type'.tr(), value: contract.carType),
              if (contract.carModel.isNotEmpty)
                AppDetailRow(label: 'car_model'.tr(), value: contract.carModel),
              if (contract.carColor.isNotEmpty)
                AppDetailRow(label: 'car_color'.tr(), value: contract.carColor),
              if (contract.chassisNumber.isNotEmpty)
                AppDetailRow(
                  label: 'car_chassis'.tr(),
                  value: contract.chassisNumber,
                ),
              AppDetailRow(label: 'car_status'.tr(), value: contract.status),
            ],
          ),
          AppDetailSection(
            title: 'car_parties'.tr(),
            children: [
              AppDetailRow(label: 'car_seller'.tr(), value: contract.sellerName),
              if (contract.sellerPhone.isNotEmpty)
                AppDetailRow(
                  label: 'car_seller_phone'.tr(),
                  value: contract.sellerPhone,
                ),
              AppDetailRow(label: 'car_buyer'.tr(), value: contract.buyerName),
              if (contract.buyerPhone.isNotEmpty)
                AppDetailRow(
                  label: 'car_buyer_phone'.tr(),
                  value: contract.buyerPhone,
                ),
            ],
          ),
          if (contract.payments.isNotEmpty)
            AppDetailSection(
              title: 'car_payment_history'.tr(),
              children: [
                for (final payment in contract.payments)
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
                    ),
                  ),
              ],
            ),
          if (contract.notes.isNotEmpty)
            AppDetailSection(
              title: 'notes'.tr(),
              children: [Text(contract.notes)],
            ),
        ],
      );
    });
  }
}
