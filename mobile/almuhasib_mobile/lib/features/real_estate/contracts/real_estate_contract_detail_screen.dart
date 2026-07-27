import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/real_estate_contract_detail_controller.dart';
import '../widgets/real_estate_labels.dart';

class RealEstateContractDetailScreen
    extends GetView<RealEstateContractDetailController> {
  const RealEstateContractDetailScreen({super.key, required this.syncId});

  @override
  final String? tag = 'real_estate_contract_detail';

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
                'real_estate_record_payment'.tr(),
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
    await AppServices.realEstate.recordPayment(
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
          appBar: AppBar(title: Text('real_estate_contract_detail'.tr())),
          body: ErrorStateWidget(
            message: AppExceptionHandler.messageFor(controller.error.value),
            onRetry: controller.load,
          ),
        );
      }

      final contract = controller.contract.value;
      if (contract == null) {
        return Scaffold(
          appBar: AppBar(title: Text('real_estate_contract_detail'.tr())),
          body: EmptyStateWidget(message: 'no_data'.tr()),
        );
      }

      return AppDetailPage(
        title: 'real_estate_contract_detail'.tr(),
        subtitle: contract.contractNumber,
        onRefresh: controller.load,
        floatingActionButton: FloatingActionButton.extended(
          onPressed: () => _pay(context),
          icon: const Icon(Icons.payments_rounded),
          label: Text('real_estate_record_payment'.tr()),
        ),
        header: Column(
          children: [
            AppBalanceHeroCard(
              title: 'real_estate_total_price'.tr(),
              value: formatCurrency(contract.totalPrice),
              subtitle: 'real_estate_remaining'.tr(),
              trendLabel: formatCurrency(contract.remainingAmount),
              trendPositive: contract.remainingAmount <= 0,
            ),
            const SizedBox(height: 12),
            AppKpiGrid(
              childAspectRatio: 1.55,
              items: [
                AppKpiItem(
                  title: 'real_estate_amount_paid'.tr(),
                  value: formatCurrency(contract.amountPaid),
                  icon: Icons.payments_rounded,
                  color: AppColors.success,
                  compact: true,
                ),
                AppKpiItem(
                  title: 'real_estate_contract_number'.tr(),
                  value: formatDate(contract.contractDate),
                  icon: Icons.event_outlined,
                  color: AppColors.primary,
                  compact: true,
                ),
              ],
            ),
          ],
        ),
        sections: [
          AppDetailSection(
            title: 'real_estate_contract_details'.tr(),
            children: [
              AppDetailRow(
                label: 'real_estate_contract_type'.tr(),
                value: realEstatePaymentStatusLabel(
                  contract.contractType.isEmpty
                      ? '${contract.contractTypeValue}'
                      : contract.contractType,
                ),
              ),
              AppDetailRow(
                label: 'real_estate_property_type'.tr(),
                value: realEstatePaymentStatusLabel(
                  contract.propertyType.isEmpty
                      ? '${contract.propertyTypeValue}'
                      : contract.propertyType,
                ),
              ),
              if (contract.propertyLocation.isNotEmpty)
                AppDetailRow(
                  label: 'real_estate_property_location'.tr(),
                  value: contract.propertyLocation,
                ),
              if (contract.propertyAddress.isNotEmpty)
                AppDetailRow(
                  label: 'real_estate_property_address'.tr(),
                  value: contract.propertyAddress,
                ),
              if (contract.propertyAreaSqm > 0)
                AppDetailRow(
                  label: 'real_estate_property_area'.tr(),
                  value: '${contract.propertyAreaSqm}',
                ),
              if (contract.propertyDescription.isNotEmpty)
                AppDetailRow(
                  label: 'real_estate_property_description'.tr(),
                  value: contract.propertyDescription,
                ),
              AppDetailRow(
                label: 'real_estate_status'.tr(),
                value: contract.status,
              ),
            ],
          ),
          AppDetailSection(
            title: 'real_estate_parties'.tr(),
            children: [
              AppDetailRow(
                label: 'real_estate_seller'.tr(),
                value: contract.sellerName,
              ),
              if (contract.sellerPhone.isNotEmpty)
                AppDetailRow(
                  label: 'real_estate_seller_phone'.tr(),
                  value: contract.sellerPhone,
                ),
              AppDetailRow(
                label: 'real_estate_buyer'.tr(),
                value: contract.buyerName,
              ),
              if (contract.buyerPhone.isNotEmpty)
                AppDetailRow(
                  label: 'real_estate_buyer_phone'.tr(),
                  value: contract.buyerPhone,
                ),
              if (contract.witnessOneName.isNotEmpty)
                AppDetailRow(
                  label: 'real_estate_witness_one'.tr(),
                  value: contract.witnessOneName,
                ),
              if (contract.witnessTwoName.isNotEmpty)
                AppDetailRow(
                  label: 'real_estate_witness_two'.tr(),
                  value: contract.witnessTwoName,
                ),
            ],
          ),
          AppDetailSection(
            title: 'real_estate_payment_details'.tr(),
            children: [
              if (contract.downPayment > 0)
                AppDetailRow(
                  label: 'real_estate_down_payment'.tr(),
                  value: formatCurrency(contract.downPayment),
                ),
              AppDetailRow(
                label: 'real_estate_payment_mode'.tr(),
                value: realEstatePaymentModeLabel(contract.paymentModeValue),
              ),
              if (contract.debtorPartyValue > 0)
                AppDetailRow(
                  label: 'real_estate_debtor_party'.tr(),
                  value: realEstateDebtorPartyLabel(contract.debtorPartyValue),
                ),
              if (contract.dueDate != null)
                AppDetailRow(
                  label: 'real_estate_due_date'.tr(),
                  value: formatDate(contract.dueDate!),
                ),
            ],
          ),
          if (contract.payments.isNotEmpty)
            AppDetailSection(
              title: 'real_estate_payment_history'.tr(),
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
          if (contract.clauses.isNotEmpty)
            AppDetailSection(
              title: 'real_estate_clauses'.tr(),
              children: [
                for (final clause in contract.clauses)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 10),
                    child: AppEntityCard(
                      title: clause.title.isEmpty
                          ? 'real_estate_clause'.tr()
                          : clause.title,
                      subtitle: clause.body,
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
