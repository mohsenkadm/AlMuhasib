import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../shared/models/master_data_models.dart';
import '../../../shared/models/mobile_models.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/lookup_picker_sheet.dart';

class WizardLineItem {
  WizardLineItem({
    this.productSyncId,
    required this.itemName,
    required this.quantity,
    required this.unitPrice,
    this.discount = 0,
  });

  final String? productSyncId;
  final String itemName;
  double quantity;
  double unitPrice;
  double discount;
}

class InvoiceWizardController extends GetxController {
  final discountController = TextEditingController(text: '0');
  final notesController = TextEditingController();

  final step = 0.obs;
  final invoiceType = 1.obs;
  final paymentMethod = 0.obs;
  final date = DateTime.now().obs;
  final creditDueDate = Rxn<DateTime>();
  final customer = Rxn<LookupItem>();
  final supplier = Rxn<LookupItem>();
  final warehouse = Rxn<LookupItem>();
  final cashBox = Rxn<LookupItem>();
  final items = <WizardLineItem>[].obs;
  final installmentCount = 6.obs;
  final installmentStart = DateTime.now().obs;
  final saving = false.obs;

  bool get needsCustomer =>
      invoiceType.value == 1 || invoiceType.value == 2;

  bool get needsSupplier =>
      invoiceType.value == 0 || invoiceType.value == 3;

  bool get needsInstallmentPlan =>
      paymentMethod.value == 2 || invoiceType.value == 2;

  double get subtotal => items.fold(
        0.0,
        (sum, item) => sum + item.quantity * item.unitPrice - item.discount,
      );

  double get discount => double.tryParse(discountController.text) ?? 0;

  double get net => subtotal - discount;

  void setInvoiceType(int type) {
    invoiceType.value = type;
    if (type == 2) paymentMethod.value = 2;
  }

  void setPaymentMethod(int method) => paymentMethod.value = method;

  Future<void> pickLookup({
    required String title,
    required Future<List<LookupItem>> Function(String) loader,
    required void Function(LookupItem) onSelected,
  }) async {
    final ctx = Get.context;
    if (ctx == null) return;
    final selected = await showLookupPickerSheet<LookupItem>(
      context: ctx,
      title: title,
      loadItems: loader,
    );
    if (selected != null) onSelected(selected);
  }

  Future<void> pickProduct() async {
    final ctx = Get.context;
    if (ctx == null) return;
    final product = await showLookupPickerSheet<ProductLookupItem>(
      context: ctx,
      title: 'select_product'.tr(),
      loadItems: (search) => AppServices.data.getProducts(search: search),
    );
    if (product == null) return;
    items.add(
      WizardLineItem(
        productSyncId: product.syncId,
        itemName: product.name,
        quantity: 1,
        unitPrice: 0,
      ),
    );
  }

  void removeItemAt(int index) => items.removeAt(index);

  void updateItemQuantity(int index, String value) {
    final parsed = double.tryParse(value);
    if (parsed != null) {
      items[index].quantity = parsed;
      items.refresh();
    }
  }

  void updateItemUnitPrice(int index, String value) {
    final parsed = double.tryParse(value);
    if (parsed != null) {
      items[index].unitPrice = parsed;
      items.refresh();
    }
  }

  void setInstallmentCount(String value) {
    installmentCount.value = int.tryParse(value) ?? installmentCount.value;
  }

  void refreshTotals() => step.refresh();

  Future<void> save() async {
    final ctx = Get.context;
    if (ctx == null) return;

    if (warehouse.value == null) {
      AppExceptionHandler.showError('select_warehouse'.tr());
      return;
    }
    if (needsCustomer && customer.value == null) {
      AppExceptionHandler.showError('select_customer'.tr());
      return;
    }
    if (needsSupplier && supplier.value == null) {
      AppExceptionHandler.showError('select_supplier'.tr());
      return;
    }
    if (paymentMethod.value == 0 && cashBox.value == null) {
      AppExceptionHandler.showError('select_cashbox'.tr());
      return;
    }
    if (items.isEmpty) {
      AppExceptionHandler.showError('add_line_item'.tr());
      return;
    }

    saving.value = true;
    try {
      final response = await AppServices.operations.createInvoice(
        CreateInvoiceRequest(
          invoiceType: invoiceType.value,
          customerSyncId: customer.value?.syncId,
          supplierSyncId: supplier.value?.syncId,
          warehouseSyncId: warehouse.value!.syncId,
          paymentMethod: paymentMethod.value,
          cashBoxSyncId: cashBox.value?.syncId,
          date: date.value,
          creditDueDate: creditDueDate.value,
          discountAmount: discount,
          notes: notesController.text.trim().isEmpty
              ? null
              : notesController.text.trim(),
          items: items
              .map(
                (item) => CreateInvoiceItemRequest(
                  productSyncId: item.productSyncId,
                  itemName: item.itemName,
                  quantity: item.quantity,
                  unitPrice: item.unitPrice,
                  discountAmount: item.discount,
                ),
              )
              .toList(),
          installmentPlan: needsInstallmentPlan
              ? CreateInstallmentPlanRequest(
                  numberOfInstallments: installmentCount.value,
                  startDate: installmentStart.value,
                )
              : null,
        ),
      );
      if (response.conflicts.isNotEmpty) {
        AppExceptionHandler.showConflicts(response.conflicts);
        return;
      }
      AppExceptionHandler.showSuccess(
        '${response.message} ${response.invoiceNumber ?? ''}',
      );
      Get.back(result: true);
    } catch (e) {
      AppExceptionHandler.showError(e);
    } finally {
      saving.value = false;
    }
  }

  void next() {
    if (step.value < 4) {
      step.value++;
    } else {
      save();
    }
  }

  void back() {
    if (step.value > 0) step.value--;
  }

  @override
  void onClose() {
    discountController.dispose();
    notesController.dispose();
    super.onClose();
  }
}
