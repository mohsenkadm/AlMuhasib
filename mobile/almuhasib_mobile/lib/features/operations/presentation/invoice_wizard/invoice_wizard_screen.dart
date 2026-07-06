import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../../core/getx/app_services.dart';
import '../../../../shared/utils/formatters.dart';
import '../../../../shared/widgets/form_section_card.dart';
import '../../../../shared/widgets/sticky_summary_bar.dart';
import '../../controllers/invoice_wizard_controller.dart';

class InvoiceWizardScreen extends GetView<InvoiceWizardController> {
  const InvoiceWizardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => Scaffold(
        appBar: AppBar(
          title: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('new_invoice'.tr()),
              Text(
                'wizard_step'.tr(args: ['${controller.step.value + 1}', '5']),
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ),
        ),
        body: Column(
          children: [
            LinearProgressIndicator(value: (controller.step.value + 1) / 5),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  switch (controller.step.value) {
                    0 => _TypeStep(controller: controller),
                    1 => _PartyStep(controller: controller),
                    2 => _ItemsStep(controller: controller),
                    3 => _PaymentStep(controller: controller),
                    _ => _ReviewStep(controller: controller),
                  },
                ],
              ),
            ),
            StickySummaryBar(
              label: 'net_amount'.tr(),
              amount: formatCurrency(controller.net),
            ),
            Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  if (controller.step.value > 0)
                    Expanded(
                      child: OutlinedButton(
                        onPressed: controller.back,
                        child: Text('back'.tr()),
                      ),
                    ),
                  if (controller.step.value > 0) const SizedBox(width: 12),
                  Expanded(
                    child: FilledButton(
                      onPressed:
                          controller.saving.value ? null : controller.next,
                      child: controller.saving.value
                          ? const SizedBox(
                              height: 20,
                              width: 20,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : Text(
                              controller.step.value == 4
                                  ? 'save'.tr()
                                  : 'next'.tr(),
                            ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
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
            (0, 'purchase'.tr()),
            (1, 'sale'.tr()),
            (2, 'installment'.tr()),
            (3, 'purchase_return'.tr()),
          ].map(
            (entry) => RadioListTile<int>(
              value: entry.$1,
              groupValue: controller.invoiceType.value,
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
            );
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
