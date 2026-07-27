import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/real_estate_expenses_controller.dart';
import '../models/real_estate_models.dart';

class RealEstateExpensesScreen
    extends GetView<RealEstateExpensesController> {
  const RealEstateExpensesScreen({super.key});

  @override
  final String? tag = 'real_estate_expenses';

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: 'real_estate_expenses_title'.tr(),
      actions: [
        IconButton(
          onPressed: () => _openCreateSheet(context),
          icon: const Icon(Icons.add_rounded),
          tooltip: 'real_estate_expense_new'.tr(),
        ),
      ],
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: Obx(
              () => Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: controller.pickFrom,
                      icon: const Icon(Icons.date_range, size: 18),
                      label: Text(formatDate(controller.from.value)),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: controller.pickTo,
                      icon: const Icon(Icons.event, size: 18),
                      label: Text(formatDate(controller.to.value)),
                    ),
                  ),
                ],
              ),
            ),
          ),
          Expanded(
            child: Obx(() {
              if (controller.isLoading.value && controller.page.value == null) {
                return const Center(child: CircularProgressIndicator());
              }
              if (controller.error.value != null &&
                  controller.page.value == null) {
                return ErrorStateWidget(
                  message: AppExceptionHandler.messageFor(
                    controller.error.value,
                  ),
                  onRetry: controller.load,
                );
              }

              final page = controller.page.value;
              if (page == null || page.items.isEmpty) {
                return EmptyStateWidget(
                  message: 'real_estate_no_expenses'.tr(),
                  onRetry: controller.load,
                );
              }

              return RefreshIndicator(
                onRefresh: controller.load,
                child: ListView(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 100),
                  children: [
                    AppKpiGrid(
                      childAspectRatio: 1.6,
                      items: [
                        AppKpiItem(
                          title: 'real_estate_expense_count'.tr(),
                          value: '${page.totalCount}',
                          icon: Icons.receipt_long_outlined,
                          color: AppColors.moduleCyan,
                        ),
                        AppKpiItem(
                          title: 'real_estate_expense_total'.tr(),
                          value: formatCurrency(page.totalAmount),
                          icon: Icons.payments_outlined,
                          color: AppColors.warning,
                        ),
                      ],
                    ),
                    const SizedBox(height: 16),
                    ...page.items.map((e) => _ExpenseTile(item: e)),
                  ],
                ),
              );
            }),
          ),
        ],
      ),
    );
  }

  void _openCreateSheet(BuildContext context) {
    final c = controller;
    if (c.types.isNotEmpty) {
      c.selectedTypeForForm.value = c.types.first;
    }
    Get.bottomSheet(
      Container(
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
          color: Theme.of(context).scaffoldBackgroundColor,
          borderRadius: const BorderRadius.vertical(top: Radius.circular(20)),
        ),
        child: SafeArea(
          child: SingleChildScrollView(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'real_estate_expense_new'.tr(),
                  style: Theme.of(context).textTheme.titleLarge?.copyWith(
                        fontWeight: FontWeight.w800,
                      ),
                ),
                const SizedBox(height: 16),
                Obx(
                  () => DropdownButtonFormField<RealEstateExpenseTypeDto>(
                    value: c.selectedTypeForForm.value,
                    items: [
                      for (final t in c.types)
                        DropdownMenuItem(value: t, child: Text(t.name)),
                    ],
                    onChanged: (v) => c.selectedTypeForForm.value = v,
                    decoration: InputDecoration(
                      labelText: 'real_estate_expense_type'.tr(),
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: c.amountCtrl,
                  keyboardType: TextInputType.number,
                  decoration: InputDecoration(
                    labelText: 'amount'.tr(),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: c.descriptionCtrl,
                  decoration: InputDecoration(
                    labelText: 'description'.tr(),
                  ),
                ),
                const SizedBox(height: 16),
                FilledButton(
                  onPressed: c.createExpense,
                  child: Text('save'.tr()),
                ),
              ],
            ),
          ),
        ),
      ),
      isScrollControlled: true,
    );
  }
}

class _ExpenseTile extends StatelessWidget {
  const _ExpenseTile({required this.item});

  final RealEstateExpenseItem item;

  @override
  Widget build(BuildContext context) {
    final controller = Get.find<RealEstateExpensesController>(
      tag: 'real_estate_expenses',
    );
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: AppEntityCard(
        title: item.expenseTypeName,
        subtitle:
            '${formatDate(item.expenseDate)}\n${item.description.isEmpty ? '—' : item.description}',
        leading: Container(
          width: 46,
          height: 46,
          decoration: BoxDecoration(
            color: AppColors.warning.withValues(alpha: 0.12),
            shape: BoxShape.circle,
          ),
          child: const Icon(Icons.money_off_csred_rounded, color: AppColors.warning),
        ),
        trailing: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              formatCurrency(item.amount),
              style: const TextStyle(fontWeight: FontWeight.w800),
            ),
            IconButton(
              visualDensity: VisualDensity.compact,
              onPressed: () => controller.deleteExpense(item),
              icon: const Icon(Icons.delete_outline, size: 20),
            ),
          ],
        ),
      ),
    );
  }
}
