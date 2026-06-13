import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/providers/core_providers.dart';
import '../../../../shared/models/master_data_models.dart';
import '../../../../shared/models/mobile_models.dart';
import '../../../../shared/utils/formatters.dart';
import '../../../../shared/widgets/form_section_card.dart';
import '../../../../shared/widgets/lookup_picker_sheet.dart';
import '../../../../shared/widgets/sticky_summary_bar.dart';

class InvoiceWizardScreen extends ConsumerStatefulWidget {
  const InvoiceWizardScreen({super.key});

  @override
  ConsumerState<InvoiceWizardScreen> createState() => _InvoiceWizardScreenState();
}

class _InvoiceWizardScreenState extends ConsumerState<InvoiceWizardScreen> {
  int _step = 0;
  int _invoiceType = 1; // Sale
  int _paymentMethod = 0; // Cash
  DateTime _date = DateTime.now();
  DateTime? _creditDueDate;
  LookupItem? _customer;
  LookupItem? _supplier;
  LookupItem? _warehouse;
  LookupItem? _cashBox;
  final _discountController = TextEditingController(text: '0');
  final _notesController = TextEditingController();
  final List<_WizardLineItem> _items = [];
  int _installmentCount = 6;
  DateTime _installmentStart = DateTime.now();
  bool _saving = false;

  @override
  void dispose() {
    _discountController.dispose();
    _notesController.dispose();
    super.dispose();
  }

  bool get _needsCustomer =>
      _invoiceType == 1 || _invoiceType == 2; // Sale, Installment
  bool get _needsSupplier =>
      _invoiceType == 0 || _invoiceType == 3; // Purchase, PurchaseReturn
  bool get _needsInstallmentPlan =>
      _paymentMethod == 2 || _invoiceType == 2;

  double get _subtotal =>
      _items.fold(0, (sum, i) => sum + i.quantity * i.unitPrice - i.discount);
  double get _discount => double.tryParse(_discountController.text) ?? 0;
  double get _net => _subtotal - _discount;

  Future<void> _pickLookup({
    required String title,
    required Future<List<LookupItem>> Function(String) loader,
    required void Function(LookupItem) onSelected,
  }) async {
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: title,
      loadItems: loader,
    );
    if (selected != null) onSelected(selected);
  }

  Future<void> _pickProduct() async {
    final repo = ref.read(dataRepositoryProvider);
    final product = await showLookupPickerSheet<ProductLookupItem>(
      context: context,
      title: 'select_product'.tr(),
      loadItems: (search) => repo.getProducts(search: search),
    );
    if (product == null) return;
    setState(() {
      _items.add(_WizardLineItem(
        productSyncId: product.syncId,
        itemName: product.name,
        quantity: 1,
        unitPrice: 0,
      ));
    });
  }

  Future<void> _save() async {
    if (_warehouse == null) {
      showErrorSnackbar(context, 'select_warehouse'.tr());
      return;
    }
    if (_needsCustomer && _customer == null) {
      showErrorSnackbar(context, 'select_customer'.tr());
      return;
    }
    if (_needsSupplier && _supplier == null) {
      showErrorSnackbar(context, 'select_supplier'.tr());
      return;
    }
    if (_paymentMethod == 0 && _cashBox == null) {
      showErrorSnackbar(context, 'select_cashbox'.tr());
      return;
    }
    if (_items.isEmpty) {
      showErrorSnackbar(context, 'add_line_item'.tr());
      return;
    }

    setState(() => _saving = true);
    try {
      final repo = ref.read(mobileOperationsRepositoryProvider);
      final response = await repo.createInvoice(
        CreateInvoiceRequest(
          invoiceType: _invoiceType,
          customerSyncId: _customer?.syncId,
          supplierSyncId: _supplier?.syncId,
          warehouseSyncId: _warehouse!.syncId,
          paymentMethod: _paymentMethod,
          cashBoxSyncId: _cashBox?.syncId,
          date: _date,
          creditDueDate: _creditDueDate,
          discountAmount: _discount,
          notes: _notesController.text.trim().isEmpty
              ? null
              : _notesController.text.trim(),
          items: _items
              .map(
                (i) => CreateInvoiceItemRequest(
                  productSyncId: i.productSyncId,
                  itemName: i.itemName,
                  quantity: i.quantity,
                  unitPrice: i.unitPrice,
                  discountAmount: i.discount,
                ),
              )
              .toList(),
          installmentPlan: _needsInstallmentPlan
              ? CreateInstallmentPlanRequest(
                  numberOfInstallments: _installmentCount,
                  startDate: _installmentStart,
                )
              : null,
        ),
      );
      if (!mounted) return;
      if (response.conflicts.isNotEmpty) {
        showErrorSnackbar(context, response.message);
        return;
      }
      showSuccessSnackbar(
        context,
        '${response.message} ${response.invoiceNumber ?? ''}',
      );
      context.pop(true);
    } catch (e) {
      if (mounted) showErrorSnackbar(context, e.toString());
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  void _next() {
    if (_step < 4) {
      setState(() => _step++);
    } else {
      _save();
    }
  }

  void _back() {
    if (_step > 0) setState(() => _step--);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('new_invoice'.tr()),
            Text(
              'wizard_step'.tr(args: ['${_step + 1}', '5']),
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ],
        ),
      ),
      body: Column(
        children: [
          LinearProgressIndicator(value: (_step + 1) / 5),
          Expanded(
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                switch (_step) {
                  0 => _buildTypeStep(),
                  1 => _buildPartyStep(),
                  2 => _buildItemsStep(),
                  3 => _buildPaymentStep(),
                  _ => _buildReviewStep(),
                },
              ],
            ),
          ),
          StickySummaryBar(
            label: 'net_amount'.tr(),
            amount: formatCurrency(_net),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                if (_step > 0)
                  Expanded(
                    child: OutlinedButton(onPressed: _back, child: Text('back'.tr())),
                  ),
                if (_step > 0) const SizedBox(width: 12),
                Expanded(
                  child: FilledButton(
                    onPressed: _saving ? null : _next,
                    child: _saving
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : Text(_step == 4 ? 'save'.tr() : 'next'.tr()),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildTypeStep() {
    return FormSectionCard(
      title: 'invoice_type'.tr(),
      children: [
        ...[
          (0, 'purchase'.tr()),
          (1, 'sale'.tr()),
          (2, 'installment'.tr()),
          (3, 'purchase_return'.tr()),
        ].map(
          (e) => RadioListTile<int>(
            value: e.$1,
            groupValue: _invoiceType,
            title: Text(e.$2),
            onChanged: (v) => setState(() {
              _invoiceType = v!;
              if (_invoiceType == 2) _paymentMethod = 2;
            }),
          ),
        ),
      ],
    );
  }

  Widget _buildPartyStep() {
    final repo = ref.read(dataRepositoryProvider);
    return Column(
      children: [
        if (_needsCustomer)
          FormSectionCard(
            title: 'customers'.tr(),
            children: [
              OutlinedButton.icon(
                onPressed: () => _pickLookup(
                  title: 'select_customer'.tr(),
                  loader: (search) => repo.getCustomers(search: search),
                  onSelected: (c) => setState(() => _customer = c),
                ),
                icon: const Icon(Icons.person),
                label: Text(_customer?.name ?? 'select_customer'.tr()),
              ),
            ],
          ),
        if (_needsSupplier)
          FormSectionCard(
            title: 'suppliers'.tr(),
            children: [
              OutlinedButton.icon(
                onPressed: () => _pickLookup(
                  title: 'select_supplier'.tr(),
                  loader: (search) => repo.getSuppliers(search: search),
                  onSelected: (s) => setState(() => _supplier = s),
                ),
                icon: const Icon(Icons.local_shipping),
                label: Text(_supplier?.name ?? 'select_supplier'.tr()),
              ),
            ],
          ),
        FormSectionCard(
          title: 'warehouses'.tr(),
          children: [
            OutlinedButton.icon(
              onPressed: () => _pickLookup(
                title: 'select_warehouse'.tr(),
                loader: (search) => repo.getWarehouses(search: search),
                onSelected: (w) => setState(() => _warehouse = w),
              ),
              icon: const Icon(Icons.warehouse),
              label: Text(_warehouse?.name ?? 'select_warehouse'.tr()),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildItemsStep() {
    return Column(
      children: [
        Align(
          alignment: AlignmentDirectional.centerEnd,
          child: FilledButton.icon(
            onPressed: _pickProduct,
            icon: const Icon(Icons.add),
            label: Text('add_product'.tr()),
          ),
        ),
        const SizedBox(height: 12),
        ..._items.asMap().entries.map((entry) {
          final i = entry.value;
          final index = entry.key;
          return Card(
            margin: const EdgeInsets.only(bottom: 8),
            child: Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(
                    children: [
                      Expanded(child: Text(i.itemName, style: Theme.of(context).textTheme.titleSmall)),
                      IconButton(
                        icon: const Icon(Icons.delete_outline),
                        onPressed: () => setState(() => _items.removeAt(index)),
                      ),
                    ],
                  ),
                  Row(
                    children: [
                      Expanded(
                        child: TextFormField(
                          initialValue: '${i.quantity}',
                          decoration: InputDecoration(labelText: 'quantity'.tr()),
                          keyboardType: TextInputType.number,
                          onChanged: (v) => i.quantity = double.tryParse(v) ?? i.quantity,
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: TextFormField(
                          initialValue: '${i.unitPrice}',
                          decoration: InputDecoration(labelText: 'unit_price'.tr()),
                          keyboardType: TextInputType.number,
                          onChanged: (v) => i.unitPrice = double.tryParse(v) ?? i.unitPrice,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          );
        }),
      ],
    );
  }

  Widget _buildPaymentStep() {
    final repo = ref.read(dataRepositoryProvider);
    return Column(
      children: [
        FormSectionCard(
          title: 'payment_method'.tr(),
          children: [
            ...[
              (0, 'cash'.tr()),
              (1, 'credit'.tr()),
              (2, 'installment'.tr()),
            ].map(
              (e) => RadioListTile<int>(
                value: e.$1,
                groupValue: _paymentMethod,
                title: Text(e.$2),
                onChanged: (v) => setState(() => _paymentMethod = v!),
              ),
            ),
            if (_paymentMethod == 0)
              OutlinedButton.icon(
                onPressed: () => _pickLookup(
                  title: 'select_cashbox'.tr(),
                  loader: (search) => repo.getCashBoxes(search: search),
                  onSelected: (c) => setState(() => _cashBox = c),
                ),
                icon: const Icon(Icons.account_balance_wallet),
                label: Text(_cashBox?.name ?? 'select_cashbox'.tr()),
              ),
            TextFormField(
              controller: _discountController,
              decoration: InputDecoration(labelText: 'discount'.tr()),
              keyboardType: TextInputType.number,
              onChanged: (_) => setState(() {}),
            ),
            if (_needsInstallmentPlan) ...[
              const SizedBox(height: 12),
              TextFormField(
                initialValue: '$_installmentCount',
                decoration: InputDecoration(labelText: 'installment_count'.tr()),
                keyboardType: TextInputType.number,
                onChanged: (v) =>
                    setState(() => _installmentCount = int.tryParse(v) ?? _installmentCount),
              ),
            ],
          ],
        ),
      ],
    );
  }

  Widget _buildReviewStep() {
    return FormSectionCard(
      title: 'review'.tr(),
      children: [
        Text('${'invoice_type'.tr()}: ${invoiceTypeLabel(_invoiceType)}'),
        Text('${'payment_method'.tr()}: ${paymentMethodLabel(_paymentMethod)}'),
        if (_customer != null) Text('${'customers'.tr()}: ${_customer!.name}'),
        if (_supplier != null) Text('${'suppliers'.tr()}: ${_supplier!.name}'),
        if (_warehouse != null) Text('${'warehouses'.tr()}: ${_warehouse!.name}'),
        Text('${'items'.tr()}: ${_items.length}'),
        Text('${'net_amount'.tr()}: ${formatCurrency(_net)}'),
        TextFormField(
          controller: _notesController,
          decoration: InputDecoration(labelText: 'notes'.tr()),
          maxLines: 2,
        ),
      ],
    );
  }
}

class _WizardLineItem {
  _WizardLineItem({
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
