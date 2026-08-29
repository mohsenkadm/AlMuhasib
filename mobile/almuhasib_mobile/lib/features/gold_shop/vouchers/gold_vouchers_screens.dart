import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/system_themes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../models/gold_shop_models.dart';

class GoldVouchersController extends GetxController {
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final items = <GoldVoucherItem>[].obs;
  final typeFilter = RxnString();

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      items.value = await AppServices.goldShop.getVouchers(
        type: typeFilter.value,
      );
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}

class GoldVouchersScreen extends StatelessWidget {
  const GoldVouchersScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final c = Get.put(GoldVouchersController(), tag: 'gold_vouchers');
    return Scaffold(
      appBar: AppBar(
        title: Text('gold_vouchers_title'.tr()),
        actions: [
          IconButton(onPressed: c.load, icon: const Icon(Icons.refresh)),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          await Get.toNamed(AppRoutes.goldShopVoucherNew);
          c.load();
        },
        icon: const Icon(Icons.add),
        label: Text('gold_new_voucher'.tr()),
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
            child: Obx(
              () => Wrap(
                spacing: 8,
                children: [
                  FilterChip(
                    label: Text('all'.tr()),
                    selected: c.typeFilter.value == null,
                    onSelected: (_) {
                      c.typeFilter.value = null;
                      c.load();
                    },
                  ),
                  FilterChip(
                    label: Text('gold_voucher_receipt'.tr()),
                    selected: c.typeFilter.value == 'Receipt',
                    onSelected: (_) {
                      c.typeFilter.value = 'Receipt';
                      c.load();
                    },
                  ),
                  FilterChip(
                    label: Text('gold_voucher_payment'.tr()),
                    selected: c.typeFilter.value == 'Payment',
                    onSelected: (_) {
                      c.typeFilter.value = 'Payment';
                      c.load();
                    },
                  ),
                ],
              ),
            ),
          ),
          Expanded(
            child: Obx(() {
              if (c.isLoading.value && c.items.isEmpty) {
                return const Center(child: CircularProgressIndicator());
              }
              if (c.error.value != null && c.items.isEmpty) {
                return Center(child: Text(c.error.value.toString()));
              }
              if (c.items.isEmpty) {
                return Center(child: Text('gold_no_vouchers'.tr()));
              }
              return RefreshIndicator(
                onRefresh: c.load,
                child: ListView.separated(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 100),
                  itemCount: c.items.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 8),
                  itemBuilder: (_, i) {
                    final v = c.items[i];
                    final isReceipt =
                        v.voucherType.toLowerCase().contains('receipt');
                    return Card(
                      child: ListTile(
                        leading: CircleAvatar(
                          backgroundColor: SystemThemes.goldPrimary
                              .withValues(alpha: 0.14),
                          child: Icon(
                            isReceipt
                                ? Icons.south_west
                                : Icons.north_east,
                            color: SystemThemes.goldPrimary,
                          ),
                        ),
                        title: Text(v.voucherNumber),
                        subtitle: Text(
                          '${isReceipt ? 'gold_voucher_receipt'.tr() : 'gold_voucher_payment'.tr()} · ${formatDate(v.voucherDate)}'
                          '${v.notes.isEmpty ? '' : '\n${v.notes}'}',
                        ),
                        trailing: Text(
                          '${formatCurrency(v.amount)} ${v.currency}',
                          style: const TextStyle(fontWeight: FontWeight.w800),
                        ),
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

class GoldCreateVoucherController extends GetxController {
  final formKey = GlobalKey<FormState>();
  final amount = TextEditingController();
  final notes = TextEditingController();
  final saving = false.obs;
  final loading = true.obs;
  final voucherType = 'Receipt'.obs;
  final currency = 'IQD'.obs;
  final cashBoxes = <GoldCashBoxItem>[].obs;
  final customers = <GoldCustomerListItem>[].obs;
  final suppliers = <GoldSupplierItem>[].obs;
  final selectedCashBox = Rxn<GoldCashBoxItem>();
  final selectedCustomer = Rxn<GoldCustomerListItem>();
  final selectedSupplier = Rxn<GoldSupplierItem>();

  @override
  void onInit() {
    super.onInit();
    _bootstrap();
  }

  @override
  void onClose() {
    amount.dispose();
    notes.dispose();
    super.onClose();
  }

  Future<void> _bootstrap() async {
    loading.value = true;
    try {
      final results = await Future.wait([
        AppServices.goldShop.getCashBoxes(),
        AppServices.goldShop.getCustomers(pageSize: 200),
        AppServices.goldShop.getSuppliers(),
      ]);
      cashBoxes.assignAll(results[0] as List<GoldCashBoxItem>);
      customers.assignAll(results[1] as List<GoldCustomerListItem>);
      suppliers.assignAll(results[2] as List<GoldSupplierItem>);
      if (cashBoxes.isNotEmpty) {
        selectedCashBox.value = cashBoxes.firstWhere(
          (b) => b.isDefault,
          orElse: () => cashBoxes.first,
        );
      }
    } finally {
      loading.value = false;
    }
  }

  Future<void> save() async {
    if (!(formKey.currentState?.validate() ?? false)) return;
    final amt = double.tryParse(amount.text) ?? 0;
    if (amt <= 0) {
      AppExceptionHandler.showError('gold_need_amount'.tr());
      return;
    }
    if (selectedCashBox.value == null) {
      AppExceptionHandler.showError('gold_need_cashbox'.tr());
      return;
    }
    saving.value = true;
    try {
      await AppServices.goldShop.createVoucher(
        CreateGoldVoucherRequest(
          amount: amt,
          voucherType: voucherType.value,
          currency: currency.value,
          cashBoxId: selectedCashBox.value!.id,
          customerId: selectedCustomer.value?.id,
          supplierId: selectedSupplier.value?.id,
          notes: notes.text.trim(),
        ),
      );
      AppExceptionHandler.showSuccess('settings_saved'.tr());
      Get.back();
    } catch (e) {
      AppExceptionHandler.showError(e);
    } finally {
      saving.value = false;
    }
  }
}

class GoldCreateVoucherScreen extends StatelessWidget {
  const GoldCreateVoucherScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final c = Get.put(GoldCreateVoucherController(), tag: 'gold_create_voucher');
    return Obx(() {
      if (c.loading.value) {
        return Scaffold(
          appBar: AppBar(title: Text('gold_new_voucher'.tr())),
          body: const Center(child: CircularProgressIndicator()),
        );
      }
      return AppFormPage(
        title: 'gold_new_voucher'.tr(),
        formKey: c.formKey,
        saveLabel: 'save'.tr(),
        onSave: c.save,
        isSaving: c.saving,
        sections: [
          AppFormSection(
            title: 'gold_voucher_details'.tr(),
            children: [
              Obx(
                () => DropdownButtonFormField<String>(
                  value: c.voucherType.value,
                  decoration: InputDecoration(
                    labelText: 'gold_voucher_type'.tr(),
                  ),
                  items: [
                    DropdownMenuItem(
                      value: 'Receipt',
                      child: Text('gold_voucher_receipt'.tr()),
                    ),
                    DropdownMenuItem(
                      value: 'Payment',
                      child: Text('gold_voucher_payment'.tr()),
                    ),
                  ],
                  onChanged: (v) => c.voucherType.value = v ?? 'Receipt',
                ),
              ),
              const SizedBox(height: 12),
              Obx(
                () => DropdownButtonFormField<GoldCashBoxItem>(
                  value: c.selectedCashBox.value,
                  decoration: InputDecoration(
                    labelText: 'gold_cash_box'.tr(),
                  ),
                  items: c.cashBoxes
                      .map(
                        (b) => DropdownMenuItem(
                          value: b,
                          child: Text(
                            '${b.name} (${b.currency} · ${formatCurrency(b.balance)})',
                          ),
                        ),
                      )
                      .toList(),
                  onChanged: (v) => c.selectedCashBox.value = v,
                ),
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: c.amount,
                keyboardType: TextInputType.number,
                decoration: InputDecoration(labelText: 'amount'.tr()),
                validator: (v) =>
                    (double.tryParse(v ?? '') ?? 0) <= 0 ? 'required'.tr() : null,
              ),
              const SizedBox(height: 12),
              Obx(
                () => DropdownButtonFormField<String>(
                  value: c.currency.value,
                  decoration: InputDecoration(
                    labelText: 'gold_payment_currency'.tr(),
                  ),
                  items: const [
                    DropdownMenuItem(value: 'IQD', child: Text('IQD')),
                    DropdownMenuItem(value: 'USD', child: Text('USD')),
                  ],
                  onChanged: (v) => c.currency.value = v ?? 'IQD',
                ),
              ),
              const SizedBox(height: 12),
              Obx(
                () => DropdownButtonFormField<GoldCustomerListItem>(
                  value: c.selectedCustomer.value,
                  decoration: InputDecoration(
                    labelText: 'gold_customer'.tr(),
                  ),
                  items: [
                    DropdownMenuItem(
                      value: null,
                      child: Text('—'),
                    ),
                    ...c.customers.map(
                      (x) => DropdownMenuItem(value: x, child: Text(x.name)),
                    ),
                  ],
                  onChanged: (v) {
                    c.selectedCustomer.value = v;
                    if (v != null) c.selectedSupplier.value = null;
                  },
                ),
              ),
              const SizedBox(height: 12),
              Obx(
                () => DropdownButtonFormField<GoldSupplierItem>(
                  value: c.selectedSupplier.value,
                  decoration: InputDecoration(
                    labelText: 'gold_supplier'.tr(),
                  ),
                  items: [
                    DropdownMenuItem(
                      value: null,
                      child: Text('—'),
                    ),
                    ...c.suppliers.map(
                      (x) => DropdownMenuItem(value: x, child: Text(x.name)),
                    ),
                  ],
                  onChanged: (v) {
                    c.selectedSupplier.value = v;
                    if (v != null) c.selectedCustomer.value = null;
                  },
                ),
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: c.notes,
                decoration: InputDecoration(labelText: 'notes'.tr()),
                maxLines: 2,
              ),
            ],
          ),
        ],
      );
    });
  }
}

class GoldCollectionController extends GetxController {
  final formKey = GlobalKey<FormState>();
  final amount = TextEditingController();
  final notes = TextEditingController();
  final loading = true.obs;
  final saving = false.obs;
  final openInvoices = <GoldInvoiceListItem>[].obs;
  final cashBoxes = <GoldCashBoxItem>[].obs;
  final selectedInvoice = Rxn<GoldInvoiceListItem>();
  final selectedCashBox = Rxn<GoldCashBoxItem>();
  final currency = 'IQD'.obs;

  @override
  void onInit() {
    super.onInit();
    _bootstrap();
  }

  @override
  void onClose() {
    amount.dispose();
    notes.dispose();
    super.onClose();
  }

  Future<void> _bootstrap() async {
    loading.value = true;
    try {
      final results = await Future.wait([
        AppServices.goldShop.getInvoices(invoiceType: 0, status: 1, pageSize: 100),
        AppServices.goldShop.getInvoices(invoiceType: 0, status: 2, pageSize: 100),
        AppServices.goldShop.getCashBoxes(),
      ]);
      final open = [
        ...(results[0] as List<GoldInvoiceListItem>),
        ...(results[1] as List<GoldInvoiceListItem>),
      ].where((i) => i.remainingAmount > 0).toList();
      openInvoices.assignAll(open);
      cashBoxes.assignAll(results[2] as List<GoldCashBoxItem>);
      if (cashBoxes.isNotEmpty) {
        selectedCashBox.value = cashBoxes.firstWhere(
          (b) => b.isDefault,
          orElse: () => cashBoxes.first,
        );
      }
      final argId = Get.arguments is int ? Get.arguments as int : null;
      if (argId != null) {
        selectedInvoice.value = openInvoices.firstWhereOrNull((i) => i.id == argId);
      }
    } finally {
      loading.value = false;
    }
  }

  void selectInvoice(GoldInvoiceListItem? inv) {
    selectedInvoice.value = inv;
    if (inv != null) {
      amount.text = inv.remainingAmount.toStringAsFixed(0);
      currency.value = inv.paymentCurrency == 1 ? 'USD' : 'IQD';
    }
  }

  Future<void> save() async {
    if (!(formKey.currentState?.validate() ?? false)) return;
    final inv = selectedInvoice.value;
    final amt = double.tryParse(amount.text) ?? 0;
    if (inv == null) {
      AppExceptionHandler.showError('gold_need_invoice'.tr());
      return;
    }
    if (amt <= 0) {
      AppExceptionHandler.showError('gold_need_amount'.tr());
      return;
    }
    saving.value = true;
    try {
      await AppServices.goldShop.collect(
        CreateGoldCollectionRequest(
          invoiceId: inv.id,
          amount: amt,
          currency: currency.value,
          cashBoxId: selectedCashBox.value?.id,
          notes: notes.text.trim(),
        ),
      );
      AppExceptionHandler.showSuccess('settings_saved'.tr());
      Get.back(result: true);
    } catch (e) {
      AppExceptionHandler.showError(e);
    } finally {
      saving.value = false;
    }
  }
}

class GoldCollectionScreen extends StatelessWidget {
  const GoldCollectionScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final c = Get.put(GoldCollectionController(), tag: 'gold_collection');
    return Obx(() {
      if (c.loading.value) {
        return Scaffold(
          appBar: AppBar(title: Text('gold_collection_title'.tr())),
          body: const Center(child: CircularProgressIndicator()),
        );
      }
      return AppFormPage(
        title: 'gold_collection_title'.tr(),
        formKey: c.formKey,
        saveLabel: 'save'.tr(),
        onSave: c.save,
        isSaving: c.saving,
        sections: [
          AppFormSection(
            title: 'gold_collection_details'.tr(),
            children: [
              Obx(
                () => DropdownButtonFormField<GoldInvoiceListItem>(
                  value: c.selectedInvoice.value,
                  decoration: InputDecoration(
                    labelText: 'gold_invoice'.tr(),
                  ),
                  items: c.openInvoices
                      .map(
                        (i) => DropdownMenuItem(
                          value: i,
                          child: Text(
                            '${i.invoiceNumber} · ${i.customerName} · ${formatCurrency(i.remainingAmount)}',
                          ),
                        ),
                      )
                      .toList(),
                  onChanged: c.selectInvoice,
                ),
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: c.amount,
                keyboardType: TextInputType.number,
                decoration: InputDecoration(labelText: 'amount'.tr()),
              ),
              const SizedBox(height: 12),
              Obx(
                () => DropdownButtonFormField<GoldCashBoxItem>(
                  value: c.selectedCashBox.value,
                  decoration: InputDecoration(
                    labelText: 'gold_cash_box'.tr(),
                  ),
                  items: c.cashBoxes
                      .map(
                        (b) => DropdownMenuItem(
                          value: b,
                          child: Text(b.name),
                        ),
                      )
                      .toList(),
                  onChanged: (v) => c.selectedCashBox.value = v,
                ),
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: c.notes,
                decoration: InputDecoration(labelText: 'notes'.tr()),
              ),
            ],
          ),
        ],
      );
    });
  }
}
