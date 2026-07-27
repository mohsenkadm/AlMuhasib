import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../../core/getx/app_services.dart';
import '../../../../shared/utils/formatters.dart';
import '../../../../shared/widgets/app_animations.dart';
import '../../../../shared/widgets/form_section_card.dart';
import '../../../../shared/widgets/sticky_summary_bar.dart';
import '../../controllers/invoice_wizard_controller.dart';

class InvoiceWizardScreen extends GetView<InvoiceWizardController> {
  const InvoiceWizardScreen({super.key});

  static const _stepTitles = [
    'wizard_step_type',
    'wizard_step_party',
    'wizard_step_items',
    'wizard_step_payment',
    'wizard_step_review',
  ];

  @override
  Widget build(BuildContext context) {
    return Obx(() {
      final step = controller.step.value;
      return Scaffold(
        appBar: AppBar(
          title: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('new_invoice'.tr()),
              Text(
                '${'wizard_step'.tr(args: ['${step + 1}', '5'])} • ${_stepTitles[step].tr()}',
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ),
        ),
        body: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
              child: Row(
                children: List.generate(5, (i) {
                  final active = i <= step;
                  return Expanded(
                    child: AnimatedContainer(
                      duration: const Duration(milliseconds: 280),
                      margin: EdgeInsetsDirectional.only(end: i == 4 ? 0 : 6),
                      height: 6,
                      decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(99),
                        color: active
                            ? Theme.of(context).colorScheme.primary
                            : Theme.of(context)
                                .colorScheme
                                .outline
                                .withValues(alpha: 0.25),
                      ),
                    ),
                  );
                }),
              ),
            ),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  AnimatedSwitcher(
                    duration: const Duration(milliseconds: 280),
                    switchInCurve: Curves.easeOutCubic,
                    switchOutCurve: Curves.easeInCubic,
                    child: KeyedSubtree(
                      key: ValueKey(step),
                      child: switch (step) {
                        0 => _TypeStep(controller: controller),
                        1 => _PartyStep(controller: controller),
                        2 => _ItemsStep(controller: controller),
                        3 => _PaymentStep(controller: controller),
                        _ => _ReviewStep(controller: controller),
                      }.fadeSlideIn(slideY: 0.04),
                    ),
                  ),
                ],
              ),
            ),
            if (step >= 2)
              StickySummaryBar(
                label: 'net_amount'.tr(),
                amount: formatCurrency(controller.net),
              ),
            SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
                child: Row(
                  children: [
                    if (step > 0)
                      Expanded(
                        child: OutlinedButton(
                          onPressed: controller.back,
                          child: Text('back'.tr()),
                        ),
                      ),
                    if (step > 0) const SizedBox(width: 12),
                    Expanded(
                      flex: 2,
                      child: FilledButton(
                        onPressed:
                            controller.saving.value ? null : controller.next,
                        child: controller.saving.value
                            ? const SizedBox(
                                height: 20,
                                width: 20,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                ),
                              )
                            : Text(
                                step == 4 ? 'save'.tr() : 'next'.tr(),
                              ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      );
    });
  }
}

class _TypeStep extends StatelessWidget {
  const _TypeStep({required this.controller});

  final InvoiceWizardController controller;

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => FormSectionCard(
        title: 'invoice_type'.tr(),
        children: [
          ...[
            (0, 'purchase'.tr(), Icons.shopping_bag_outlined),
            (1, 'sale'.tr(), Icons.point_of_sale_outlined),
            (2, 'installment'.tr(), Icons.calendar_month_outlined),
            (3, 'purchase_return'.tr(), Icons.undo_rounded),
          ].map(
            (entry) => RadioListTile<int>(
              value: entry.$1,
              groupValue: controller.invoiceType.value,
              secondary: Icon(entry.$3),
              title: Text(entry.$2),
              onChanged: (value) {
                if (value != null) controller.setInvoiceType(value);
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _PartyStep extends StatelessWidget {
  const _PartyStep({required this.controller});

  final InvoiceWizardController controller;

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => Column(
        children: [
          if (controller.needsCustomer)
            FormSectionCard(
              title: 'customers'.tr(),
              children: [
                OutlinedButton.icon(
                  onPressed: () => controller.pickLookup(
                    title: 'select_customer'.tr(),
                    loader: (search) =>
                        AppServices.data.getCustomers(search: search),
                    onSelected: (customer) =>
                        controller.customer.value = customer,
                  ),
                  icon: const Icon(Icons.person),
                  label: Text(
                    controller.customer.value?.name ?? 'select_customer'.tr(),
                  ),
                ),
              ],
            ),
          if (controller.needsSupplier)
            FormSectionCard(
              title: 'suppliers'.tr(),
              children: [
                OutlinedButton.icon(
                  onPressed: () => controller.pickLookup(
                    title: 'select_supplier'.tr(),
                    loader: (search) =>
                        AppServices.data.getSuppliers(search: search),
                    onSelected: (supplier) =>
                        controller.supplier.value = supplier,
                  ),
                  icon: const Icon(Icons.local_shipping),
                  label: Text(
                    controller.supplier.value?.name ?? 'select_supplier'.tr(),
                  ),
                ),
              ],
            ),
          FormSectionCard(
            title: 'warehouses'.tr(),
            children: [
              OutlinedButton.icon(
                onPressed: () => controller.pickLookup(
                  title: 'select_warehouse'.tr(),
                  loader: (search) =>
                      AppServices.data.getWarehouses(search: search),
                  onSelected: (warehouse) =>
                      controller.warehouse.value = warehouse,
                ),
                icon: const Icon(Icons.warehouse),
                label: Text(
                  controller.warehouse.value?.name ?? 'select_warehouse'.tr(),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _ItemsStep extends StatelessWidget {
  const _ItemsStep({required this.controller});

  final InvoiceWizardController controller;

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => Column(
        children: [
          Align(
            alignment: AlignmentDirectional.centerEnd,
            child: FilledButton.icon(
              onPressed: controller.pickProduct,
              icon: const Icon(Icons.add),
              label: Text('add_product'.tr()),
            ),
          ),
          const SizedBox(height: 12),
          if (controller.items.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 32),
              child: Text(
                'add_line_item'.tr(),
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodyMedium,
              ),
            ),
          ...controller.items.asMap().entries.map((entry) {
            final item = entry.value;
            final index = entry.key;
            return Card(
              margin: const EdgeInsets.only(bottom: 8),
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            item.itemName,
                            style: Theme.of(context).textTheme.titleSmall,
                          ),
                        ),
                        IconButton(
                          icon: const Icon(Icons.delete_outline),
                          onPressed: () => controller.removeItemAt(index),
                        ),
                      ],
                    ),
                    Row(
                      children: [
                        Expanded(
                          child: TextFormField(
                            initialValue: '${item.quantity}',
                            decoration:
                                InputDecoration(labelText: 'quantity'.tr()),
                            keyboardType: TextInputType.number,
                            onChanged: (value) =>
                                controller.updateItemQuantity(index, value),
                          ),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: TextFormField(
                            initialValue: '${item.unitPrice}',
                            decoration:
                                InputDecoration(labelText: 'unit_price'.tr()),
                            keyboardType: TextInputType.number,
                            onChanged: (value) =>
                                controller.updateItemUnitPrice(index, value),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ).fadeSlideInList(index: index);
          }),
        ],
      ),
    );
  }
}

class _PaymentStep extends StatelessWidget {
  const _PaymentStep({required this.controller});

  final InvoiceWizardController controller;

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => Column(
        children: [
          FormSectionCard(
            title: 'payment_method'.tr(),
            children: [
              ...[
                (0, 'cash'.tr()),
                (1, 'credit'.tr()),
                (2, 'installment'.tr()),
              ].map(
                (entry) => RadioListTile<int>(
                  value: entry.$1,
                  groupValue: controller.paymentMethod.value,
                  title: Text(entry.$2),
                  onChanged: (value) {
                    if (value != null) controller.setPaymentMethod(value);
                  },
                ),
              ),
              if (controller.paymentMethod.value == 0)
                OutlinedButton.icon(
                  onPressed: () => controller.pickLookup(
                    title: 'select_cashbox'.tr(),
                    loader: (search) =>
                        AppServices.data.getCashBoxes(search: search),
                    onSelected: (cashBox) =>
                        controller.cashBox.value = cashBox,
                  ),
                  icon: const Icon(Icons.account_balance_wallet),
                  label: Text(
                    controller.cashBox.value?.name ?? 'select_cashbox'.tr(),
                  ),
                ),
              TextFormField(
                controller: controller.discountController,
                decoration: InputDecoration(labelText: 'discount'.tr()),
                keyboardType: TextInputType.number,
                onChanged: (_) => controller.refreshTotals(),
              ),
              if (controller.needsInstallmentPlan) ...[
                const SizedBox(height: 12),
                TextFormField(
                  initialValue: '${controller.installmentCount.value}',
                  decoration:
                      InputDecoration(labelText: 'installment_count'.tr()),
                  keyboardType: TextInputType.number,
                  onChanged: controller.setInstallmentCount,
                ),
              ],
            ],
          ),
        ],
      ),
    );
  }
}

class _ReviewStep extends StatelessWidget {
  const _ReviewStep({required this.controller});

  final InvoiceWizardController controller;

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => FormSectionCard(
        title: 'review'.tr(),
        children: [
          Text(
            '${'invoice_type'.tr()}: ${invoiceTypeLabel(controller.invoiceType.value)}',
          ),
          Text(
            '${'payment_method'.tr()}: ${paymentMethodLabel(controller.paymentMethod.value)}',
          ),
          if (controller.customer.value != null)
            Text('${'customers'.tr()}: ${controller.customer.value!.name}'),
          if (controller.supplier.value != null)
            Text('${'suppliers'.tr()}: ${controller.supplier.value!.name}'),
          if (controller.warehouse.value != null)
            Text('${'warehouses'.tr()}: ${controller.warehouse.value!.name}'),
          Text('${'items'.tr()}: ${controller.items.length}'),
          Text('${'net_amount'.tr()}: ${formatCurrency(controller.net)}'),
          TextFormField(
            controller: controller.notesController,
            decoration: InputDecoration(labelText: 'notes'.tr()),
            maxLines: 2,
          ),
        ],
      ),
    );
  }
}
