import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../shared/models/master_data_models.dart';
import '../../../shared/models/mobile_models.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/lookup_picker_sheet.dart';

class ProductPriceFormController extends GetxController {
  ProductPriceFormController({this.syncId, this.prefillProductSyncId});

  final String? syncId;
  final String? prefillProductSyncId;

  final formKey = GlobalKey<FormState>();
  final salePriceController = TextEditingController(text: '0');
  final purchasePriceController = TextEditingController(text: '0');
  final product = Rxn<ProductLookupItem>();
  final pricingType = Rxn<PricingTypeLookupItem>();
  final saving = false.obs;
  final deleting = false.obs;
  final loading = false.obs;

  bool get isEdit => syncId != null && syncId!.isNotEmpty;

  @override
  void onInit() {
    super.onInit();
    if (isEdit) {
      _loadExisting();
    } else if (prefillProductSyncId != null) {
      _prefillProduct(prefillProductSyncId!);
    } else {
      final args = Get.arguments;
      if (args is ProductLookupItem) {
        product.value = args;
      } else if (args is ProductPriceLookupItem) {
        _applyPrice(args);
      } else if (args is String && args.isNotEmpty) {
        _prefillProduct(args);
      }
    }
  }

  Future<void> _prefillProduct(String productSyncId) async {
    try {
      final products = await AppServices.data.getProducts();
      for (final p in products) {
        if (p.syncId == productSyncId) {
          product.value = p;
          break;
        }
      }
    } catch (_) {}
  }

  Future<void> _loadExisting() async {
    loading.value = true;
    try {
      final args = Get.arguments;
      if (args is ProductPriceLookupItem) {
        await _applyPrice(args);
        return;
      }
      final prices = await AppServices.data.getProductPrices();
      for (final price in prices) {
        if (price.syncId == syncId) {
          await _applyPrice(price);
          break;
        }
      }
    } catch (e) {
      AppExceptionHandler.showError(e);
    } finally {
      loading.value = false;
    }
  }

  Future<void> _applyPrice(ProductPriceLookupItem item) async {
    salePriceController.text = item.salePrice.toString();
    purchasePriceController.text = item.purchasePrice.toString();
    product.value = ProductLookupItem(
      id: 0,
      syncId: item.productSyncId,
      name: item.productName,
      categorySyncId: '',
      categoryName: '',
    );
    pricingType.value = PricingTypeLookupItem(
      id: 0,
      syncId: item.pricingTypeSyncId,
      name: item.pricingTypeName,
      isDefault: item.isDefaultPricingType,
    );
  }

  Future<void> pickProduct() async {
    if (isEdit) return;
    final ctx = Get.context;
    if (ctx == null) return;
    final selected = await showLookupPickerSheet<ProductLookupItem>(
      context: ctx,
      title: 'select_product'.tr(),
      loadItems: (search) => AppServices.data.getProducts(search: search),
    );
    if (selected != null) product.value = selected;
  }

  Future<void> pickPricingType() async {
    final ctx = Get.context;
    if (ctx == null) return;
    final selected = await showLookupPickerSheet<PricingTypeLookupItem>(
      context: ctx,
      title: 'select_pricing_type'.tr(),
      loadItems: (search) => AppServices.data.getPricingTypes(search: search),
      itemBuilder: (item) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(item.name),
          if (item.isDefault || !item.isActive)
            Text(
              [
                if (item.isDefault) 'pricing_type_default'.tr(),
                if (!item.isActive) 'pricing_type_inactive'.tr(),
              ].join(' • '),
              style: Theme.of(ctx).textTheme.bodySmall,
            ),
        ],
      ),
    );
    if (selected != null) pricingType.value = selected;
  }

  Future<void> save() async {
    if (!(formKey.currentState?.validate() ?? false)) return;
    if (product.value == null) {
      AppExceptionHandler.showError('select_product'.tr());
      return;
    }
    if (pricingType.value == null) {
      AppExceptionHandler.showError('select_pricing_type'.tr());
      return;
    }
    saving.value = true;
    try {
      final response = await AppServices.operations.upsertProductPrice(
        UpsertProductPriceRequest(
          syncId: syncId,
          productSyncId: product.value!.syncId,
          pricingTypeSyncId: pricingType.value!.syncId,
          salePrice: double.tryParse(salePriceController.text) ?? 0,
          purchasePrice: double.tryParse(purchasePriceController.text) ?? 0,
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

  Future<void> delete() async {
    if (!isEdit) return;
    final confirmed = await Get.dialog<bool>(
      AlertDialog(
        title: Text('delete'.tr()),
        content: Text('confirm_delete_product_price'.tr()),
        actions: [
          TextButton(onPressed: () => Get.back(result: false), child: Text('cancel'.tr())),
          FilledButton(onPressed: () => Get.back(result: true), child: Text('delete'.tr())),
        ],
      ),
    );
    if (confirmed != true) return;
    deleting.value = true;
    try {
      final response = await AppServices.operations.deleteProductPrice(syncId!);
      if (response.conflicts.isNotEmpty) {
        AppExceptionHandler.showConflicts(response.conflicts);
        return;
      }
      AppExceptionHandler.showSuccess(response.message);
      Get.back(result: true);
    } catch (e) {
      AppExceptionHandler.showError(e);
    } finally {
      deleting.value = false;
    }
  }

  @override
  void onClose() {
    salePriceController.dispose();
    purchasePriceController.dispose();
    super.onClose();
  }
}
