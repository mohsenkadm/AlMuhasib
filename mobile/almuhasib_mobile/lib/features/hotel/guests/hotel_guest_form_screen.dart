import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/providers/core_providers.dart';
import '../../../shared/widgets/form_section_card.dart';
import '../../../shared/widgets/sticky_summary_bar.dart' show showErrorSnackbar, showSuccessSnackbar;
import '../models/hotel_models.dart';

class HotelGuestFormScreen extends ConsumerStatefulWidget {
  const HotelGuestFormScreen({super.key, this.guest});

  final HotelGuest? guest;

  @override
  ConsumerState<HotelGuestFormScreen> createState() =>
      _HotelGuestFormScreenState();
}

class _HotelGuestFormScreenState extends ConsumerState<HotelGuestFormScreen> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _name;
  late final TextEditingController _idNumber;
  late final TextEditingController _phone;
  late final TextEditingController _email;
  late final TextEditingController _notes;
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    final guest = widget.guest;
    _name = TextEditingController(text: guest?.fullName ?? '');
    _idNumber = TextEditingController(text: guest?.idNumber ?? '');
    _phone = TextEditingController(text: guest?.phone ?? '');
    _email = TextEditingController(text: guest?.email ?? '');
    _notes = TextEditingController(text: guest?.notes ?? '');
  }

  @override
  void dispose() {
    _name.dispose();
    _idNumber.dispose();
    _phone.dispose();
    _email.dispose();
    _notes.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    try {
      final request = HotelGuestUpsertRequest(
        fullName: _name.text.trim(),
        idNumber: _idNumber.text.trim(),
        phone: _phone.text.trim(),
        email: _email.text.trim(),
        notes: _notes.text.trim(),
      );
      final repo = ref.read(hotelRepositoryProvider);
      if (widget.guest != null) {
        await repo.updateGuest(widget.guest!.syncId, request);
      } else {
        await repo.createGuest(request);
      }
      if (!mounted) return;
      showSuccessSnackbar(context, 'settings_saved'.tr());
      context.pop(true);
    } catch (e) {
      if (mounted) showErrorSnackbar(context, e.toString());
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isEdit = widget.guest != null;

    return Scaffold(
      appBar: AppBar(
        title: Text(
          isEdit ? 'hotel_edit_guest'.tr() : 'hotel_add_guest'.tr(),
        ),
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            FormSectionCard(
              title: 'hotel_guest_info'.tr(),
              children: [
                TextFormField(
                  controller: _name,
                  decoration: InputDecoration(labelText: 'name'.tr()),
                  validator: (v) =>
                      v == null || v.trim().isEmpty ? 'required_field'.tr() : null,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _idNumber,
                  decoration: InputDecoration(
                    labelText: 'hotel_id_number'.tr(),
                  ),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _phone,
                  decoration: InputDecoration(labelText: 'phone'.tr()),
                  keyboardType: TextInputType.phone,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _email,
                  decoration: InputDecoration(labelText: 'hotel_email'.tr()),
                  keyboardType: TextInputType.emailAddress,
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
              child: _saving
                  ? const SizedBox(
                      height: 20,
                      width: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : Text('save'.tr()),
            ),
          ],
        ),
      ),
    );
  }
}
