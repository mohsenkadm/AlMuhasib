import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/car_trade_party_statement_controller.dart';
import '../models/car_trade_models.dart';

class CarTradePartyStatementScreen extends GetView<CarTradePartyStatementController> {
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
                TextField(
                  controller: controller.partyNameController,
                  decoration: InputDecoration(
                    labelText: 'car_trade_party_name'.tr(),
                    border: const OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 8),
                TextField(
                  controller: controller.partyPhoneController,
                  decoration: InputDecoration(
                    labelText: 'car_trade_party_phone'.tr(),
                    border: const OutlineInputBorder(),
                  ),
                  keyboardType: TextInputType.phone,
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Expanded(
                      child: Obx(
                        () => OutlinedButton(
                          onPressed: controller.pickFromDate,
                          child: Text(
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
                        () => OutlinedButton(
                          onPressed: controller.pickToDate,
                          child: Text(
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
                  child: FilledButton(
                    onPressed: controller.load,
                    child: Text('search'.tr()),
                  ),
                ),
              ],
            ),
          ),
          Obx(() {
            if (controller.isLoading.value) {
              return const LinearProgressIndicator(minHeight: 3);
            }
            final data = controller.statement.value;
            if (data != null) {
              return Padding(
                padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 8),
                child: Row(
                  children: [
                    Expanded(
                      child: AppEntityCard(
                        title: 'car_trade_total_debit'.tr(),
                        trailing: Text(formatCurrency(data.totalDebit)),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: AppEntityCard(
                        title: 'car_trade_balance'.tr(),
                        trailing: Text(formatCurrency(data.balance)),
                      ),
                    ),
                  ],
                ),
              );
            }
            return const SizedBox.shrink();
          }),
          Expanded(
            child: Obx(() {
              if (controller.error.value == 'car_trade_party_name_required') {
                return Center(child: Text('car_trade_party_name_required'.tr()));
              }
              return AppAsyncBody<CarTradePartyStatementDto>(
                isLoading: controller.isLoading.value,
                error: controller.error.value == 'car_trade_party_name_required'
                    ? null
                    : controller.error.value,
                data: controller.statement.value,
                onRetry: controller.load,
                showEmptyWhenNull: true,
                builder: (context, data) {
                  if (data.rows.isEmpty) {
                    return Center(child: Text('no_data'.tr()));
                  }
                  return ListView.builder(
                    padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
                    itemCount: data.rows.length,
                    itemBuilder: (context, i) {
                      final row = data.rows[i];
                      return AppEntityCard(
                        title: row.transactionNumber,
                        subtitle:
                            '${formatDate(row.transactionDate)} • ${row.tradeType}',
                        trailing: Text(formatCurrency(row.remainingAmount)),
                        child: Padding(
                          padding: const EdgeInsets.only(top: 8),
                          child: Text(
                            '${row.carName} — ${row.partyRole}',
                            style: Theme.of(context).textTheme.bodySmall,
                          ),
                        ),
                      );
                    },
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
