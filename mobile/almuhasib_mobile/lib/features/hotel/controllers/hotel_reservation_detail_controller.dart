import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../shared/widgets/sticky_summary_bar.dart'
    show showErrorSnackbar, showSuccessSnackbar;
import '../models/hotel_models.dart';
import '../models/hotel_status_helpers.dart';

class HotelReservationDetailController extends GetxController {
  HotelReservationDetailController({
    required this.syncId,
    this.initialReservation,
  });

  final String syncId;
  final HotelReservation? initialReservation;

  final reservation = Rxn<HotelReservation>();
  final processing = false.obs;

  @override
  void onInit() {
    super.onInit();
    if (initialReservation != null) {
      reservation.value = initialReservation;
    } else {
      loadReservation();
    }
  }

  Future<void> loadReservation() async {
    try {
      final page = await AppServices.hotel.getReservations(
        search: syncId,
        pageSize: 1,
      );
      final match = page.items.where((r) => r.syncId == syncId).toList();
      reservation.value = match.isNotEmpty
          ? match.first
          : (page.items.isNotEmpty ? page.items.first : null);
    } catch (_) {
      // keep null — UI shows loading
    }
  }

  Future<void> checkIn() async {
    processing.value = true;
    try {
      await AppServices.hotel.checkIn(
        HotelCheckInRequest(reservationSyncId: syncId),
      );
      final ctx = Get.context;
      if (ctx != null) showSuccessSnackbar(ctx, 'hotel_check_in_success'.tr());
      Get.back(result: true);
    } catch (e) {
      final ctx = Get.context;
      if (ctx != null) showErrorSnackbar(ctx, e.toString());
    } finally {
      processing.value = false;
    }
  }

  Future<void> checkOut() async {
    processing.value = true;
    try {
      await AppServices.hotel.checkOut(
        HotelCheckOutRequest(reservationSyncId: syncId),
      );
      final ctx = Get.context;
      if (ctx != null) showSuccessSnackbar(ctx, 'hotel_check_out_success'.tr());
      Get.back(result: true);
    } catch (e) {
      final ctx = Get.context;
      if (ctx != null) showErrorSnackbar(ctx, e.toString());
    } finally {
      processing.value = false;
    }
  }

  Future<void> recordPayment(BuildContext context) async {
    final current = reservation.value;
    if (current == null) return;

    final amountController = TextEditingController(
      text: current.remainingAmount > 0
          ? current.remainingAmount.toString()
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

    if (confirmed != true) {
      amountController.dispose();
      return;
    }
    final amount = double.tryParse(amountController.text.trim());
    amountController.dispose();
    if (amount == null || amount <= 0) return;

    processing.value = true;
    try {
      await AppServices.hotel.recordPayment(
        HotelPaymentRequest(
          reservationSyncId: syncId,
          amount: amount,
        ),
      );
      if (context.mounted) {
        showSuccessSnackbar(context, 'hotel_payment_success'.tr());
      }
      await loadReservation();
    } catch (e) {
      if (context.mounted) showErrorSnackbar(context, e.toString());
    } finally {
      processing.value = false;
    }
  }

  Future<void> primaryAction(BuildContext context) async {
    final current = reservation.value;
    if (current == null) return;

    if (current.status == HotelReservationStatus.confirmed) {
      await checkIn();
    } else if (current.status == HotelReservationStatus.checkedIn) {
      await checkOut();
    } else {
      await recordPayment(context);
    }
  }
}
