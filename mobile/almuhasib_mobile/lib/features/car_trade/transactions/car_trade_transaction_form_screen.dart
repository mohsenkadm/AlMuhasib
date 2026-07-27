import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/car_trade_transaction_form_controller.dart';

class CarTradeTransactionFormScreen
    extends GetView<CarTradeTransactionFormController> {
  const CarTradeTransactionFormScreen({super.key});

  @override
  final String? tag = 'car_trade_transaction_form';

  @override
  Widget build(BuildContext context) {
    return AppFormPage(
      title: 'car_trade_new_transaction'.tr(),
      formKey: controller.formKey,
      saveLabel: 'save'.tr(),
      onSave: controller.save,
      isSaving: controller.saving,
      sections: [
        AppFormSection(
          title: 'car_trade_transaction_details'.tr(),
          children: [
            Obx(
              () => SegmentedButton<String>(
                segments: [
                  ButtonSegment(
                    value: 'Buy',
                    label: Text('car_trade_type_buy'.tr()),
                    icon: const Icon(Icons.shopping_cart_outlined),
                  ),
                  ButtonSegment(
                    value: 'Sell',
                    label: Text('car_trade_type_sell'.tr()),
                    icon: const Icon(Icons.sell_outlined),
                  ),
                ],
                selected: {controller.tradeType.value},
                onSelectionChanged: (s) => controller.setTradeType(s.first),
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.transactionNumber,
              label: 'car_trade_transaction_number'.tr(),
              prefixIcon: Icons.tag_rounded,
              validator: (v) =>
                  v == null || v.isEmpty ? 'required'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.sellerName,
              label: 'car_trade_seller'.tr(),
              prefixIcon: Icons.person_outline_rounded,
              validator: (v) =>
                  v == null || v.isEmpty ? 'required'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.buyerName,
              label: 'car_trade_buyer'.tr(),
              prefixIcon: Icons.person_rounded,
              validator: (v) =>
                  v == null || v.isEmpty ? 'required'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.carName,
              label: 'car_trade_car_name'.tr(),
              prefixIcon: Icons.directions_car_outlined,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.plateNumber,
              label: 'car_trade_plate'.tr(),
              prefixIcon: Icons.confirmation_number_outlined,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.carType,
              label: 'car_trade_car_type'.tr(),
              prefixIcon: Icons.category_outlined,
            ),
          ],
        ),
        AppFormSection(
          title: 'car_trade_payment_details'.tr(),
          children: [
            AppTextField(
              controller: controller.totalAmount,
              label: 'car_trade_total_amount'.tr(),
              prefixIcon: Icons.payments_outlined,
              keyboardType: TextInputType.number,
              validator: (v) =>
                  v == null || v.isEmpty ? 'required'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.amountPaid,
              label: 'car_trade_amount_paid'.tr(),
              prefixIcon: Icons.account_balance_wallet_outlined,
              keyboardType: TextInputType.number,
            ),
          ],
        ),
      ],
    );
  }
}
