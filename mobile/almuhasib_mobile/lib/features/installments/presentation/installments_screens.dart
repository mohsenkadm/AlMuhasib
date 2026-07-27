import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/models/master_data_models.dart';
import '../../../shared/models/mobile_models.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/lookup_picker_sheet.dart';
import '../../operations/presentation/forms/finance/finance_entity_forms.dart';

class InstallmentsController extends GetxController {
  final isLoading = true.obs;
  final Rxn<Object> error = Rxn<Object>();
  final items = <InstallmentListItem>[].obs;
  final filter = 'all'.obs;
  final searchController = TextEditingController();

  @override
  void onInit() {
    super.onInit();
    final arg = Get.parameters['status'] ?? Get.arguments;
    if (arg is String && arg.isNotEmpty) {
      filter.value = arg;
    }
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      final status = filter.value == 'all' ? null : filter.value;
      final r = await AppServices.finance.getInstallments(
        status: status,
        search: searchController.text.trim().isEmpty
            ? null
            : searchController.text.trim(),
      );
      items.assignAll(r.items);
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }

  void setFilter(String value) {
    filter.value = value;
    load();
  }

  Future<void> notifyOverdue() async {
    try {
      final result = await AppServices.finance.notifyOverdueInstallments();
      final message = result['message']?.toString() ?? 'done'.tr();
      AppExceptionHandler.showSuccess(message);
    } catch (e) {
      AppExceptionHandler.showError(AppExceptionHandler.messageFor(e));
    }
  }

  @override
  void onClose() {
    searchController.dispose();
    super.onClose();
  }
}

class InstallmentsScreen extends GetView<InstallmentsController> {
  const InstallmentsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final filters = [
      ('all', 'filter_all'),
      ('overdue', 'installments_overdue'),
      ('upcoming', 'installments_upcoming'),
      ('unpaid', 'installments_unpaid'),
      ('paid', 'installments_paid'),
    ];

    return AppPageScaffold(
      title: 'installments'.tr(),
      subtitle: 'installments_subtitle'.tr(),
      actions: [
        IconButton(
          tooltip: 'notify_overdue'.tr(),
          onPressed: controller.notifyOverdue,
          icon: const Icon(Icons.notifications_active_outlined),
        ),
      ],
      body: Column(
        children: [
          SizedBox(
            height: 48,
            child: Obx(
              () => ListView(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(horizontal: 16),
                children: [
                  for (final f in filters)
                    Padding(
                      padding: const EdgeInsets.only(right: 8),
                      child: FilterChip(
                        selected: controller.filter.value == f.$1,
                        label: Text(f.$2.tr()),
                        onSelected: (_) => controller.setFilter(f.$1),
                      ),
                    ),
                ],
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 4, 20, 8),
            child: TextField(
              controller: controller.searchController,
              onSubmitted: (_) => controller.load(),
              decoration: InputDecoration(
                hintText: 'search_hint'.tr(),
                prefixIcon: const Icon(Icons.search),
              ),
            ),
          ),
          Expanded(
            child: Obx(() {
              if (controller.isLoading.value) {
                return const Center(child: CircularProgressIndicator());
              }
              if (controller.error.value != null) {
                return Center(
                  child: Text(
                    AppExceptionHandler.messageFor(controller.error.value),
                  ),
                );
              }
              if (controller.items.isEmpty) {
                return Center(child: Text('no_data'.tr()));
              }
              return RefreshIndicator(
                onRefresh: controller.load,
                child: ListView.separated(
                  padding: const EdgeInsets.fromLTRB(20, 8, 20, 120),
                  itemCount: controller.items.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 10),
                  itemBuilder: (context, index) {
                    final item = controller.items[index];
                    final overdue = item.status == 3 ||
                        (item.remainingAmount > 0 &&
                            item.dueDate.isBefore(DateTime.now()));
                    return AppEntityCard(
                      title: item.customerName,
                      subtitle:
                          '${formatDate(item.dueDate)} • ${installmentStatusLabel(item.status)}${item.fileNumber != null ? ' • ${item.fileNumber}' : ''}',
                      leading: Container(
                        width: 44,
                        height: 44,
                        decoration: BoxDecoration(
                          color: (overdue ? AppColors.warning : AppColors.primary)
                              .withValues(alpha: 0.14),
                          shape: BoxShape.circle,
                        ),
                        child: Icon(
                          overdue
                              ? Icons.warning_amber_rounded
                              : Icons.event_note_outlined,
                          color: overdue ? AppColors.warning : AppColors.primary,
                        ),
                      ),
                      trailing: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          Text(
                            formatCurrency(item.remainingAmount),
                            style: const TextStyle(fontWeight: FontWeight.w800),
                          ),
                          if (item.remainingAmount > 0)
                            TextButton(
                              onPressed: () async {
                                final ok = await Get.toNamed(
                                  AppRoutes.installmentPayPath(item.syncId),
                                  arguments: item,
                                );
                                if (ok == true) controller.load();
                              },
                              child: Text('pay'.tr()),
                            ),
                        ],
                      ),
                      onTap: () => Get.toNamed(
                        AppRoutes.installmentPlanDetailPath(item.planSyncId),
                      ),
                    );
                  },
                ),
              );
            }),
          ),
        ],
      ),
    );
  }
}

class InstallmentPlanDetailController extends GetxController {
  InstallmentPlanDetailController({required this.syncId});
  final String syncId;
  final isLoading = true.obs;
  final Rxn<Object> error = Rxn<Object>();
  final Rxn<InstallmentPlanDetail> plan = Rxn<InstallmentPlanDetail>();

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      plan.value = await AppServices.finance.getInstallmentPlan(syncId);
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}

class InstallmentPlanDetailScreen
    extends GetView<InstallmentPlanDetailController> {
  const InstallmentPlanDetailScreen({super.key, required this.syncId});
  final String syncId;

  @override
  String? get tag => 'plan_$syncId';

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: 'installment_plan'.tr(),
      body: Obx(() {
        if (controller.isLoading.value) {
          return const Center(child: CircularProgressIndicator());
        }
        final p = controller.plan.value;
        if (p == null) {
          return Center(
            child: Text(AppExceptionHandler.messageFor(controller.error.value)),
          );
        }
        return RefreshIndicator(
          onRefresh: controller.load,
          child: ListView(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 120),
            children: [
              AppBalanceHeroCard(
                title: p.customerName,
                value: formatCurrency(p.totalAmount),
                subtitle:
                    '${p.invoiceNumber} • ${p.numberOfInstallments} ${'installments'.tr()}',
              ),
              const SizedBox(height: 16),
              ...p.installments.map(
                (i) => Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: AppEntityCard(
                    title: formatDate(i.dueDate),
                    subtitle:
                        '${installmentStatusLabel(i.status)} • ${'paid'.tr()}: ${formatCurrency(i.paidAmount)}',
                    trailing: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Text(
                          formatCurrency(i.remainingAmount),
                          style: const TextStyle(fontWeight: FontWeight.w800),
                        ),
                        if (i.remainingAmount > 0)
                          TextButton(
                            onPressed: () async {
                              final ok = await Get.toNamed(
                                AppRoutes.installmentPayPath(i.syncId),
                                arguments: i,
                              );
                              if (ok == true) controller.load();
                            },
                            child: Text('pay'.tr()),
                          ),
                      ],
                    ),
                  ),
                ),
              ),
            ],
          ),
        );
      }),
    );
  }
}

class InstallmentPayController extends GetxController {
  InstallmentPayController({required this.syncId, this.item});

  final String syncId;
  final InstallmentListItem? item;
  final formKey = GlobalKey<FormState>();
  final amountController = TextEditingController();
  final notesController = TextEditingController();
  final cashBox = Rxn<LookupItem>();
  final paymentDate = DateTime.now().obs;
  final saving = false.obs;

  @override
  void onInit() {
    super.onInit();
    if (item != null) {
      amountController.text = item!.remainingAmount.toStringAsFixed(2);
    }
    _preload();
  }

  Future<void> _preload() async {
    try {
      final boxes = await AppServices.data.getCashBoxes();
      if (boxes.length == 1) cashBox.value = boxes.first;
    } catch (_) {}
  }

  Future<void> pickCashBox(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_cashbox'.tr(),
      loadItems: (s) => AppServices.data.getCashBoxes(search: s),
    );
    if (selected != null) cashBox.value = selected;
  }

  Future<void> save() async {
    if (!formKey.currentState!.validate()) return;
    if (cashBox.value == null) {
      AppExceptionHandler.showError('select_cashbox'.tr());
      return;
    }
    final amount = double.tryParse(amountController.text);
    if (amount == null || amount <= 0) {
      AppExceptionHandler.showError('invalid_amount'.tr());
      return;
    }
    saving.value = true;
    try {
      final response = await AppServices.operations.payInstallment(
        syncId,
        PayInstallmentRequest(
          amount: amount,
          cashBoxSyncId: cashBox.value!.syncId,
          paymentDate: paymentDate.value,
          notes: notesController.text.trim().isEmpty
              ? null
              : notesController.text.trim(),
        ),
      );
      if (response.conflicts.isNotEmpty) {
        AppExceptionHandler.showConflicts(response.conflicts);
        return;
      }
      AppExceptionHandler.showSuccess(response.message);
      Get.back(result: true);
    } catch (e) {
      AppExceptionHandler.showError(e);
    } finally {
      saving.value = false;
    }
  }

  @override
  void onClose() {
    amountController.dispose();
    notesController.dispose();
    super.onClose();
  }
}

class InstallmentPayScreen extends GetView<InstallmentPayController> {
  const InstallmentPayScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppFormPage(
      title: 'pay_installment'.tr(),
      formKey: controller.formKey,
      saveLabel: 'pay'.tr(),
      onSave: controller.save,
      isSaving: controller.saving,
      sections: [
        AppFormSection(
          title: 'payment_info'.tr(),
          children: [
            if (controller.item != null) ...[
              Text(
                controller.item!.customerName,
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
              ),
              Text(
                '${'remaining'.tr()}: ${formatCurrency(controller.item!.remainingAmount)}',
              ),
              const SizedBox(height: AppSpacing.md),
            ],
            AppTextField(
              controller: controller.amountController,
              label: 'amount'.tr(),
              prefixIcon: Icons.payments_outlined,
              keyboardType:
                  const TextInputType.numberWithOptions(decimal: true),
              validator: (v) =>
                  v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            Obx(
              () => ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.account_balance_wallet_outlined),
                title: Text('cash_box'.tr()),
                subtitle: Text(
                  controller.cashBox.value?.name ?? 'select_cashbox'.tr(),
                ),
                onTap: () => controller.pickCashBox(context),
              ),
            ),
            AppTextField(
              controller: controller.notesController,
              label: 'notes'.tr(),
              prefixIcon: Icons.notes_outlined,
              maxLines: 2,
            ),
          ],
        ),
      ],
    );
  }
}
