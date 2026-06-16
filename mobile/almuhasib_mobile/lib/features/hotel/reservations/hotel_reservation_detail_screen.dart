import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/constants/app_colors.dart';
import '../../../core/providers/core_providers.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/form_section_card.dart';
import '../../../shared/widgets/sticky_summary_bar.dart' show showErrorSnackbar, showSuccessSnackbar;
import '../models/hotel_models.dart';
import '../models/hotel_status_helpers.dart';

class HotelReservationDetailScreen extends ConsumerStatefulWidget {
  const HotelReservationDetailScreen({
    super.key,
    required this.syncId,
    this.reservation,
  });

  final String syncId;
  final HotelReservation? reservation;

  @override
  ConsumerState<HotelReservationDetailScreen> createState() =>
      _HotelReservationDetailScreenState();
}

class _HotelReservationDetailScreenState
    extends ConsumerState<HotelReservationDetailScreen> {
  bool _processing = false;
  late HotelReservation? _reservation;

  @override
  void initState() {
    super.initState();
    _reservation = widget.reservation;
    if (_reservation == null) {
      _loadReservation();
    }
  }

  Future<void> _loadReservation() async {
    try {
      final page = await ref
          .read(hotelRepositoryProvider)
          .getReservations(search: widget.syncId, pageSize: 1);
      if (!mounted) return;
      setState(() {
        final match =
            page.items.where((r) => r.syncId == widget.syncId).toList();
        _reservation = match.isNotEmpty
            ? match.first
            : (page.items.isNotEmpty ? page.items.first : null);
      });
    } catch (_) {
      // keep null — UI shows loading/error via build
    }
  }

  Future<void> _checkIn() async {
    setState(() => _processing = true);
    try {
      await ref.read(hotelRepositoryProvider).checkIn(
            HotelCheckInRequest(reservationSyncId: widget.syncId),
          );
      if (!mounted) return;
      showSuccessSnackbar(context, 'hotel_check_in_success'.tr());
      Navigator.of(context).pop(true);
    } catch (e) {
      if (mounted) showErrorSnackbar(context, e.toString());
    } finally {
      if (mounted) setState(() => _processing = false);
    }
  }

  Future<void> _checkOut() async {
    setState(() => _processing = true);
    try {
      await ref.read(hotelRepositoryProvider).checkOut(
            HotelCheckOutRequest(reservationSyncId: widget.syncId),
          );
      if (!mounted) return;
      showSuccessSnackbar(context, 'hotel_check_out_success'.tr());
      Navigator.of(context).pop(true);
    } catch (e) {
      if (mounted) showErrorSnackbar(context, e.toString());
    } finally {
      if (mounted) setState(() => _processing = false);
    }
  }

  Future<void> _recordPayment() async {
    final reservation = _reservation;
    if (reservation == null) return;
    final amountController = TextEditingController(
      text: reservation.remainingAmount > 0
          ? reservation.remainingAmount.toString()
          : '',
    );

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('hotel_record_payment'.tr()),
        content: TextField(
          controller: amountController,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          decoration: InputDecoration(labelText: 'total'.tr()),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: Text('cancel'.tr()),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: Text('save'.tr()),
          ),
        ],
      ),
    );

    if (confirmed != true) return;
    final amount = double.tryParse(amountController.text.trim());
    if (amount == null || amount <= 0) return;

    setState(() => _processing = true);
    try {
      await ref.read(hotelRepositoryProvider).recordPayment(
            HotelPaymentRequest(
              reservationSyncId: widget.syncId,
              amount: amount,
            ),
          );
      if (!mounted) return;
      showSuccessSnackbar(context, 'hotel_payment_success'.tr());
    } catch (e) {
      if (mounted) showErrorSnackbar(context, e.toString());
    } finally {
      if (mounted) setState(() => _processing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final reservation = _reservation;
    final statusColor = reservation != null
        ? hotelReservationStatusColor(reservation.status)
        : AppColors.primaryLight;

    return Scaffold(
      appBar: AppBar(title: Text('hotel_reservation_detail'.tr())),
      body: reservation == null
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                FormSectionCard(
                  title: 'hotel_reservation_info'.tr(),
                  children: [
                    _InfoRow(
                      label: 'hotel_reservation_number'.tr(),
                      value: reservation.reservationNumber,
                    ),
                    _InfoRow(
                      label: 'hotel_guest'.tr(),
                      value: reservation.guestName,
                    ),
                    _InfoRow(
                      label: 'hotel_room'.tr(),
                      value: reservation.roomNumber ?? '—',
                    ),
                    _InfoRow(
                      label: 'hotel_check_in'.tr(),
                      value: formatDate(reservation.checkInDate),
                    ),
                    _InfoRow(
                      label: 'hotel_check_out'.tr(),
                      value: formatDate(reservation.checkOutDate),
                    ),
                    _InfoRow(
                      label: 'hotel_status'.tr(),
                      value: hotelReservationStatusLabel(reservation.status),
                      valueColor: statusColor,
                    ),
                  ],
                ),
                FormSectionCard(
                  title: 'hotel_payment_info'.tr(),
                  children: [
                    _InfoRow(
                      label: 'total'.tr(),
                      value: formatCurrency(reservation.totalAmount),
                    ),
                    _InfoRow(
                      label: 'hotel_amount_paid'.tr(),
                      value: formatCurrency(reservation.amountPaid),
                    ),
                    _InfoRow(
                      label: 'hotel_remaining'.tr(),
                      value: formatCurrency(reservation.remainingAmount),
                    ),
                  ],
                ),
                if (reservation.notes != null && reservation.notes!.isNotEmpty)
                  FormSectionCard(
                    title: 'notes'.tr(),
                    children: [Text(reservation.notes!)],
                  ),
                const SizedBox(height: 80),
              ],
            ),
      bottomNavigationBar: reservation == null
          ? null
          : SafeArea(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    if (reservation.remainingAmount > 0 &&
                        reservation.status == HotelReservationStatus.checkedIn)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 8),
                        child: OutlinedButton(
                          onPressed: _processing ? null : _recordPayment,
                          child: Text('hotel_record_payment'.tr()),
                        ),
                      ),
                    FilledButton(
                      onPressed: _processing
                          ? null
                          : () {
                              if (reservation.status ==
                                  HotelReservationStatus.confirmed) {
                                _checkIn();
                              } else if (reservation.status ==
                                  HotelReservationStatus.checkedIn) {
                                _checkOut();
                              } else {
                                _recordPayment();
                              }
                            },
                      child: _processing
                          ? const SizedBox(
                              height: 20,
                              width: 20,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : Text(
                              reservation.status ==
                                      HotelReservationStatus.confirmed
                                  ? 'hotel_check_in'.tr()
                                  : reservation.status ==
                                          HotelReservationStatus.checkedIn
                                      ? 'hotel_check_out'.tr()
                                      : 'hotel_record_payment'.tr(),
                            ),
                    ),
                  ],
                ),
              ),
            ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({
    required this.label,
    required this.value,
    this.valueColor,
  });

  final String label;
  final String value;
  final Color? valueColor;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            flex: 2,
            child: Text(
              label,
              style: Theme.of(context).textTheme.bodyMedium,
            ),
          ),
          Expanded(
            flex: 3,
            child: Text(
              value,
              textAlign: TextAlign.end,
              style: Theme.of(context).textTheme.titleSmall?.copyWith(
                    color: valueColor,
                    fontWeight: FontWeight.w600,
                  ),
            ),
          ),
        ],
      ),
    );
  }
}
