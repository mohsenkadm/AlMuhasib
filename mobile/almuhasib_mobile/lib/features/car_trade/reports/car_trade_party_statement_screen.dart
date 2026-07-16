import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/car_trade_party_statement_controller.dart';
import '../models/car_trade_models.dart';
import '../widgets/car_trade_labels.dart';

class CarTradePartyStatementScreen
    extends GetView<CarTradePartyStatementController> {
  const CarTradePartyStatementScreen({super.key});

  @override
  final String? tag = 'car_trade_party_statement';

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: 'car_trade_party_statement_title'.tr(),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 12, 20, 8),
            child: Column(
              children: [
                AppTextField(
                  controller: controller.partyNameController,
                  label: 'car_trade_party_name'.tr(),
                  prefixIcon: Icons.person_outline_rounded,
                ),
                const SizedBox(height: 10),
                AppTextField(
                  controller: controller.partyPhoneController,
                  label: 'car_trade_party_phone'.tr(),
                  keyboardType: TextInputType.phone,
                  prefixIcon: Icons.phone_outlined,
                ),
                const SizedBox(height: 10),
                Row(
                  children: [
                    Expanded(
                      child: Obx(
                        () => OutlinedButton.icon(
                          onPressed: controller.pickFromDate,
                          icon: const Icon(Icons.calendar_today_outlined, size: 16),
                          label: Text(
                            controller.from.value == null
                                ? 'from_date'.tr()
                                : formatDate(controller.from.value!),
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Obx(
                        () => OutlinedButton.icon(
                          onPressed: controller.pickToDate,
                          icon: const Icon(Icons.event_outlined, size: 16),
                          label: Text(
                            controller.to.value == null
                                ? 'to_date'.tr()
                                : formatDate(controller.to.value!),
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                SizedBox(
                  width: double.infinity,
                  child: FilledButton.icon(
                    onPressed: controller.load,
                    icon: const Icon(Icons.search_rounded),
                    label: Text('search'.tr()),
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: Obx(() {
              if (controller.error.value == 'car_trade_party_name_required') {
                return Center(
                  child: Text('car_trade_party_name_required'.tr()),
                );
              }

              return AppAsyncBody<CarTradePartyStatementDto>(
                isLoading: controller.isLoading.value,
                error: controller.error.value == 'car_trade_party_name_required'
                    ? null
                    : controller.error.value,
                data: controller.statement.value,
                onRetry: controller.load,
                showEmptyWhenNull: true,
                emptyMessage: 'no_data'.tr(),
                builder: (context, statement) {
                  return ListView(
                    padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
                    children: [
                      AppBalanceHeroCard(
                        title: statement.partyName,
                        value: formatCurrency(statement.balance),
                        subtitle: 'car_trade_balance'.tr(),
                        trendLabel: statement.partyPhone.isEmpty
                            ? null
                            : statement.partyPhone,
                      ),
                      const SizedBox(height: 14),
                      AppKpiGrid(
                        childAspectRatio: 1.45,
                        items: [
                          AppKpiItem(
                            title: 'car_trade_total_debit'.tr(),
                            value: formatCurrency(statement.totalDebit),
                            icon: Icons.arrow_upward_rounded,
                            color: AppColors.error,
                            compact: true,
                          ),
                          AppKpiItem(
                            title: 'car_trade_total_credit'.tr(),
                            value: formatCurrency(statement.totalCredit),
                            icon: Icons.arrow_downward_rounded,
                            color: AppColors.success,
                            compact: true,
                          ),
                        ],
                      ),
                      const SizedBox(height: 16),
                      Text(
                        'car_trade_transactions_title'.tr(),
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(
                              fontWeight: FontWeight.w800,
                            ),
                      ),
                      const SizedBox(height: 10),
                      if (statement.rows.isEmpty)
                        Padding(
                          padding: const EdgeInsets.symmetric(vertical: 32),
                          child: Center(child: Text('no_data'.tr())),
                        )
                      else
                        ...statement.rows.map(
                          (row) => Padding(
                            padding: const EdgeInsets.only(bottom: 10),
                            child: AppEntityCard(
                              title: row.transactionNumber,
                              subtitle:
                                  '${formatDate(row.transactionDate)} • ${carTradeTypeLabel(row.tradeType)}\n${row.carName} — ${row.partyRole}',
                              leading: Container(
                                width: 46,
                                height: 46,
                                decoration: BoxDecoration(
                                  color: AppColors.primary.withValues(alpha: 0.12),
                                  shape: BoxShape.circle,
                                ),
                                child: const Icon(
                                  Icons.directions_car_outlined,
                                  color: AppColors.primary,
                                ),
                              ),
                              trailing: Text(
                                formatCurrency(row.remainingAmount),
                                style: const TextStyle(fontWeight: FontWeight.w800),
                              ),
                            ),
                          ),
                        ),
                    ],
                  );
                },
              );
            }),
          ),
        ],
      ),
    );
  }
}
