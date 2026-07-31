import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;
import 'package:uuid/uuid.dart';

import '../../../core/getx/app_services.dart';
import '../../../shared/models/master_data_models.dart';
import '../../../shared/models/mobile_models.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/lookup_picker_sheet.dart';

class WizardLineItem {
  WizardLineItem({
    this.productSyncId,
    this.pricingTypeSyncId,
    required this.itemName,
    required this.quantity,
    required this.unitPrice,
    this.discount = 0,
  });

  final String? productSyncId;
  final String? pricingTypeSyncId;
  final String itemName;
  double quantity;
  double unitPrice;
  double discount;
}

class InvoiceWizardController extends GetxController {
  final discountController = TextEditingController(text: '0');
  final notesController = TextEditingController();
  final _uuid = const Uuid();

  /// Stable client SyncId for idempotent save / offline queue retries.
  late final String draftSyncId = _uuid.v4();

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
  final productPricingEnabled = false.obs;
  final bootstrapping = true.obs;

  @override
  void onInit() {
    super.onInit();
    creditDueDate.value = DateTime.now().add(const Duration(days: 30));
    _bootstrap();
  }

  Future<void> _bootstrap() async {
    bootstrapping.value = true;
    try {
      await Future.wait([
        _loadBusinessSettings(),
        _preloadDefaults(),
      ]);
    } finally {
      bootstrapping.value = false;
    }
  }

  Future<void> _loadBusinessSettings() async {
    try {
      final settings = await AppServices.data.getBusinessSettings();
      productPricingEnabled.value = settings.productPricingEnabled;
    } catch (_) {
      productPricingEnabled.value = false;
    }
  }

  Future<void> _preloadDefaults() async {
    try {
      final warehouses = await AppServices.data.getWarehouses();
      if (warehouses.isNotEmpty && warehouse.value == null) {
        warehouse.value = warehouses.first;
      }
      final cashBoxes = await AppServices.data.getCashBoxes();
      if (cashBoxes.isNotEmpty && cashBox.value == null) {
        cashBox.value = cashBoxes.first;
      }
    } catch (_) {}
  }

  bool validateCurrentStep() {
    switch (step.value) {
      case 1:
        if (warehouse.value == null) {
          AppExceptionHandler.showError('select_warehouse'.tr());
          return false;
        }
        if (needsCustomer && customer.value == null) {
          AppExceptionHandler.showError('select_customer'.tr());
          return false;
        }
        if (needsSupplier && supplier.value == null) {
          AppExceptionHandler.showError('select_supplier'.tr());
          return false;
        }
        return true;
      case 2:
        if (items.isEmpty) {
          AppExceptionHandler.showError('add_line_item'.tr());
          return false;
        }
        if (items.any((item) => item.quantity <= 0)) {
          AppExceptionHandler.showError('invalid_quantity'.tr());
          return false;
        }
        if (items.any((item) => item.unitPrice < 0)) {
          AppExceptionHandler.showError('invalid_unit_price'.tr());
          return false;
        }
        if (items.any((item) => item.unitPrice == 0)) {
          AppExceptionHandler.showError('zero_unit_price'.tr());
          return false;
        }
        return true;
      case 3:
        if (paymentMethod.value == 0 && cashBox.value == null) {
          AppExceptionHandler.showError('select_cashbox'.tr());
          return false;
        }
        if (paymentMethod.value == 1 && creditDueDate.value == null) {
          AppExceptionHandler.showError('select_credit_due_date'.tr());
          return false;
        }
        if (needsInstallmentPlan && installmentCount.value < 1) {
          AppExceptionHandler.showError('invalid_installment_count'.tr());
          return false;
        }
        return true;
      default:
        return true;
    }
  }

  bool get needsCustomer =>
      invoiceType.value == 1 || invoiceType.value == 2;

  bool get needsSupplier =>
      invoiceType.value == 0 || invoiceType.value == 3;

  bool get needsInstallmentPlan =>
      paymentMethod.value == 2 || invoiceType.value == 2;

  bool get _usesSalePrice =>
      invoiceType.value == 1 || invoiceType.value == 2;

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

  void setPaymentMethod(int method) {
    paymentMethod.value = method;
    if (method == 1 && creditDueDate.value == null) {
      creditDueDate.value = DateTime.now().add(const Duration(days: 30));
    }
  }

  Future<void> pickCreditDueDate() async {
    final ctx = Get.context;
    if (ctx == null) return;
    final picked = await showDatePicker(
      context: ctx,
      initialDate: creditDueDate.value ?? DateTime.now().add(const Duration(days: 30)),
      firstDate: DateTime.now().subtract(const Duration(days: 1)),
      lastDate: DateTime.now().add(const Duration(days: 3650)),
    );
    if (picked != null) creditDueDate.value = picked;
  }

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

    String? pricingTypeSyncId;
    var unitPrice = 0.0;

    if (productPricingEnabled.value && product.prices.isNotEmpty) {
      final selectedPrice = await _pickProductPrice(ctx, product);
      if (selectedPrice == null) return;
      pricingTypeSyncId = selectedPrice.pricingTypeSyncId;
      unitPrice = _usesSalePrice
          ? selectedPrice.salePrice
          : selectedPrice.purchasePrice;
    }

    items.add(
      WizardLineItem(
        productSyncId: product.syncId,
        pricingTypeSyncId: pricingTypeSyncId,
        itemName: product.name,
        quantity: 1,
        unitPrice: unitPrice,
      ),
    );
  }

  Future<ProductPriceLookupItem?> _pickProductPrice(
    BuildContext context,
    ProductLookupItem product,
  ) async {
    if (product.prices.length == 1) return product.prices.first;

    return showModalBottomSheet<ProductPriceLookupItem>(
      context: context,
      showDragHandle: true,
      builder: (ctx) => SafeArea(
        child: ListView(
          shrinkWrap: true,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
              child: Text(
                'select_pricing_type'.tr(),
                style: Theme.of(ctx).textTheme.titleLarge,
              ),
            ),
            ...product.prices.map(
              (price) => ListTile(
                title: Text(price.pricingTypeName),
                subtitle: Text(
                  '${'sale_price'.tr()}: ${formatCurrency(price.salePrice)} • ${'purchase_price'.tr()}: ${formatCurrency(price.purchasePrice)}',
                ),
                trailing: price.isDefaultPricingType
                    ? AppStatusChip(
                        label: 'pricing_type_default'.tr(),
                        tone: AppStatusTone.info,
                      )
                    : null,
                onTap: () => Navigator.pop(ctx, price),
              ),
            ),
          ],
        ),
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
    final parsed = int.tryParse(value);
    if (parsed != null && parsed >= 1) {
      installmentCount.value = parsed;
    } else if (value.trim().isEmpty) {
      installmentCount.value = 0;
    }
  }

  void refreshTotals() => step.refresh();

  Future<void> save() async {
    final ctx = Get.context;
    if (ctx == null) return;

    for (var s = 1; s <= 3; s++) {
      final previous = step.value;
      step.value = s;
      final ok = validateCurrentStep();
      step.value = previous;
      if (!ok) {
        step.value = s;
        return;
      }
    }

    if (discount < 0) {
      AppExceptionHandler.showError('invalid_discount'.tr());
      return;
    }

    saving.value = true;
    try {
      final response = await AppServices.operations.createInvoice(
        CreateInvoiceRequest(
          syncId: draftSyncId,
          invoiceType: invoiceType.value,
          customerSyncId: customer.value?.syncId,
          supplierSyncId: supplier.value?.syncId,
          warehouseSyncId: warehouse.value!.syncId,
          paymentMethod: paymentMethod.value,
          cashBoxSyncId: cashBox.value?.syncId,
          date: date.value,
          creditDueDate:
              paymentMethod.value == 1 ? creditDueDate.value : null,
          discountAmount: discount,
          notes: notesController.text.trim().isEmpty
              ? null
              : notesController.text.trim(),
          items: items
              .map(
                (item) => CreateInvoiceItemRequest(
                  productSyncId: item.productSyncId,
                  pricingTypeSyncId: item.pricingTypeSyncId,
                  itemName: item.itemName,
                  quantity: item.quantity,
                  unitPrice: item.unitPrice,
                  discountAmount: item.discount,
                ),
              )
              .toList(),
          installmentPlan: needsInstallmentPlan
              ? CreateInstallmentPlanRequest(
                  numberOfInstallments: installmentCount.value.clamp(1, 360),
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
        '${response.message} ${response.invoiceNumber ?? ''}'.trim(),
      );
      Get.back(result: true);
    } catch (e) {
      AppExceptionHandler.showError(e);
    } finally {
      saving.value = false;
    }
  }

  void next() {
    if (!validateCurrentStep()) return;
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
