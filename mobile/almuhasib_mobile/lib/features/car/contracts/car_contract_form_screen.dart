import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/car_contract_form_controller.dart';

class CarContractFormScreen extends GetView<CarContractFormController> {
  const CarContractFormScreen({super.key});

  @override
  final String? tag = 'car_contract_form';

  @override
  Widget build(BuildContext context) {
    return AppFormPage(
      title: 'car_new_contract'.tr(),
      formKey: controller.formKey,
      saveLabel: 'save'.tr(),
      onSave: controller.save,
      isSaving: controller.saving,
      sections: [
        AppFormSection(
          title: 'car_contract_details'.tr(),
          children: [
            AppTextField(
              controller: controller.contractNumber,
              label: 'car_contract_number'.tr(),
              prefixIcon: Icons.tag_rounded,
              validator: (v) =>
                  v == null || v.isEmpty ? 'required'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.sellerName,
              label: 'car_seller'.tr(),
              prefixIcon: Icons.person_outline_rounded,
              validator: (v) =>
                  v == null || v.isEmpty ? 'required'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.buyerName,
              label: 'car_buyer'.tr(),
              prefixIcon: Icons.person_rounded,
              validator: (v) =>
                  v == null || v.isEmpty ? 'required'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.plateNumber,
              label: 'car_plate'.tr(),
              prefixIcon: Icons.confirmation_number_outlined,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.carType,
              label: 'car_type'.tr(),
              prefixIcon: Icons.directions_car_outlined,
            ),
          ],
        ),
        AppFormSection(
          title: 'car_payment_details'.tr(),
          children: [
            AppTextField(
              controller: controller.carPrice,
              label: 'car_price'.tr(),
              prefixIcon: Icons.payments_outlined,
              keyboardType: TextInputType.number,
              validator: (v) =>
                  v == null || v.isEmpty ? 'required'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.amountReceived,
              label: 'car_received'.tr(),
              prefixIcon: Icons.account_balance_wallet_outlined,
              keyboardType: TextInputType.number,
            ),
          ],
        ),
      ],
    );
  }
}
