import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/real_estate_contract_form_controller.dart';

class RealEstateContractFormScreen
    extends GetView<RealEstateContractFormController> {
  const RealEstateContractFormScreen({super.key});

  @override
  final String? tag = 'real_estate_contract_form';

  @override
  Widget build(BuildContext context) {
    return AppFormPage(
      title: 'real_estate_new_contract'.tr(),
      formKey: controller.formKey,
      saveLabel: 'save'.tr(),
      onSave: controller.save,
      isSaving: controller.saving,
      sections: [
        AppFormSection(
          title: 'real_estate_contract_details'.tr(),
          children: [
            Obx(
              () => SegmentedButton<int>(
                segments: [
                  ButtonSegment(
                    value: 0,
                    label: Text('real_estate_type_sale'.tr()),
                    icon: const Icon(Icons.sell_outlined),
                  ),
                  ButtonSegment(
                    value: 1,
                    label: Text('real_estate_type_purchase'.tr()),
                    icon: const Icon(Icons.shopping_cart_outlined),
                  ),
                ],
                selected: {controller.contractType.value},
                onSelectionChanged: (s) => controller.setContractType(s.first),
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.contractNumber,
              label: 'real_estate_contract_number'.tr(),
              prefixIcon: Icons.tag_rounded,
              validator: (v) =>
                  v == null || v.isEmpty ? 'required'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            Obx(
              () => SegmentedButton<int>(
                segments: [
                  ButtonSegment(
                    value: 0,
                    label: Text('real_estate_property_house'.tr()),
                  ),
                  ButtonSegment(
                    value: 1,
                    label: Text('real_estate_property_land'.tr()),
                  ),
                  ButtonSegment(
                    value: 2,
                    label: Text('real_estate_property_other'.tr()),
                  ),
                ],
                selected: {controller.propertyType.value},
                onSelectionChanged: (s) => controller.setPropertyType(s.first),
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.propertyLocation,
              label: 'real_estate_property_location'.tr(),
              prefixIcon: Icons.place_outlined,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.propertyAddress,
              label: 'real_estate_property_address'.tr(),
              prefixIcon: Icons.home_work_outlined,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.propertyAreaSqm,
              label: 'real_estate_property_area'.tr(),
              prefixIcon: Icons.straighten_outlined,
              keyboardType: TextInputType.number,
            ),
          ],
        ),
        AppFormSection(
          title: 'real_estate_parties'.tr(),
          children: [
            AppTextField(
              controller: controller.sellerName,
              label: 'real_estate_seller'.tr(),
              prefixIcon: Icons.person_outline_rounded,
              validator: (v) =>
                  v == null || v.isEmpty ? 'required'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.buyerName,
              label: 'real_estate_buyer'.tr(),
              prefixIcon: Icons.person_rounded,
              validator: (v) =>
                  v == null || v.isEmpty ? 'required'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.witnessOneName,
              label: 'real_estate_witness_one'.tr(),
              prefixIcon: Icons.people_outline_rounded,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.witnessTwoName,
              label: 'real_estate_witness_two'.tr(),
              prefixIcon: Icons.people_outline_rounded,
            ),
          ],
        ),
        AppFormSection(
          title: 'real_estate_payment_details'.tr(),
          children: [
            AppTextField(
              controller: controller.totalPrice,
              label: 'real_estate_total_price'.tr(),
              prefixIcon: Icons.payments_outlined,
              keyboardType: TextInputType.number,
              validator: (v) =>
                  v == null || v.isEmpty ? 'required'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.downPayment,
              label: 'real_estate_down_payment'.tr(),
              prefixIcon: Icons.savings_outlined,
              keyboardType: TextInputType.number,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.amountPaid,
              label: 'real_estate_amount_paid'.tr(),
              prefixIcon: Icons.account_balance_wallet_outlined,
              keyboardType: TextInputType.number,
            ),
            const SizedBox(height: AppSpacing.md),
            Obx(
              () => SegmentedButton<int>(
                segments: [
                  ButtonSegment(
                    value: 0,
                    label: Text('real_estate_payment_cash'.tr()),
                  ),
                  ButtonSegment(
                    value: 1,
                    label: Text('real_estate_payment_credit'.tr()),
                  ),
                ],
                selected: {controller.paymentMode.value},
                onSelectionChanged: (s) => controller.setPaymentMode(s.first),
              ),
            ),
            Obx(() {
              if (controller.paymentMode.value != 1) {
                return const SizedBox.shrink();
              }
              return Padding(
                padding: const EdgeInsets.only(top: AppSpacing.md),
                child: SegmentedButton<int>(
                  segments: [
                    ButtonSegment(
                      value: 1,
                      label: Text('real_estate_debtor_buyer'.tr()),
                    ),
                    ButtonSegment(
                      value: 2,
                      label: Text('real_estate_debtor_seller'.tr()),
                    ),
                  ],
                  selected: {
                    controller.debtorParty.value == 0
                        ? 1
                        : controller.debtorParty.value,
                  },
                  onSelectionChanged: (s) =>
                      controller.setDebtorParty(s.first),
                ),
              );
            }),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.notes,
              label: 'notes'.tr(),
              prefixIcon: Icons.notes_outlined,
              maxLines: 3,
            ),
          ],
        ),
      ],
    );
  }
}
