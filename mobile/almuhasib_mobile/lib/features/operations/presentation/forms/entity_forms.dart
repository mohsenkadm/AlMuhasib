import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/providers/core_providers.dart';
import '../../../../shared/models/master_data_models.dart';
import '../../../../shared/models/mobile_models.dart';
import '../../../../shared/widgets/form_section_card.dart';
import '../../../../shared/widgets/lookup_picker_sheet.dart';
import '../../../../shared/widgets/sticky_summary_bar.dart';

class SupplierFormScreen extends ConsumerStatefulWidget {
  const SupplierFormScreen({super.key, this.syncId});

  final String? syncId;

  @override
  ConsumerState<SupplierFormScreen> createState() => _SupplierFormScreenState();
}

class _SupplierFormScreenState extends ConsumerState<SupplierFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _phone = TextEditingController();
  final _address = TextEditingController();
  final _notes = TextEditingController();
  bool _saving = false;

  @override
  void dispose() {
    _name.dispose();
    _phone.dispose();
    _address.dispose();
    _notes.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    try {
      final response = await ref.read(mobileOperationsRepositoryProvider).createSupplier(
            CreateSupplierRequest(
              syncId: widget.syncId,
              name: _name.text.trim(),
              phone: _phone.text.trim().isEmpty ? null : _phone.text.trim(),
              address: _address.text.trim().isEmpty ? null : _address.text.trim(),
              notes: _notes.text.trim().isEmpty ? null : _notes.text.trim(),
            ),
          );
      if (!mounted) return;
      if (response.conflicts.isNotEmpty) {
        showErrorSnackbar(context, response.message);
        return;
      }
      showSuccessSnackbar(context, response.message);
      context.pop(true);
    } catch (e) {
      if (mounted) showErrorSnackbar(context, e.toString());
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('add_supplier'.tr())),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            FormSectionCard(
              title: 'supplier_info'.tr(),
              children: [
                TextFormField(
                  controller: _name,
                  decoration: InputDecoration(labelText: 'name'.tr()),
                  validator: (v) =>
                      v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _phone,
                  decoration: InputDecoration(labelText: 'phone'.tr()),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _address,
                  decoration: InputDecoration(labelText: 'address'.tr()),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _notes,
                  decoration: InputDecoration(labelText: 'notes'.tr()),
                  maxLines: 3,
                ),
              ],
            ),
            FilledButton(
              onPressed: _saving ? null : _save,
              child: Text('save'.tr()),
            ),
          ],
        ),
      ),
    );
  }
}

class InvestorFormScreen extends ConsumerStatefulWidget {
  const InvestorFormScreen({super.key, this.syncId});

  final String? syncId;

  @override
  ConsumerState<InvestorFormScreen> createState() => _InvestorFormScreenState();
}

class _InvestorFormScreenState extends ConsumerState<InvestorFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _phone = TextEditingController();
  final _profitPct = TextEditingController(text: '0');
  final _openingBalance = TextEditingController(text: '0');
  bool _saving = false;

  @override
  void dispose() {
    _name.dispose();
    _phone.dispose();
    _profitPct.dispose();
    _openingBalance.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    try {
      final response = await ref.read(mobileOperationsRepositoryProvider).createInvestor(
            CreateInvestorRequest(
              syncId: widget.syncId,
              name: _name.text.trim(),
              phone: _phone.text.trim().isEmpty ? null : _phone.text.trim(),
              profitPercentage: double.tryParse(_profitPct.text) ?? 0,
              openingBalance: double.tryParse(_openingBalance.text) ?? 0,
            ),
          );
      if (!mounted) return;
      showSuccessSnackbar(context, response.message);
      context.pop(true);
    } catch (e) {
      if (mounted) showErrorSnackbar(context, e.toString());
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('add_investor'.tr())),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            FormSectionCard(
              title: 'investor_info'.tr(),
              children: [
                TextFormField(
                  controller: _name,
                  decoration: InputDecoration(labelText: 'name'.tr()),
                  validator: (v) =>
                      v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _phone,
                  decoration: InputDecoration(labelText: 'phone'.tr()),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _profitPct,
                  decoration: InputDecoration(labelText: 'profit_percentage'.tr()),
                  keyboardType: TextInputType.number,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _openingBalance,
                  decoration: InputDecoration(labelText: 'opening_balance'.tr()),
                  keyboardType: TextInputType.number,
                ),
              ],
            ),
            FilledButton(onPressed: _saving ? null : _save, child: Text('save'.tr())),
          ],
        ),
      ),
    );
  }
}

class ProductFormScreen extends ConsumerStatefulWidget {
  const ProductFormScreen({super.key, this.syncId});

  final String? syncId;

  @override
  ConsumerState<ProductFormScreen> createState() => _ProductFormScreenState();
}

class _ProductFormScreenState extends ConsumerState<ProductFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _barcode = TextEditingController();
  final _description = TextEditingController();
  LookupItem? _category;
  bool _saving = false;

  @override
  void dispose() {
    _name.dispose();
    _barcode.dispose();
    _description.dispose();
    super.dispose();
  }

  Future<void> _pickCategory() async {
    final repo = ref.read(dataRepositoryProvider);
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_category'.tr(),
      loadItems: (search) => repo.getCategories(search: search),
    );
    if (selected != null) setState(() => _category = selected);
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    if (_category == null) {
      showErrorSnackbar(context, 'select_category'.tr());
      return;
    }
    setState(() => _saving = true);
    try {
      final response = await ref.read(mobileOperationsRepositoryProvider).createProduct(
            CreateProductRequest(
              syncId: widget.syncId,
              name: _name.text.trim(),
              categorySyncId: _category!.syncId,
              barcode: _barcode.text.trim().isEmpty ? null : _barcode.text.trim(),
              description:
                  _description.text.trim().isEmpty ? null : _description.text.trim(),
            ),
          );
      if (!mounted) return;
      showSuccessSnackbar(context, response.message);
      context.pop(true);
    } catch (e) {
      if (mounted) showErrorSnackbar(context, e.toString());
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('add_product'.tr())),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            FormSectionCard(
              title: 'product_info'.tr(),
              children: [
                TextFormField(
                  controller: _name,
                  decoration: InputDecoration(labelText: 'name'.tr()),
                  validator: (v) =>
                      v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
                ),
                const SizedBox(height: 12),
                OutlinedButton.icon(
                  onPressed: _pickCategory,
                  icon: const Icon(Icons.category_outlined),
                  label: Text(_category?.name ?? 'select_category'.tr()),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _barcode,
                  decoration: InputDecoration(labelText: 'barcode'.tr()),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _description,
                  decoration: InputDecoration(labelText: 'description'.tr()),
                  maxLines: 3,
                ),
              ],
            ),
            FilledButton(onPressed: _saving ? null : _save, child: Text('save'.tr())),
          ],
        ),
      ),
    );
  }
}
