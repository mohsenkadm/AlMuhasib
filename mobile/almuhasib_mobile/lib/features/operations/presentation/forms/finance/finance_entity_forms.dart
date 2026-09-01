import 'package:almuhasib_mobile/core/getx/app_services.dart';
import 'package:almuhasib_mobile/core/theme/app_spacing.dart';
import 'package:almuhasib_mobile/shared/models/master_data_models.dart';
import 'package:almuhasib_mobile/shared/models/mobile_models.dart';
import 'package:almuhasib_mobile/shared/utils/formatters.dart';
import 'package:almuhasib_mobile/shared/widgets/design_system/design_system.dart';
import 'package:almuhasib_mobile/shared/widgets/lookup_picker_sheet.dart';
import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

String voucherTypeLabel(int type) => switch (type) {
      0 => 'voucher_receipt'.tr(),
      1 => 'voucher_payment'.tr(),
      2 => 'voucher_bank_receipt'.tr(),
      3 => 'voucher_investor_deposit'.tr(),
      4 => 'voucher_investor_withdrawal'.tr(),
      5 => 'voucher_debt_receipt'.tr(),
      _ => 'voucher'.tr(),
    };

String installmentStatusLabel(int status) => switch (status) {
      0 => 'installment_status_pending'.tr(),
      1 => 'installment_status_partial'.tr(),
      2 => 'installment_status_paid'.tr(),
      3 => 'installment_status_overdue'.tr(),
      _ => 'status'.tr(),
    };

class CashBoxFormController extends GetxController {
  CashBoxFormController({this.syncId});
  final String? syncId;
  final formKey = GlobalKey<FormState>();
  final nameController = TextEditingController();
  final openingController = TextEditingController(text: '0');
  final saving = false.obs;
  bool get isEdit => syncId != null;

  Future<void> save() async {
    if (!formKey.currentState!.validate()) return;
    saving.value = true;
    try {
      final response = await AppServices.operations.upsertCashBox(
        UpsertCashBoxRequest(
          syncId: syncId,
          name: nameController.text.trim(),
          openingBalance: double.tryParse(openingController.text) ?? 0,
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
    nameController.dispose();
    openingController.dispose();
    super.onClose();
  }
}

class CashBoxFormScreen extends GetView<CashBoxFormController> {
  const CashBoxFormScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppFormPage(
      title: controller.isEdit ? 'edit_cash_box'.tr() : 'new_cash_box'.tr(),
      formKey: controller.formKey,
      saveLabel: 'save'.tr(),
      onSave: controller.save,
      isSaving: controller.saving,
      sections: [
        AppFormSection(
          title: 'cash_box_info'.tr(),
          children: [
            AppTextField(
              controller: controller.nameController,
              label: 'name'.tr(),
              prefixIcon: Icons.account_balance_wallet_outlined,
              validator: (v) =>
                  v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.openingController,
              label: 'opening_balance'.tr(),
              prefixIcon: Icons.payments_outlined,
              keyboardType:
                  const TextInputType.numberWithOptions(decimal: true),
            ),
          ],
        ),
      ],
    );
  }
}

class BankAccountFormController extends GetxController {
  BankAccountFormController({this.syncId});
  final String? syncId;
  final formKey = GlobalKey<FormState>();
  final nameController = TextEditingController();
  final accountController = TextEditingController();
  final openingController = TextEditingController(text: '0');
  final saving = false.obs;
  bool get isEdit => syncId != null;

  Future<void> save() async {
    if (!formKey.currentState!.validate()) return;
    saving.value = true;
    try {
      final response = await AppServices.operations.upsertBankAccount(
        UpsertBankAccountRequest(
          syncId: syncId,
          name: nameController.text.trim(),
          accountNumber: accountController.text.trim().isEmpty
              ? null
              : accountController.text.trim(),
          openingBalance: double.tryParse(openingController.text) ?? 0,
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
    nameController.dispose();
    accountController.dispose();
    openingController.dispose();
    super.onClose();
  }
}

class BankAccountFormScreen extends GetView<BankAccountFormController> {
  const BankAccountFormScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppFormPage(
      title: controller.isEdit
          ? 'edit_bank_account'.tr()
          : 'new_bank_account'.tr(),
      formKey: controller.formKey,
      saveLabel: 'save'.tr(),
      onSave: controller.save,
      isSaving: controller.saving,
      sections: [
        AppFormSection(
          title: 'bank_account_info'.tr(),
          children: [
            AppTextField(
              controller: controller.nameController,
              label: 'name'.tr(),
              prefixIcon: Icons.account_balance_outlined,
              validator: (v) =>
                  v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.accountController,
              label: 'account_number'.tr(),
              prefixIcon: Icons.numbers_outlined,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.openingController,
              label: 'opening_balance'.tr(),
              prefixIcon: Icons.payments_outlined,
              keyboardType:
                  const TextInputType.numberWithOptions(decimal: true),
            ),
          ],
        ),
      ],
    );
  }
}

class ExpenseTypeFormController extends GetxController {
  ExpenseTypeFormController({this.syncId});
  final String? syncId;
  final formKey = GlobalKey<FormState>();
  final nameController = TextEditingController();
  final saving = false.obs;
  bool get isEdit => syncId != null;

  Future<void> save() async {
    if (!formKey.currentState!.validate()) return;
    saving.value = true;
    try {
      final response = await AppServices.operations.upsertExpenseType(
        UpsertExpenseTypeRequest(
          syncId: syncId,
          name: nameController.text.trim(),
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
    nameController.dispose();
    super.onClose();
  }
}

class ExpenseTypeFormScreen extends GetView<ExpenseTypeFormController> {
  const ExpenseTypeFormScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppFormPage(
      title: controller.isEdit
          ? 'edit_expense_type'.tr()
          : 'new_expense_type'.tr(),
      formKey: controller.formKey,
      saveLabel: 'save'.tr(),
      onSave: controller.save,
      isSaving: controller.saving,
      sections: [
        AppFormSection(
          title: 'expense_type_info'.tr(),
          children: [
            AppTextField(
              controller: controller.nameController,
              label: 'name'.tr(),
              prefixIcon: Icons.category_outlined,
              validator: (v) =>
                  v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
            ),
          ],
        ),
      ],
    );
  }
}

class WarehouseFormController extends GetxController {
  WarehouseFormController({this.syncId});
  final String? syncId;
  final formKey = GlobalKey<FormState>();
  final nameController = TextEditingController();
  final locationController = TextEditingController();
  final saving = false.obs;
  bool get isEdit => syncId != null;

  Future<void> save() async {
    if (!formKey.currentState!.validate()) return;
    saving.value = true;
    try {
      final response = await AppServices.operations.upsertWarehouse(
        UpsertWarehouseRequest(
          syncId: syncId,
          name: nameController.text.trim(),
          location: locationController.text.trim().isEmpty
              ? null
              : locationController.text.trim(),
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
    nameController.dispose();
    locationController.dispose();
    super.onClose();
  }
}

class WarehouseFormScreen extends GetView<WarehouseFormController> {
  const WarehouseFormScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppFormPage(
      title: controller.isEdit ? 'edit_warehouse'.tr() : 'new_warehouse'.tr(),
      formKey: controller.formKey,
      saveLabel: 'save'.tr(),
      onSave: controller.save,
      isSaving: controller.saving,
      sections: [
        AppFormSection(
          title: 'warehouse_info'.tr(),
          children: [
            AppTextField(
              controller: controller.nameController,
              label: 'name'.tr(),
              prefixIcon: Icons.warehouse_outlined,
              validator: (v) =>
                  v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.locationController,
              label: 'location'.tr(),
              prefixIcon: Icons.location_on_outlined,
            ),
          ],
        ),
      ],
    );
  }
}

class VoucherFormController extends GetxController {
  final formKey = GlobalKey<FormState>();
  final amountController = TextEditingController();
  final bankFeesController = TextEditingController(text: '0');
  final notesController = TextEditingController();
  final voucherType = 0.obs;
  final date = DateTime.now().obs;
  final customer = Rxn<LookupItem>();
  final investor = Rxn<LookupItem>();
  final cashBox = Rxn<LookupItem>();
  final bankAccount = Rxn<LookupItem>();
  final saving = false.obs;

  @override
  void onInit() {
    super.onInit();
    _preload();
  }

  Future<void> _preload() async {
    try {
      final boxes = await AppServices.data.getCashBoxes();
      if (boxes.length == 1) cashBox.value = boxes.first;
    } catch (_) {}
  }

  Future<void> pickCustomer(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_customer'.tr(),
      loadItems: (s) => AppServices.data.getCustomers(search: s),
    );
    if (selected != null) customer.value = selected;
  }

  Future<void> pickInvestor(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_investor'.tr(),
      loadItems: (s) => AppServices.data.getInvestors(search: s),
    );
    if (selected != null) investor.value = selected;
  }

  Future<void> pickCashBox(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_cashbox'.tr(),
      loadItems: (s) => AppServices.data.getCashBoxes(search: s),
    );
    if (selected != null) cashBox.value = selected;
  }

  Future<void> pickBank(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_bank_account'.tr(),
      loadItems: (s) => AppServices.data.getBankAccounts(search: s),
    );
    if (selected != null) bankAccount.value = selected;
  }

  Future<void> pickDate(BuildContext context) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: date.value,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (picked != null) date.value = picked;
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
      final response = await AppServices.operations.createVoucher(
        CreateVoucherRequest(
          voucherType: voucherType.value,
          amount: amount,
          bankFees: double.tryParse(bankFeesController.text) ?? 0,
          customerSyncId: customer.value?.syncId,
          investorSyncId: investor.value?.syncId,
          cashBoxSyncId: cashBox.value!.syncId,
          bankAccountSyncId: bankAccount.value?.syncId,
          date: date.value,
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
    bankFeesController.dispose();
    notesController.dispose();
    super.onClose();
  }
}

class VoucherFormScreen extends GetView<VoucherFormController> {
  const VoucherFormScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppFormPage(
      title: 'new_voucher'.tr(),
      formKey: controller.formKey,
      saveLabel: 'save'.tr(),
      onSave: controller.save,
      isSaving: controller.saving,
      sections: [
        AppFormSection(
          title: 'voucher_info'.tr(),
          children: [
            Obx(
              () => DropdownButtonFormField<int>(
                value: controller.voucherType.value,
                decoration: InputDecoration(labelText: 'voucher_type'.tr()),
                items: List.generate(
                  6,
                  (i) => DropdownMenuItem(
                    value: i,
                    child: Text(voucherTypeLabel(i)),
                  ),
                ),
                onChanged: (v) {
                  if (v != null) controller.voucherType.value = v;
                },
              ),
            ),
            const SizedBox(height: AppSpacing.md),
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
            AppTextField(
              controller: controller.bankFeesController,
              label: 'bank_fees'.tr(),
              prefixIcon: Icons.account_balance_outlined,
              keyboardType:
                  const TextInputType.numberWithOptions(decimal: true),
            ),
            const SizedBox(height: AppSpacing.md),
            Obx(
              () => ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.event_rounded),
                title: Text('date'.tr()),
                subtitle: Text(formatDate(controller.date.value)),
                onTap: () => controller.pickDate(context),
              ),
            ),
            Obx(
              () => ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.person_outline),
                title: Text('customer'.tr()),
                subtitle: Text(
                  controller.customer.value?.displayName ?? 'select_customer'.tr(),
                ),
                onTap: () => controller.pickCustomer(context),
              ),
            ),
            Obx(
              () => ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.savings_outlined),
                title: Text('investor'.tr()),
                subtitle: Text(
                  controller.investor.value?.name ?? 'select_investor'.tr(),
                ),
                onTap: () => controller.pickInvestor(context),
              ),
            ),
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
            Obx(
              () => ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.account_balance_outlined),
                title: Text('bank_account'.tr()),
                subtitle: Text(
                  controller.bankAccount.value?.name ??
                      'select_bank_account'.tr(),
                ),
                onTap: () => controller.pickBank(context),
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            AppTextField(
              controller: controller.notesController,
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
