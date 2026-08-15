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

class ExpenseFormController extends GetxController {
  final formKey = GlobalKey<FormState>();
  final amountController = TextEditingController();
  final notesController = TextEditingController();
  final date = DateTime.now().obs;
  final expenseType = Rxn<LookupItem>();
  final cashBox = Rxn<LookupItem>();
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
      final types = await AppServices.data.getExpenseTypes();
      if (types.length == 1) expenseType.value = types.first;
    } catch (_) {}
  }

  Future<void> pickType(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_expense_type'.tr(),
      loadItems: (s) => AppServices.data.getExpenseTypes(search: s),
    );
    if (selected != null) expenseType.value = selected;
  }

  Future<void> pickCashBox(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_cashbox'.tr(),
      loadItems: (s) => AppServices.data.getCashBoxes(search: s),
    );
    if (selected != null) cashBox.value = selected;
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
    if (expenseType.value == null) {
      AppExceptionHandler.showError('select_expense_type'.tr());
      return;
    }
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
      final response = await AppServices.operations.createExpense(
        CreateExpenseRequest(
          expenseTypeSyncId: expenseType.value!.syncId,
          amount: amount,
          date: date.value,
          cashBoxSyncId: cashBox.value!.syncId,
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

class ExpenseFormScreen extends GetView<ExpenseFormController> {
  const ExpenseFormScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppFormPage(
      title: 'new_expense'.tr(),
      formKey: controller.formKey,
      saveLabel: 'save'.tr(),
      onSave: controller.save,
      isSaving: controller.saving,
      sections: [
        AppFormSection(
          title: 'expense_info'.tr(),
          children: [
            Obx(
              () => ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.category_outlined),
                title: Text('expense_type'.tr()),
                subtitle: Text(
                  controller.expenseType.value?.name ??
                      'select_expense_type'.tr(),
                ),
                onTap: () => controller.pickType(context),
              ),
            ),
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
                leading: const Icon(Icons.event_rounded),
                title: Text('date'.tr()),
                subtitle: Text(formatDate(controller.date.value)),
                onTap: () => controller.pickDate(context),
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

class TransferFormController extends GetxController {
  final formKey = GlobalKey<FormState>();
  final amountController = TextEditingController();
  final notesController = TextEditingController();
  final fromType = 0.obs;
  final toType = 1.obs;
  final fromAccount = Rxn<LookupItem>();
  final toAccount = Rxn<LookupItem>();
  final date = DateTime.now().obs;
  final saving = false.obs;

  Future<void> pickFrom(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: fromType.value == 0
          ? 'select_cashbox'.tr()
          : 'select_bank_account'.tr(),
      loadItems: (s) => fromType.value == 0
          ? AppServices.data.getCashBoxes(search: s)
          : AppServices.data.getBankAccounts(search: s),
    );
    if (selected != null) fromAccount.value = selected;
  }

  Future<void> pickTo(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: toType.value == 0
          ? 'select_cashbox'.tr()
          : 'select_bank_account'.tr(),
      loadItems: (s) => toType.value == 0
          ? AppServices.data.getCashBoxes(search: s)
          : AppServices.data.getBankAccounts(search: s),
    );
    if (selected != null) toAccount.value = selected;
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
    if (fromAccount.value == null || toAccount.value == null) {
      AppExceptionHandler.showError('select_accounts'.tr());
      return;
    }
    final amount = double.tryParse(amountController.text);
    if (amount == null || amount <= 0) {
      AppExceptionHandler.showError('invalid_amount'.tr());
      return;
    }
    saving.value = true;
    try {
      final response = await AppServices.operations.createTransfer(
        CreateTransferRequest(
          fromType: fromType.value,
          fromSyncId: fromAccount.value!.syncId,
          toType: toType.value,
          toSyncId: toAccount.value!.syncId,
          amount: amount,
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
    notesController.dispose();
    super.onClose();
  }
}

class TransferFormScreen extends GetView<TransferFormController> {
  const TransferFormScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppFormPage(
      title: 'new_transfer'.tr(),
      formKey: controller.formKey,
      saveLabel: 'save'.tr(),
      onSave: controller.save,
      isSaving: controller.saving,
      sections: [
        AppFormSection(
          title: 'transfer_info'.tr(),
          children: [
            Obx(
              () => DropdownButtonFormField<int>(
                value: controller.fromType.value,
                decoration: InputDecoration(labelText: 'from_type'.tr()),
                items: [
                  DropdownMenuItem(value: 0, child: Text('cash_box'.tr())),
                  DropdownMenuItem(value: 1, child: Text('bank_account'.tr())),
                ],
                onChanged: (v) {
                  if (v != null) {
                    controller.fromType.value = v;
                    controller.fromAccount.value = null;
                  }
                },
              ),
            ),
            Obx(
              () => ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.outbox_outlined),
                title: Text('from_account'.tr()),
                subtitle: Text(
                  controller.fromAccount.value?.name ?? 'select'.tr(),
                ),
                onTap: () => controller.pickFrom(context),
              ),
            ),
            Obx(
              () => DropdownButtonFormField<int>(
                value: controller.toType.value,
                decoration: InputDecoration(labelText: 'to_type'.tr()),
                items: [
                  DropdownMenuItem(value: 0, child: Text('cash_box'.tr())),
                  DropdownMenuItem(value: 1, child: Text('bank_account'.tr())),
                ],
                onChanged: (v) {
                  if (v != null) {
                    controller.toType.value = v;
                    controller.toAccount.value = null;
                  }
                },
              ),
            ),
            Obx(
              () => ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.move_to_inbox_outlined),
                title: Text('to_account'.tr()),
                subtitle:
                    Text(controller.toAccount.value?.name ?? 'select'.tr()),
                onTap: () => controller.pickTo(context),
              ),
            ),
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
                leading: const Icon(Icons.event_rounded),
                title: Text('date'.tr()),
                subtitle: Text(formatDate(controller.date.value)),
                onTap: () => controller.pickDate(context),
              ),
            ),
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

class TransferLineItem {
  TransferLineItem({
    required this.product,
    required this.quantity,
  });

  final LookupItem product;
  double quantity;
}

class WarehouseTransferFormController extends GetxController {
  final formKey = GlobalKey<FormState>();
  final notesController = TextEditingController();
  final fromWarehouse = Rxn<LookupItem>();
  final toWarehouse = Rxn<LookupItem>();
  final date = DateTime.now().obs;
  final items = <TransferLineItem>[].obs;
  final saving = false.obs;

  Future<void> pickFrom(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_warehouse'.tr(),
      loadItems: (s) => AppServices.data.getWarehouses(search: s),
    );
    if (selected != null) fromWarehouse.value = selected;
  }

  Future<void> pickTo(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_warehouse'.tr(),
      loadItems: (s) => AppServices.data.getWarehouses(search: s),
    );
    if (selected != null) toWarehouse.value = selected;
  }

  Future<void> addItem(BuildContext context) async {
    final product = await showLookupPickerSheet<ProductLookupItem>(
      context: context,
      title: 'select_product'.tr(),
      loadItems: (s) => AppServices.data.getProducts(search: s),
    );
    if (product == null) return;
    final qtyController = TextEditingController(text: '1');
    final ok = await Get.dialog<bool>(
      AlertDialog(
        title: Text(product.name),
        content: TextField(
          controller: qtyController,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          decoration: InputDecoration(labelText: 'quantity'.tr()),
        ),
        actions: [
          TextButton(
            onPressed: () => Get.back(result: false),
            child: Text('cancel'.tr()),
          ),
          FilledButton(
            onPressed: () => Get.back(result: true),
            child: Text('add'.tr()),
          ),
        ],
      ),
    );
    if (ok == true) {
      final qty = double.tryParse(qtyController.text) ?? 0;
      if (qty > 0) {
        items.add(TransferLineItem(product: product, quantity: qty));
      }
    }
    qtyController.dispose();
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
    if (fromWarehouse.value == null || toWarehouse.value == null) {
      AppExceptionHandler.showError('select_warehouse'.tr());
      return;
    }
    if (items.isEmpty) {
      AppExceptionHandler.showError('add_line_item'.tr());
      return;
    }
    saving.value = true;
    try {
      final response = await AppServices.operations.createWarehouseTransfer(
        CreateWarehouseTransferRequest(
          fromWarehouseSyncId: fromWarehouse.value!.syncId,
          toWarehouseSyncId: toWarehouse.value!.syncId,
          date: date.value,
          notes: notesController.text.trim().isEmpty
              ? null
              : notesController.text.trim(),
          items: items
              .map(
                (e) => CreateWarehouseTransferItemRequest(
                  productSyncId: e.product.syncId,
                  quantity: e.quantity,
                ),
              )
              .toList(),
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
    notesController.dispose();
    super.onClose();
  }
}

class WarehouseTransferFormScreen
    extends GetView<WarehouseTransferFormController> {
  const WarehouseTransferFormScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppFormPage(
      title: 'new_warehouse_transfer'.tr(),
      formKey: controller.formKey,
      saveLabel: 'save'.tr(),
      onSave: controller.save,
      isSaving: controller.saving,
      sections: [
        AppFormSection(
          title: 'warehouse_transfer_info'.tr(),
          children: [
            Obx(
              () => ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.warehouse_outlined),
                title: Text('from_warehouse'.tr()),
                subtitle: Text(
                  controller.fromWarehouse.value?.name ??
                      'select_warehouse'.tr(),
                ),
                onTap: () => controller.pickFrom(context),
              ),
            ),
            Obx(
              () => ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.warehouse_outlined),
                title: Text('to_warehouse'.tr()),
                subtitle: Text(
                  controller.toWarehouse.value?.name ??
                      'select_warehouse'.tr(),
                ),
                onTap: () => controller.pickTo(context),
              ),
            ),
            Obx(
              () => ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.event_rounded),
                title: Text('date'.tr()),
                subtitle: Text(formatDate(controller.date.value)),
                onTap: () => controller.pickDate(context),
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
        AppFormSection(
          title: 'items'.tr(),
          children: [
            Obx(
              () => Column(
                children: [
                  ...controller.items.asMap().entries.map(
                        (e) => ListTile(
                          contentPadding: EdgeInsets.zero,
                          title: Text(e.value.product.name),
                          subtitle: Text(
                            '${'quantity'.tr()}: ${e.value.quantity}',
                          ),
                          trailing: IconButton(
                            icon: const Icon(Icons.delete_outline),
                            onPressed: () => controller.items.removeAt(e.key),
                          ),
                        ),
                      ),
                  OutlinedButton.icon(
                    onPressed: () => controller.addItem(context),
                    icon: const Icon(Icons.add),
                    label: Text('add_product_line'.tr()),
                  ),
                ],
              ),
            ),
          ],
        ),
      ],
    );
  }
}

class StockAdjLine {
  StockAdjLine({required this.product, required this.newQuantity});
  final LookupItem product;
  double newQuantity;
}

class StockAdjustmentFormController extends GetxController {
  final formKey = GlobalKey<FormState>();
  final notesController = TextEditingController();
  final warehouse = Rxn<LookupItem>();
  final items = <StockAdjLine>[].obs;
  final saving = false.obs;

  Future<void> pickWarehouse(BuildContext context) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_warehouse'.tr(),
      loadItems: (s) => AppServices.data.getWarehouses(search: s),
    );
    if (selected != null) warehouse.value = selected;
  }

  Future<void> addItem(BuildContext context) async {
    final product = await showLookupPickerSheet<ProductLookupItem>(
      context: context,
      title: 'select_product'.tr(),
      loadItems: (s) => AppServices.data.getProducts(search: s),
    );
    if (product == null) return;
    final qtyController = TextEditingController(text: '0');
    final ok = await Get.dialog<bool>(
      AlertDialog(
        title: Text(product.name),
        content: TextField(
          controller: qtyController,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          decoration: InputDecoration(labelText: 'new_quantity'.tr()),
        ),
        actions: [
          TextButton(
            onPressed: () => Get.back(result: false),
            child: Text('cancel'.tr()),
          ),
          FilledButton(
            onPressed: () => Get.back(result: true),
            child: Text('add'.tr()),
          ),
        ],
      ),
    );
    if (ok == true) {
      final qty = double.tryParse(qtyController.text) ?? 0;
      items.add(StockAdjLine(product: product, newQuantity: qty));
    }
    qtyController.dispose();
  }

  Future<void> save() async {
    if (warehouse.value == null) {
      AppExceptionHandler.showError('select_warehouse'.tr());
      return;
    }
    if (items.isEmpty) {
      AppExceptionHandler.showError('add_line_item'.tr());
      return;
    }
    saving.value = true;
    try {
      final response = await AppServices.operations.createStockAdjustment(
        CreateStockAdjustmentRequest(
          warehouseSyncId: warehouse.value!.syncId,
          notes: notesController.text.trim().isEmpty
              ? null
              : notesController.text.trim(),
          items: items
              .map(
                (e) => StockAdjustmentItemRequest(
                  productSyncId: e.product.syncId,
                  newQuantity: e.newQuantity,
                ),
              )
              .toList(),
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
    notesController.dispose();
    super.onClose();
  }
}

class StockAdjustmentFormScreen extends GetView<StockAdjustmentFormController> {
  const StockAdjustmentFormScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppFormPage(
      title: 'stock_adjustment'.tr(),
      formKey: controller.formKey,
      saveLabel: 'save'.tr(),
      onSave: controller.save,
      isSaving: controller.saving,
      sections: [
        AppFormSection(
          title: 'stock_adjustment_info'.tr(),
          children: [
            Obx(
              () => ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.warehouse_outlined),
                title: Text('warehouse'.tr()),
                subtitle: Text(
                  controller.warehouse.value?.name ?? 'select_warehouse'.tr(),
                ),
                onTap: () => controller.pickWarehouse(context),
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
        AppFormSection(
          title: 'items'.tr(),
          children: [
            Obx(
              () => Column(
                children: [
                  ...controller.items.asMap().entries.map(
                        (e) => ListTile(
                          contentPadding: EdgeInsets.zero,
                          title: Text(e.value.product.name),
                          subtitle: Text(
                            '${'new_quantity'.tr()}: ${e.value.newQuantity}',
                          ),
                          trailing: IconButton(
                            icon: const Icon(Icons.delete_outline),
                            onPressed: () => controller.items.removeAt(e.key),
                          ),
                        ),
                      ),
                  OutlinedButton.icon(
                    onPressed: () => controller.addItem(context),
                    icon: const Icon(Icons.add),
                    label: Text('add_product_line'.tr()),
                  ),
                ],
              ),
            ),
          ],
        ),
      ],
    );
  }
}
