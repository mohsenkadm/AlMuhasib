import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/theme/app_spacing.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/gold_create_sale_controller.dart';
import '../models/gold_shop_models.dart';

class GoldCreateSaleScreen extends GetView<GoldCreateSaleController> {
  const GoldCreateSaleScreen({super.key});

  @override
  final String? tag = 'gold_create_sale';

  @override
  Widget build(BuildContext context) {
    return Obx(() {
      if (controller.loading.value) {
        return Scaffold(
          appBar: AppBar(title: Text('gold_new_sale'.tr())),
          body: const Center(child: CircularProgressIndicator()),
        );
      }

      if (controller.error.value != null) {
        return Scaffold(
          appBar: AppBar(title: Text('gold_new_sale'.tr())),
          body: Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(controller.error.value.toString()),
                const SizedBox(height: 12),
                FilledButton(
                  onPressed: controller.loadBootstrap,
                  child: Text('retry'.tr()),
                ),
              ],
            ),
          ),
        );
      }

      return AppFormPage(
        title: 'gold_new_sale'.tr(),
        formKey: controller.formKey,
        saveLabel: 'save'.tr(),
        onSave: controller.save,
        isSaving: controller.saving,
        sections: [
          AppFormSection(
            title: 'gold_sale_header'.tr(),
            children: [
              Obx(
                () => DropdownButtonFormField<GoldWarehouseItem>(
                  value: controller.selectedWarehouse.value,
                  decoration: InputDecoration(
                    labelText: 'gold_warehouse'.tr(),
                    prefixIcon: const Icon(Icons.warehouse_outlined),
                  ),
                  items: controller.warehouses
                      .map(
                        (w) => DropdownMenuItem(
                          value: w,
                          child: Text(w.name),
                        ),
                      )
                      .toList(),
                  onChanged: controller.selectWarehouse,
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              TextField(
                controller: controller.customerSearch,
                decoration: InputDecoration(
                  labelText: 'gold_customer_search'.tr(),
                  prefixIcon: const Icon(Icons.search),
                ),
                onChanged: controller.filterCustomers,
              ),
              const SizedBox(height: AppSpacing.sm),
              Obx(
                () => DropdownButtonFormField<GoldCustomerListItem>(
                  value: controller.selectedCustomer.value,
                  decoration: InputDecoration(
                    labelText: 'gold_customer'.tr(),
                    prefixIcon: const Icon(Icons.person_outline),
                  ),
                  items: [
                    DropdownMenuItem<GoldCustomerListItem>(
                      value: null,
                      child: Text('—'),
                    ),
                    ...controller.filteredCustomers.map(
                      (c) => DropdownMenuItem(
                        value: c,
                        child: Text(
                          c.phone.isEmpty ? c.name : '${c.name} (${c.phone})',
                        ),
                      ),
                    ),
                  ],
                  onChanged: controller.selectCustomer,
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              Obx(
                () => DropdownButtonFormField<String>(
                  value: controller.paymentMethod.value,
                  decoration: InputDecoration(
                    labelText: 'gold_payment_method'.tr(),
                  ),
                  items: [
                    DropdownMenuItem(
                      value: 'Cash',
                      child: Text('gold_payment_cash'.tr()),
                    ),
                    DropdownMenuItem(
                      value: 'Credit',
                      child: Text('gold_payment_credit'.tr()),
                    ),
                  ],
                  onChanged: (v) {
                    if (v != null) controller.paymentMethod.value = v;
                  },
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              Row(
                children: [
                  Expanded(
                    child: Obx(
                      () => DropdownButtonFormField<String>(
                        value: controller.pricingCurrency.value,
                        decoration: InputDecoration(
                          labelText: 'gold_pricing_currency'.tr(),
                        ),
                        items: const [
                          DropdownMenuItem(value: 'USD', child: Text('USD')),
                          DropdownMenuItem(value: 'IQD', child: Text('IQD')),
                        ],
                        onChanged: (v) {
                          if (v != null) {
                            controller.pricingCurrency.value = v;
                          }
                        },
                      ),
                    ),
                  ),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: Obx(
                      () => DropdownButtonFormField<String>(
                        value: controller.paymentCurrency.value,
                        decoration: InputDecoration(
                          labelText: 'gold_payment_currency'.tr(),
                        ),
                        items: const [
                          DropdownMenuItem(value: 'IQD', child: Text('IQD')),
                          DropdownMenuItem(value: 'USD', child: Text('USD')),
                        ],
                        onChanged: (v) {
                          if (v != null) {
                            controller.paymentCurrency.value = v;
                          }
                        },
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppSpacing.md),
              TextField(
                controller: controller.fxRate,
                keyboardType: TextInputType.number,
                decoration: InputDecoration(
                  labelText: 'gold_fx_rate'.tr(),
                  prefixIcon: const Icon(Icons.currency_exchange),
                ),
              ),
            ],
          ),
          AppFormSection(
            title: 'gold_invoice_lines'.tr(),
            children: [
              Obx(
                () => Column(
                  children: [
                    for (var i = 0; i < controller.lines.length; i++)
                      _LineEditor(index: i, controller: controller),
                    Align(
                      alignment: AlignmentDirectional.centerStart,
                      child: TextButton.icon(
                        onPressed: controller.addLine,
                        icon: const Icon(Icons.add),
                        label: Text('gold_add_line'.tr()),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          AppFormSection(
            title: 'gold_totals'.tr(),
            children: [
              Obx(
                () => Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _TotalRow(
                      label: 'gold_gold_value'.tr(),
                      value: formatCurrency(controller.totalsGold),
                    ),
                    _TotalRow(
                      label: 'gold_making_charge'.tr(),
                      value: formatCurrency(controller.totalsMaking),
                    ),
                    TextField(
                      controller: controller.discountAmount,
                      keyboardType: TextInputType.number,
                      decoration: InputDecoration(
                        labelText: 'gold_discount'.tr(),
                      ),
                      onChanged: (_) => controller.lines.refresh(),
                    ),
                    const SizedBox(height: AppSpacing.sm),
                    _TotalRow(
                      label: 'gold_total'.tr(),
                      value: formatCurrency(controller.grandTotal),
                      emphasize: true,
                    ),
                    const SizedBox(height: AppSpacing.md),
                    TextField(
                      controller: controller.paidAmount,
                      keyboardType: TextInputType.number,
                      decoration: InputDecoration(
                        labelText: 'gold_paid'.tr(),
                      ),
                    ),
                    const SizedBox(height: AppSpacing.md),
                    TextField(
                      controller: controller.notes,
                      maxLines: 2,
                      decoration: InputDecoration(
                        labelText: 'notes'.tr(),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ],
      );
    });
  }
}

class _LineEditor extends StatelessWidget {
  const _LineEditor({required this.index, required this.controller});

  final int index;
  final GoldCreateSaleController controller;

  @override
  Widget build(BuildContext context) {
    final line = controller.lines[index];
    return Padding(
      padding: const EdgeInsets.only(bottom: AppSpacing.md),
      child: DecoratedBox(
        decoration: BoxDecoration(
          border: Border.all(color: Theme.of(context).dividerColor),
          borderRadius: BorderRadius.circular(12),
        ),
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Column(
            children: [
              Row(
                children: [
                  Expanded(
                    child: DropdownButtonFormField<int>(
                      value: line.karatValue,
                      decoration: InputDecoration(
                        labelText: 'gold_karat'.tr(),
                      ),
                      items: GoldCreateSaleController.karatOptions
                          .map(
                            (k) => DropdownMenuItem(
                              value: k,
                              child: Text('$k'),
                            ),
                          )
                          .toList(),
                      onChanged: (v) {
                        if (v != null) controller.updateLineKarat(index, v);
                      },
                    ),
                  ),
                  IconButton(
                    onPressed: () => controller.removeLine(index),
                    icon: const Icon(Icons.delete_outline),
                  ),
                ],
              ),
              const SizedBox(height: AppSpacing.sm),
              Row(
                children: [
                  Expanded(
                    child: TextFormField(
                      initialValue: line.weightGrams == 0
                          ? ''
                          : line.weightGrams.toString(),
                      keyboardType: TextInputType.number,
                      decoration: InputDecoration(
                        labelText: 'gold_weight'.tr(),
                      ),
                      validator: (v) {
                        final n = double.tryParse(v ?? '');
                        if (n == null || n <= 0) return 'required'.tr();
                        return null;
                      },
                      onChanged: (v) => controller.updateLineWeight(index, v),
                    ),
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  Expanded(
                    child: TextFormField(
                      initialValue: line.mithqalPrice == 0
                          ? ''
                          : line.mithqalPrice.toString(),
                      keyboardType: TextInputType.number,
                      decoration: InputDecoration(
                        labelText: 'gold_mithqal_price'.tr(),
                      ),
                      validator: (v) {
                        final n = double.tryParse(v ?? '');
                        if (n == null || n <= 0) return 'required'.tr();
                        return null;
                      },
                      onChanged: (v) => controller.updateLineMithqal(index, v),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppSpacing.sm),
              TextFormField(
                initialValue: line.makingCharge == 0
                    ? ''
                    : line.makingCharge.toString(),
                keyboardType: TextInputType.number,
                decoration: InputDecoration(
                  labelText: 'gold_making_charge'.tr(),
                ),
                onChanged: (v) => controller.updateLineMaking(index, v),
              ),
              const SizedBox(height: AppSpacing.sm),
              TextFormField(
                initialValue: line.description,
                decoration: InputDecoration(
                  labelText: 'gold_line_description'.tr(),
                ),
                onChanged: (v) => controller.updateLineDescription(index, v),
              ),
              Align(
                alignment: AlignmentDirectional.centerEnd,
                child: Text(
                  formatCurrency(line.lineTotal),
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _TotalRow extends StatelessWidget {
  const _TotalRow({
    required this.label,
    required this.value,
    this.emphasize = false,
  });

  final String label;
  final String value;
  final bool emphasize;

  @override
  Widget build(BuildContext context) {
    final style = emphasize
        ? Theme.of(context).textTheme.titleMedium?.copyWith(
              fontWeight: FontWeight.w800,
            )
        : Theme.of(context).textTheme.bodyMedium;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: style),
          Text(value, style: style),
        ],
      ),
    );
  }
}
