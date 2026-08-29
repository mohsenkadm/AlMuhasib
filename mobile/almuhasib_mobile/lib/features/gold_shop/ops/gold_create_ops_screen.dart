import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/gold_create_sale_controller.dart';
import '../models/gold_shop_models.dart';

enum GoldOpsMode { purchase, saleReturn, exchange }

class GoldCreateOpsController extends GetxController {
  GoldCreateOpsController(this.mode);

  final GoldOpsMode mode;
  final formKey = GlobalKey<FormState>();
  final customerSearch = TextEditingController();
  final fxRate = TextEditingController();
  final paidAmount = TextEditingController(text: '0');
  final discountAmount = TextEditingController(text: '0');
  final notes = TextEditingController();
  final relatedInvoiceId = TextEditingController();
  final exchangeCashDiff = TextEditingController(text: '0');

  final loading = true.obs;
  final saving = false.obs;
  final error = Rxn<Object>();

  final customers = <GoldCustomerListItem>[].obs;
  final filteredCustomers = <GoldCustomerListItem>[].obs;
  final suppliers = <GoldSupplierItem>[].obs;
  final warehouses = <GoldWarehouseItem>[].obs;
  final prices = <GoldMithqalPriceRow>[].obs;
  final saleInvoices = <GoldInvoiceListItem>[].obs;

  final selectedCustomer = Rxn<GoldCustomerListItem>();
  final selectedSupplier = Rxn<GoldSupplierItem>();
  final selectedWarehouse = Rxn<GoldWarehouseItem>();
  final paymentMethod = 'Cash'.obs;
  final pricingCurrency = 'IQD'.obs;
  final paymentCurrency = 'IQD'.obs;
  final lines = <GoldSaleLineDraft>[GoldSaleLineDraft()].obs;
  final inLines = <GoldSaleLineDraft>[GoldSaleLineDraft()].obs;
  final outLines = <GoldSaleLineDraft>[GoldSaleLineDraft()].obs;
  final mithqalGrams = 5.0.obs;

  static const karatOptions = [24, 22, 21, 18];

  String get title => switch (mode) {
        GoldOpsMode.purchase => 'gold_new_purchase'.tr(),
        GoldOpsMode.saleReturn => 'gold_new_return'.tr(),
        GoldOpsMode.exchange => 'gold_new_exchange'.tr(),
      };

  List<GoldSaleLineDraft> get activeLines =>
      mode == GoldOpsMode.exchange ? outLines : lines;

  double get grandTotal {
    final grams = mithqalGrams.value > 0 ? mithqalGrams.value : 5.0;
    var sum = 0.0;
    for (final line in activeLines) {
      sum += line.weightGrams * (line.mithqalPrice / grams) + line.makingCharge;
    }
    final discount = double.tryParse(discountAmount.text) ?? 0;
    final t = sum - discount;
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
    relatedInvoiceId.dispose();
    exchangeCashDiff.dispose();
    super.onClose();
  }

  Future<void> loadBootstrap() async {
    loading.value = true;
    error.value = null;
    try {
      final futures = <Future>[
        AppServices.goldShop.getCustomers(pageSize: 200),
        AppServices.goldShop.getWarehouses(),
        AppServices.goldShop.getPrices(),
        AppServices.goldShop.getDashboard(),
      ];
      if (mode == GoldOpsMode.purchase) {
        futures.add(AppServices.goldShop.getSuppliers());
      }
      if (mode == GoldOpsMode.saleReturn) {
        futures.add(
          AppServices.goldShop.getInvoices(invoiceType: 0, pageSize: 100),
        );
      }
      final results = await Future.wait(futures);
      customers.assignAll(results[0] as List<GoldCustomerListItem>);
      filteredCustomers.assignAll(customers);
      warehouses.assignAll(results[1] as List<GoldWarehouseItem>);
      prices.assignAll(results[2] as List<GoldMithqalPriceRow>);
      final dash = results[3] as GoldDashboardDto;
      if (dash.mithqalGrams > 0) mithqalGrams.value = dash.mithqalGrams;
      if (dash.latestUsdToIqd != null && dash.latestUsdToIqd! > 0) {
        fxRate.text = dash.latestUsdToIqd!.toStringAsFixed(0);
      }
      if (warehouses.isNotEmpty) {
        selectedWarehouse.value = warehouses.firstWhere(
          (w) => w.isDefault,
          orElse: () => warehouses.first,
        );
      }
      var idx = 4;
      if (mode == GoldOpsMode.purchase) {
        suppliers.assignAll(results[idx++] as List<GoldSupplierItem>);
      }
      if (mode == GoldOpsMode.saleReturn) {
        saleInvoices.assignAll(results[idx] as List<GoldInvoiceListItem>);
      }
      for (final list in [lines, inLines, outLines]) {
        for (final l in list) {
          l.mithqalGrams = mithqalGrams.value;
          _applyPrice(l, l.karatValue);
        }
      }
      lines.refresh();
      inLines.refresh();
      outLines.refresh();
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

  void _applyPrice(GoldSaleLineDraft line, int karat) {
    final match = prices.where((p) => p.karatValue == karat).toList();
    if (match.isNotEmpty) line.mithqalPrice = match.first.pricePerMithqal;
  }

  void addLine(RxList<GoldSaleLineDraft> target) {
    final draft = GoldSaleLineDraft(mithqalGrams: mithqalGrams.value);
    _applyPrice(draft, draft.karatValue);
    target.add(draft);
    target.refresh();
  }

  void removeLine(RxList<GoldSaleLineDraft> target, int index) {
    if (target.length <= 1) return;
    target.removeAt(index);
    target.refresh();
  }

  Future<void> save() async {
    if (!(formKey.currentState?.validate() ?? false)) return;
    saving.value = true;
    try {
      final paid = double.tryParse(paidAmount.text) ?? 0;
      late GoldInvoiceDetail created;
      if (mode == GoldOpsMode.exchange) {
        final inValid = inLines
            .where((l) => l.weightGrams > 0 && l.mithqalPrice > 0)
            .toList();
        final outValid = outLines
            .where((l) => l.weightGrams > 0 && l.mithqalPrice > 0)
            .toList();
        if (inValid.isEmpty || outValid.isEmpty) {
          AppExceptionHandler.showError('gold_need_line'.tr());
          return;
        }
        created = await AppServices.goldShop.createExchange(
          CreateGoldExchangeRequest(
            paymentMethod: paymentMethod.value,
            customerId: selectedCustomer.value?.id,
            warehouseId: selectedWarehouse.value?.id,
            pricingCurrency: pricingCurrency.value,
            paymentCurrency: paymentCurrency.value,
            fxRate: double.tryParse(fxRate.text) ?? 0,
            exchangeCashDifference:
                double.tryParse(exchangeCashDiff.text) ?? 0,
            paidAmount: paid,
            notes: notes.text.trim(),
            inLines: inValid.map(_mapLine).toList(),
            outLines: outValid.map(_mapLine).toList(),
          ),
        );
      } else {
        final valid = lines
            .where((l) => l.weightGrams > 0 && l.mithqalPrice > 0)
            .toList();
        if (valid.isEmpty) {
          AppExceptionHandler.showError('gold_need_line'.tr());
          return;
        }
        if (mode == GoldOpsMode.purchase && selectedSupplier.value == null) {
          AppExceptionHandler.showError('gold_need_supplier'.tr());
          return;
        }
        final req = CreateGoldSaleRequest(
          paymentMethod: paymentMethod.value,
          customerId: selectedCustomer.value?.id,
          supplierId: selectedSupplier.value?.id,
          warehouseId: selectedWarehouse.value?.id,
          pricingCurrency: pricingCurrency.value,
          paymentCurrency: paymentCurrency.value,
          fxRate: double.tryParse(fxRate.text) ?? 0,
          discountAmount: double.tryParse(discountAmount.text) ?? 0,
          paidAmount: paid,
          notes: notes.text.trim(),
          relatedInvoiceId: int.tryParse(relatedInvoiceId.text),
          lines: valid.map(_mapLine).toList(),
        );
        created = mode == GoldOpsMode.purchase
            ? await AppServices.goldShop.createPurchase(req)
            : await AppServices.goldShop.createSaleReturn(req);
      }
      AppExceptionHandler.showSuccess('settings_saved'.tr());
      Get.offNamed(AppRoutes.goldShopSaleDetailPath(created.id));
    } catch (e) {
      AppExceptionHandler.showError(e);
    } finally {
      saving.value = false;
    }
  }

  CreateGoldSaleLineRequest _mapLine(GoldSaleLineDraft l) =>
      CreateGoldSaleLineRequest(
        karatValue: l.karatValue,
        weightGrams: l.weightGrams,
        mithqalPrice: l.mithqalPrice,
        makingCharge: l.makingCharge,
        description: l.description,
      );
}

class GoldCreateOpsScreen extends StatelessWidget {
  const GoldCreateOpsScreen({super.key, required this.mode});

  final GoldOpsMode mode;

  @override
  Widget build(BuildContext context) {
    final tag = 'gold_ops_${mode.name}';
    final c = Get.put(GoldCreateOpsController(mode), tag: tag);

    return Obx(() {
      if (c.loading.value) {
        return Scaffold(
          appBar: AppBar(title: Text(c.title)),
          body: const Center(child: CircularProgressIndicator()),
        );
      }
      if (c.error.value != null) {
        return Scaffold(
          appBar: AppBar(title: Text(c.title)),
          body: Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(c.error.value.toString()),
                FilledButton(
                  onPressed: c.loadBootstrap,
                  child: Text('retry'.tr()),
                ),
              ],
            ),
          ),
        );
      }

      return AppFormPage(
        title: c.title,
        formKey: c.formKey,
        saveLabel: 'save'.tr(),
        onSave: c.save,
        isSaving: c.saving,
        sections: [
          AppFormSection(
            title: 'gold_sale_header'.tr(),
            children: [
              Obx(
                () => DropdownButtonFormField<GoldWarehouseItem>(
                  value: c.selectedWarehouse.value,
                  decoration: InputDecoration(labelText: 'gold_warehouse'.tr()),
                  items: c.warehouses
                      .map(
                        (w) => DropdownMenuItem(value: w, child: Text(w.name)),
                      )
                      .toList(),
                  onChanged: (v) => c.selectedWarehouse.value = v,
                ),
              ),
              if (mode == GoldOpsMode.purchase) ...[
                const SizedBox(height: AppSpacing.md),
                Obx(
                  () => DropdownButtonFormField<GoldSupplierItem>(
                    value: c.selectedSupplier.value,
                    decoration:
                        InputDecoration(labelText: 'gold_supplier'.tr()),
                    items: c.suppliers
                        .map(
                          (s) =>
                              DropdownMenuItem(value: s, child: Text(s.name)),
                        )
                        .toList(),
                    onChanged: (v) => c.selectedSupplier.value = v,
                  ),
                ),
              ],
              if (mode != GoldOpsMode.purchase) ...[
                const SizedBox(height: AppSpacing.md),
                TextField(
                  controller: c.customerSearch,
                  decoration: InputDecoration(
                    labelText: 'gold_customer_search'.tr(),
                  ),
                  onChanged: c.filterCustomers,
                ),
                const SizedBox(height: AppSpacing.sm),
                Obx(
                  () => DropdownButtonFormField<GoldCustomerListItem>(
                    value: c.selectedCustomer.value,
                    decoration:
                        InputDecoration(labelText: 'gold_customer'.tr()),
                    items: [
                      const DropdownMenuItem(value: null, child: Text('—')),
                      ...c.filteredCustomers.map(
                        (x) => DropdownMenuItem(
                          value: x,
                          child: Text(x.name),
                        ),
                      ),
                    ],
                    onChanged: (v) => c.selectedCustomer.value = v,
                  ),
                ),
              ],
              if (mode == GoldOpsMode.saleReturn) ...[
                const SizedBox(height: AppSpacing.md),
                Obx(
                  () => DropdownButtonFormField<String>(
                    value: relatedValue(c),
                    decoration: InputDecoration(
                      labelText: 'gold_related_invoice'.tr(),
                    ),
                    items: [
                      const DropdownMenuItem(value: '', child: Text('—')),
                      ...c.saleInvoices.map(
                        (i) => DropdownMenuItem(
                          value: '${i.id}',
                          child: Text('${i.invoiceNumber} · ${i.customerName}'),
                        ),
                      ),
                    ],
                    onChanged: (v) => c.relatedInvoiceId.text = v ?? '',
                  ),
                ),
              ],
              const SizedBox(height: AppSpacing.md),
              Obx(
                () => DropdownButtonFormField<String>(
                  value: c.paymentMethod.value,
                  decoration:
                      InputDecoration(labelText: 'gold_payment_method'.tr()),
                  items: [
                    DropdownMenuItem(
                      value: 'Cash',
                      child: Text('gold_payment_cash'.tr()),
                    ),
                    DropdownMenuItem(
                      value: 'Credit',
                      child: Text('gold_payment_credit'.tr()),
                    ),
                  ],
                  onChanged: (v) => c.paymentMethod.value = v ?? 'Cash',
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              TextFormField(
                controller: c.paidAmount,
                keyboardType: TextInputType.number,
                decoration: InputDecoration(labelText: 'gold_paid'.tr()),
              ),
              if (mode == GoldOpsMode.exchange) ...[
                const SizedBox(height: AppSpacing.md),
                TextFormField(
                  controller: c.exchangeCashDiff,
                  keyboardType: TextInputType.number,
                  decoration: InputDecoration(
                    labelText: 'gold_exchange_cash_diff'.tr(),
                  ),
                ),
              ],
              const SizedBox(height: AppSpacing.md),
              TextFormField(
                controller: c.notes,
                decoration: InputDecoration(labelText: 'notes'.tr()),
              ),
            ],
          ),
          if (mode == GoldOpsMode.exchange) ...[
            _linesSection(
              context,
              c,
              'gold_exchange_in'.tr(),
              c.inLines,
            ),
            _linesSection(
              context,
              c,
              'gold_exchange_out'.tr(),
              c.outLines,
            ),
          ] else
            _linesSection(context, c, 'gold_invoice_lines'.tr(), c.lines),
        ],
      );
    });
  }

  String? relatedValue(GoldCreateOpsController c) {
    final t = c.relatedInvoiceId.text;
    if (t.isEmpty) return '';
    return t;
  }

  AppFormSection _linesSection(
    BuildContext context,
    GoldCreateOpsController c,
    String title,
    RxList<GoldSaleLineDraft> target,
  ) {
    return AppFormSection(
      title: title,
      children: [
        Obx(
          () => Column(
            children: [
              for (var i = 0; i < target.length; i++)
                Card(
                  margin: const EdgeInsets.only(bottom: 10),
                  child: Padding(
                    padding: const EdgeInsets.all(12),
                    child: Column(
                      children: [
                        Row(
                          children: [
                            Expanded(
                              child: DropdownButtonFormField<int>(
                                value: target[i].karatValue,
                                decoration: InputDecoration(
                                  labelText: 'gold_karat'.tr(),
                                ),
                                items: GoldCreateOpsController.karatOptions
                                    .map(
                                      (k) => DropdownMenuItem(
                                        value: k,
                                        child: Text('$k'),
                                      ),
                                    )
                                    .toList(),
                                onChanged: (v) {
                                  if (v == null) return;
                                  target[i].karatValue = v;
                                  c._applyPrice(target[i], v);
                                  target.refresh();
                                },
                              ),
                            ),
                            IconButton(
                              onPressed: () => c.removeLine(target, i),
                              icon: const Icon(Icons.delete_outline),
                            ),
                          ],
                        ),
                        TextFormField(
                          initialValue: target[i].weightGrams == 0
                              ? ''
                              : target[i].weightGrams.toString(),
                          keyboardType: TextInputType.number,
                          decoration:
                              InputDecoration(labelText: 'gold_weight'.tr()),
                          onChanged: (v) {
                            target[i].weightGrams = double.tryParse(v) ?? 0;
                            target.refresh();
                          },
                        ),
                        TextFormField(
                          initialValue: target[i].mithqalPrice == 0
                              ? ''
                              : target[i].mithqalPrice.toString(),
                          keyboardType: TextInputType.number,
                          decoration: InputDecoration(
                            labelText: 'gold_mithqal_price'.tr(),
                          ),
                          onChanged: (v) {
                            target[i].mithqalPrice = double.tryParse(v) ?? 0;
                            target.refresh();
                          },
                        ),
                        TextFormField(
                          initialValue: target[i].makingCharge == 0
                              ? ''
                              : target[i].makingCharge.toString(),
                          keyboardType: TextInputType.number,
                          decoration: InputDecoration(
                            labelText: 'gold_making_charge'.tr(),
                          ),
                          onChanged: (v) {
                            target[i].makingCharge = double.tryParse(v) ?? 0;
                            target.refresh();
                          },
                        ),
                      ],
                    ),
                  ),
                ),
              OutlinedButton.icon(
                onPressed: () => c.addLine(target),
                icon: const Icon(Icons.add),
                label: Text('gold_add_line'.tr()),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
