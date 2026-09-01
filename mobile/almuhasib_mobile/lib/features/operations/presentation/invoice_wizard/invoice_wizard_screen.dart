import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../../core/getx/app_services.dart';
import '../../../../shared/utils/formatters.dart';
import '../../../../shared/widgets/app_animations.dart';
import '../../../../shared/widgets/design_system/design_system.dart';
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
    final scheme = Theme.of(context).colorScheme;
    return Obx(() {
      final step = controller.step.value;
      final isLast = step == 4;
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
              padding: const EdgeInsets.fromLTRB(16, 10, 16, 0),
              child: Row(
                children: List.generate(5, (i) {
                  final active = i <= step;
                  final current = i == step;
                  return Expanded(
                    child: AnimatedContainer(
                      duration: const Duration(milliseconds: 320),
                      curve: Curves.easeOutCubic,
                      margin: EdgeInsetsDirectional.only(end: i == 4 ? 0 : 6),
                      height: current ? 8 : 6,
                      decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(99),
                        color: active
                            ? scheme.primary
                            : scheme.outline.withValues(alpha: 0.22),
                      ),
                    ),
                  );
                }),
              ),
            ).fadeSlideIn(slideY: 0.02, duration: AppAnimations.fast),
            Expanded(
              child: controller.bootstrapping.value
                  ? const Center(child: CircularProgressIndicator())
                  : ListView(
                      padding: const EdgeInsets.all(16),
                      children: [
                        AnimatedSwitcher(
                          duration: const Duration(milliseconds: 320),
                          switchInCurve: Curves.easeOutCubic,
                          switchOutCurve: Curves.easeInCubic,
                          transitionBuilder: (child, animation) {
                            final offset = Tween<Offset>(
                              begin: const Offset(0.04, 0.06),
                              end: Offset.zero,
                            ).animate(animation);
                            return FadeTransition(
                              opacity: animation,
                              child: SlideTransition(
                                position: offset,
                                child: child,
                              ),
                            );
                          },
                          child: KeyedSubtree(
                            key: ValueKey(step),
                            child: switch (step) {
                              0 => _TypeStep(controller: controller),
                              1 => _PartyStep(controller: controller),
                              2 => _ItemsStep(controller: controller),
                              3 => _PaymentStep(controller: controller),
                              _ => _ReviewStep(controller: controller),
                            },
                          ),
                        ),
                      ],
                    ),
            ),
            if (step >= 2)
              StickySummaryBar(
                label: 'net_amount'.tr(),
                amount: formatCurrency(controller.net),
              ).fadeSlideIn(slideY: 0.05, duration: AppAnimations.fast),
            SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
                child: Row(
                  children: [
                    if (step > 0)
                      Expanded(
                        child: OutlinedButton(
                          onPressed:
                              controller.saving.value ? null : controller.back,
                          child: Text('back'.tr()),
                        ),
                      ),
                    if (step > 0) const SizedBox(width: 12),
                    Expanded(
                      flex: 2,
                      child: AppProgressButton(
                        label: isLast ? 'save_invoice'.tr() : 'next'.tr(),
                        icon: isLast ? Icons.save_rounded : Icons.arrow_forward,
                        isLoading: controller.saving.value,
                        onPressed: controller.next,
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
    final options = [
      (0, 'purchase'.tr(), Icons.shopping_bag_outlined),
      (1, 'sale'.tr(), Icons.point_of_sale_outlined),
      (2, 'installment'.tr(), Icons.calendar_month_outlined),
      (3, 'purchase_return'.tr(), Icons.undo_rounded),
    ];

    return Obx(
      () => FormSectionCard(
        title: 'invoice_type'.tr(),
        children: [
          Text(
            'wizard_type_hint'.tr(),
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
          ),
          const SizedBox(height: 12),
          ...options.asMap().entries.map((entry) {
            final index = entry.key;
            final option = entry.value;
            final selected = controller.invoiceType.value == option.$1;
            return Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: Material(
                color: selected
                    ? Theme.of(context).colorScheme.primaryContainer
                    : Theme.of(context).colorScheme.surfaceContainerHighest
                        .withValues(alpha: 0.45),
                borderRadius: BorderRadius.circular(14),
                child: InkWell(
                  borderRadius: BorderRadius.circular(14),
                  onTap: () => controller.setInvoiceType(option.$1),
                  child: Padding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 14,
                      vertical: 14,
                    ),
                    child: Row(
                      children: [
                        Icon(
                          option.$3,
                          color: selected
                              ? Theme.of(context).colorScheme.onPrimaryContainer
                              : Theme.of(context).colorScheme.primary,
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Text(
                            option.$2,
                            style: Theme.of(context).textTheme.titleMedium,
                          ),
                        ),
                        if (selected)
                          Icon(
                            Icons.check_circle,
                            color: Theme.of(context).colorScheme.primary,
                          ),
                      ],
                    ),
                  ),
                ),
              ),
            ).fadeSlideInList(index: index, slideY: 0.06);
          }),
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
                _PickerButton(
                  icon: Icons.person_outline,
                  label: controller.customer.value?.displayName ??
                      'select_customer'.tr(),
                  filled: controller.customer.value != null,
                  onPressed: () => controller.pickLookup(
                    title: 'select_customer'.tr(),
                    loader: (search) =>
                        AppServices.data.getCustomers(search: search),
                    onSelected: (customer) =>
                        controller.customer.value = customer,
                  ),
                ),
                if (controller.customer.value?.balance != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 8),
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 12, vertical: 8),
                      decoration: BoxDecoration(
                        color: controller.customer.value!.balance! > 0
                            ? Colors.orange.shade50
                            : Colors.green.shade50,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Row(
                        children: [
                          Icon(
                            Icons.account_balance_wallet_outlined,
                            size: 18,
                            color: controller.customer.value!.balance! > 0
                                ? Colors.orange.shade800
                                : Colors.green.shade800,
                          ),
                          const SizedBox(width: 8),
                          Text(
                            '${'balance'.tr()}: ${formatCurrency(controller.customer.value!.balance!)}',
                            style: TextStyle(
                              fontWeight: FontWeight.w600,
                              color: controller.customer.value!.balance! > 0
                                  ? Colors.orange.shade900
                                  : Colors.green.shade900,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
              ],
            ).fadeSlideIn(slideY: 0.04),
          if (controller.needsSupplier)
            FormSectionCard(
              title: 'suppliers'.tr(),
              children: [
                _PickerButton(
                  icon: Icons.local_shipping_outlined,
                  label:
                      controller.supplier.value?.name ?? 'select_supplier'.tr(),
                  filled: controller.supplier.value != null,
                  onPressed: () => controller.pickLookup(
                    title: 'select_supplier'.tr(),
                    loader: (search) =>
                        AppServices.data.getSuppliers(search: search),
                    onSelected: (supplier) =>
                        controller.supplier.value = supplier,
                  ),
                ),
              ],
            ).fadeSlideIn(slideY: 0.05),
          FormSectionCard(
            title: 'warehouses'.tr(),
            children: [
              _PickerButton(
                icon: Icons.warehouse_outlined,
                label:
                    controller.warehouse.value?.name ?? 'select_warehouse'.tr(),
                filled: controller.warehouse.value != null,
                onPressed: () => controller.pickLookup(
                  title: 'select_warehouse'.tr(),
                  loader: (search) =>
                      AppServices.data.getWarehouses(search: search),
                  onSelected: (warehouse) =>
                      controller.warehouse.value = warehouse,
                ),
              ),
            ],
          ).fadeSlideIn(slideY: 0.06),
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
            child: FilledButton.tonalIcon(
              onPressed: controller.pickProduct,
              icon: const Icon(Icons.add_rounded),
              label: Text('add_product'.tr()),
            ),
          ).scaleIn(),
          const SizedBox(height: 12),
          if (controller.items.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 40),
              child: Column(
                children: [
                  Icon(
                    Icons.inventory_2_outlined,
                    size: 42,
                    color: Theme.of(context).colorScheme.outline,
                  ),
                  const SizedBox(height: 12),
                  Text(
                    'add_line_item'.tr(),
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                ],
              ),
            ).fadeSlideIn(),
          ...controller.items.asMap().entries.map((entry) {
            final item = entry.value;
            final index = entry.key;
            return Container(
              margin: const EdgeInsets.only(bottom: 10),
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(14),
                border: Border.all(
                  color: Theme.of(context)
                      .colorScheme
                      .outline
                      .withValues(alpha: 0.25),
                ),
              ),
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
              _PickerButton(
                icon: Icons.calendar_today_outlined,
                label:
                    '${'date'.tr()}: ${DateFormat.yMMMd().format(controller.date.value)}',
                filled: true,
                onPressed: controller.pickInvoiceDate,
              ),
              const SizedBox(height: 8),
              ...[
                (0, 'cash'.tr(), Icons.payments_outlined),
                (1, 'credit'.tr(), Icons.schedule_outlined),
                (2, 'installment'.tr(), Icons.calendar_view_month_outlined),
              ].asMap().entries.map((entry) {
                final index = entry.key;
                final option = entry.value;
                final selected = controller.paymentMethod.value == option.$1;
                return Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Material(
                    color: selected
                        ? Theme.of(context).colorScheme.secondaryContainer
                        : Theme.of(context)
                            .colorScheme
                            .surfaceContainerHighest
                            .withValues(alpha: 0.4),
                    borderRadius: BorderRadius.circular(14),
                    child: InkWell(
                      borderRadius: BorderRadius.circular(14),
                      onTap: () => controller.setPaymentMethod(option.$1),
                      child: ListTile(
                        leading: Icon(option.$3),
                        title: Text(option.$2),
                        trailing: selected
                            ? Icon(
                                Icons.check_circle,
                                color: Theme.of(context).colorScheme.primary,
                              )
                            : null,
                      ),
                    ),
                  ),
                ).fadeSlideInList(index: index, slideY: 0.05);
              }),
              if (controller.paymentMethod.value == 0) ...[
                const SizedBox(height: 4),
                _PickerButton(
                  icon: Icons.account_balance_wallet_outlined,
                  label:
                      controller.cashBox.value?.name ?? 'select_cashbox'.tr(),
                  filled: controller.cashBox.value != null,
                  onPressed: () => controller.pickLookup(
                    title: 'select_cashbox'.tr(),
                    loader: (search) =>
                        AppServices.data.getCashBoxes(search: search),
                    onSelected: (cashBox) =>
                        controller.cashBox.value = cashBox,
                  ),
                ),
              ],
              if (controller.paymentMethod.value == 1) ...[
                const SizedBox(height: 4),
                _PickerButton(
                  icon: Icons.event_outlined,
                  label: controller.creditDueDate.value == null
                      ? 'select_credit_due_date'.tr()
                      : '${'credit_due_date'.tr()}: ${DateFormat.yMMMd().format(controller.creditDueDate.value!)}',
                  filled: controller.creditDueDate.value != null,
                  onPressed: controller.pickCreditDueDate,
                ),
              ],
              const SizedBox(height: 8),
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
            'wizard_review_hint'.tr(),
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
          ).fadeSlideIn(slideY: 0.03),
          const SizedBox(height: 12),
          _ReviewRow(
            label: 'invoice_type'.tr(),
            value: invoiceTypeLabel(controller.invoiceType.value),
          ),
          _ReviewRow(
            label: 'date'.tr(),
            value: DateFormat.yMMMd().format(controller.date.value),
          ),
          _ReviewRow(
            label: 'payment_method'.tr(),
            value: paymentMethodLabel(controller.paymentMethod.value),
          ),
          if (controller.customer.value != null)
            _ReviewRow(
              label: 'customers'.tr(),
              value: controller.customer.value!.displayName,
            ),
          if (controller.supplier.value != null)
            _ReviewRow(
              label: 'suppliers'.tr(),
              value: controller.supplier.value!.name,
            ),
          if (controller.warehouse.value != null)
            _ReviewRow(
              label: 'warehouses'.tr(),
              value: controller.warehouse.value!.name,
            ),
          if (controller.paymentMethod.value == 0 &&
              controller.cashBox.value != null)
            _ReviewRow(
              label: 'cash_boxes'.tr(),
              value: controller.cashBox.value!.name,
            ),
          _ReviewRow(
            label: 'items'.tr(),
            value: '${controller.items.length}',
          ),
          _ReviewRow(
            label: 'net_amount'.tr(),
            value: formatCurrency(controller.net),
            emphasize: true,
          ),
          const SizedBox(height: 8),
          TextFormField(
            controller: controller.notesController,
            decoration: InputDecoration(labelText: 'notes'.tr()),
            maxLines: 2,
          ),
        ],
      ).scaleIn(),
    );
  }
}

class _ReviewRow extends StatelessWidget {
  const _ReviewRow({
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
        ? Theme.of(context).textTheme.titleMedium
        : Theme.of(context).textTheme.bodyLarge;
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        children: [
          Expanded(
            child: Text(
              label,
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: Theme.of(context).colorScheme.onSurfaceVariant,
                  ),
            ),
          ),
          Text(value, style: style),
        ],
      ),
    );
  }
}

class _PickerButton extends StatelessWidget {
  const _PickerButton({
    required this.icon,
    required this.label,
    required this.onPressed,
    this.filled = false,
  });

  final IconData icon;
  final String label;
  final VoidCallback onPressed;
  final bool filled;

  @override
  Widget build(BuildContext context) {
    return OutlinedButton.icon(
      onPressed: onPressed,
      icon: Icon(icon),
      style: OutlinedButton.styleFrom(
        alignment: AlignmentDirectional.centerStart,
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
        foregroundColor: filled
            ? Theme.of(context).colorScheme.onSurface
            : Theme.of(context).colorScheme.primary,
      ),
      label: Text(label),
    );
  }
}
