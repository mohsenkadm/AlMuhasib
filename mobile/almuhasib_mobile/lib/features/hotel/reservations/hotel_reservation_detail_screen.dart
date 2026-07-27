import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/form_section_card.dart';
import '../controllers/hotel_reservation_detail_controller.dart';
import '../models/hotel_models.dart';
import '../models/hotel_status_helpers.dart';

class HotelReservationDetailScreen
    extends GetView<HotelReservationDetailController> {
  const HotelReservationDetailScreen({
    super.key,
    required this.syncId,
    this.reservation,
  });

  @override
  final String? tag = 'hotel_reservation_detail';

  final String syncId;
  final HotelReservation? reservation;

  @override
  Widget build(BuildContext context) {
    return Obx(() {
      final reservation = controller.reservation.value;
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
                  if (reservation.notes != null &&
                      reservation.notes!.isNotEmpty)
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
                          reservation.status ==
                              HotelReservationStatus.checkedIn)
                        Padding(
                          padding: const EdgeInsets.only(bottom: 8),
                          child: OutlinedButton(
                            onPressed: controller.processing.value
                                ? null
                                : () => controller.recordPayment(context),
                            child: Text('hotel_record_payment'.tr()),
                          ),
                        ),
                      FilledButton(
                        onPressed: controller.processing.value
                            ? null
                            : () => controller.primaryAction(context),
                        child: controller.processing.value
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
    });
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
