import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../models/gold_shop_models.dart';

class GoldSaleLineDraft {
  GoldSaleLineDraft({
    this.karatValue = 21,
    this.weightGrams = 0,
    this.mithqalPrice = 0,
    this.makingCharge = 0,
    this.description = '',
    this.mithqalGrams = 5,
  });

  int karatValue;
  double weightGrams;
  double mithqalPrice;
  double makingCharge;
  String description;
  double mithqalGrams;

  double get lineTotal {
    final grams = mithqalGrams > 0 ? mithqalGrams : 5.0;
    final pricePerGram = mithqalPrice / grams;
    return (weightGrams * pricePerGram) + makingCharge;
  }
}

class GoldCreateSaleController extends GetxController {
  final formKey = GlobalKey<FormState>();
  final customerSearch = TextEditingController();
  final fxRate = TextEditingController();
  final paidAmount = TextEditingController(text: '0');
  final discountAmount = TextEditingController(text: '0');
  final notes = TextEditingController();

  final loading = true.obs;
  final saving = false.obs;
  final error = Rxn<Object>();

  final customers = <GoldCustomerListItem>[].obs;
  final filteredCustomers = <GoldCustomerListItem>[].obs;
  final warehouses = <GoldWarehouseItem>[].obs;
  final prices = <GoldMithqalPriceRow>[].obs;

  final selectedCustomer = Rxn<GoldCustomerListItem>();
  final selectedWarehouse = Rxn<GoldWarehouseItem>();
  final paymentMethod = 'Cash'.obs;
  final pricingCurrency = 'USD'.obs;
  final paymentCurrency = 'IQD'.obs;
  final lines = <GoldSaleLineDraft>[GoldSaleLineDraft()].obs;
  final mithqalGrams = 5.0.obs;

  static const karatOptions = [24, 22, 21, 18];

  double get totalsGold {
    final grams = mithqalGrams.value > 0 ? mithqalGrams.value : 5.0;
    var sum = 0.0;
    for (final line in lines) {
      sum += line.weightGrams * (line.mithqalPrice / grams);
    }
    return sum;
  }

  double get totalsMaking =>
      lines.fold<double>(0, (s, l) => s + l.makingCharge);

  double get discount => double.tryParse(discountAmount.text) ?? 0;

  double get grandTotal {
    final t = totalsGold + totalsMaking - discount;
    return t < 0 ? 0 : t;
  }

  @override
  void onInit() {
    super.onInit();
    loadBootstrap();
  }

  @override
  void onClose() {
    customerSearch.dispose();
    fxRate.dispose();
    paidAmount.dispose();
    discountAmount.dispose();
    notes.dispose();
    super.onClose();
  }

  Future<void> loadBootstrap() async {
    loading.value = true;
    error.value = null;
    try {
      final results = await Future.wait([
        AppServices.goldShop.getCustomers(pageSize: 200),
        AppServices.goldShop.getWarehouses(),
        AppServices.goldShop.getPrices(),
        AppServices.goldShop.getDashboard(),
      ]);

      customers.assignAll(results[0] as List<GoldCustomerListItem>);
      filteredCustomers.assignAll(customers);
      warehouses.assignAll(results[1] as List<GoldWarehouseItem>);
      prices.assignAll(results[2] as List<GoldMithqalPriceRow>);
      final dash = results[3] as GoldDashboardDto;

      if (dash.mithqalGrams > 0) {
        mithqalGrams.value = dash.mithqalGrams;
        for (final line in lines) {
          line.mithqalGrams = dash.mithqalGrams;
        }
      }

      if (dash.latestUsdToIqd != null && dash.latestUsdToIqd! > 0) {
        fxRate.text = dash.latestUsdToIqd!.toStringAsFixed(0);
      }

      if (warehouses.isNotEmpty) {
        selectedWarehouse.value = warehouses.firstWhere(
          (w) => w.isDefault,
          orElse: () => warehouses.first,
        );
      }

      if (lines.isNotEmpty && prices.isNotEmpty) {
        _applyPriceForLine(0, lines.first.karatValue);
      }
      lines.refresh();
    } catch (e) {
      error.value = e;
    } finally {
      loading.value = false;
    }
  }

  void filterCustomers(String query) {
    final q = query.trim().toLowerCase();
    if (q.isEmpty) {
      filteredCustomers.assignAll(customers);
      return;
    }
    filteredCustomers.assignAll(
      customers.where(
        (c) =>
            c.name.toLowerCase().contains(q) ||
            c.phone.toLowerCase().contains(q),
      ),
    );
  }

  void selectCustomer(GoldCustomerListItem? customer) {
    selectedCustomer.value = customer;
  }

  void selectWarehouse(GoldWarehouseItem? warehouse) {
    selectedWarehouse.value = warehouse;
  }

  void addLine() {
    final draft = GoldSaleLineDraft(mithqalGrams: mithqalGrams.value);
    lines.add(draft);
    _applyPriceForLine(lines.length - 1, draft.karatValue);
    lines.refresh();
  }

  void removeLine(int index) {
    if (lines.length <= 1) return;
    lines.removeAt(index);
    lines.refresh();
  }

  void updateLineKarat(int index, int karat) {
    lines[index].karatValue = karat;
    _applyPriceForLine(index, karat);
    lines.refresh();
  }

  void updateLineWeight(int index, String value) {
    lines[index].weightGrams = double.tryParse(value) ?? 0;
    lines.refresh();
  }

  void updateLineMithqal(int index, String value) {
    lines[index].mithqalPrice = double.tryParse(value) ?? 0;
    lines.refresh();
  }

  void updateLineMaking(int index, String value) {
    lines[index].makingCharge = double.tryParse(value) ?? 0;
    lines.refresh();
  }

  void updateLineDescription(int index, String value) {
    lines[index].description = value;
  }

  void _applyPriceForLine(int index, int karat) {
    final match = prices.where((p) => p.karatValue == karat).toList();
    if (match.isEmpty) return;
    lines[index].mithqalPrice = match.first.pricePerMithqal;
  }

  Future<void> save() async {
    if (!(formKey.currentState?.validate() ?? false)) return;

    if (paymentMethod.value == 'Credit' && selectedCustomer.value == null) {
      AppExceptionHandler.showError('gold_credit_needs_customer'.tr());
      return;
    }

    final validLines = lines
        .where((l) => l.weightGrams > 0 && l.mithqalPrice > 0)
        .toList();
    if (validLines.isEmpty) {
      AppExceptionHandler.showError('gold_need_line'.tr());
      return;
    }

    saving.value = true;
    try {
      final paid = double.tryParse(paidAmount.text) ?? 0;
      final created = await AppServices.goldShop.createSale(
        CreateGoldSaleRequest(
          paymentMethod: paymentMethod.value,
          customerId: selectedCustomer.value?.id,
          warehouseId: selectedWarehouse.value?.id,
          pricingCurrency: pricingCurrency.value,
          paymentCurrency: paymentCurrency.value,
          fxRate: double.tryParse(fxRate.text) ?? 0,
          discountAmount: discount,
          paidAmount: paid,
          notes: notes.text.trim(),
          lines: validLines
              .map(
                (l) => CreateGoldSaleLineRequest(
                  karatValue: l.karatValue,
                  weightGrams: l.weightGrams,
                  mithqalPrice: l.mithqalPrice,
                  makingCharge: l.makingCharge,
                  description: l.description,
                ),
              )
              .toList(),
        ),
      );
      AppExceptionHandler.showSuccess('settings_saved'.tr());
      Get.offNamed(AppRoutes.goldShopSaleDetailPath(created.id));
    } catch (e) {
      AppExceptionHandler.showError(e);
    } finally {
      saving.value = false;
    }
  }
}
