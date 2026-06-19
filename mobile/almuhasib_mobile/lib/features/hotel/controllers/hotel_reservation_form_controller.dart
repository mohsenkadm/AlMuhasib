import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../models/hotel_models.dart';

class HotelReservationFormController extends GetxController {
  final guestNameController = TextEditingController();
  final totalAmountController = TextEditingController();

  final checkIn = DateTime.now().obs;
  final checkOut = DateTime.now().add(const Duration(days: 1)).obs;
  final selectedRoom = Rxn<HotelRoom>();
  final rooms = <HotelRoom>[].obs;
  final saving = false.obs;

  @override
  void onInit() {
    super.onInit();
    loadRooms();
  }

  Future<void> loadRooms() async {
    rooms.value = await AppServices.hotel.getRooms();
  }

  void selectRoom(HotelRoom? room) => selectedRoom.value = room;

  Future<void> pickCheckIn(BuildContext context) async {
    final date = await showDatePicker(
      context: context,
      initialDate: checkIn.value,
      firstDate: DateTime.now().subtract(const Duration(days: 1)),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (date != null) checkIn.value = date;
  }

  Future<void> pickCheckOut(BuildContext context) async {
    final date = await showDatePicker(
      context: context,
      initialDate: checkOut.value,
      firstDate: checkIn.value,
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (date != null) checkOut.value = date;
  }

  Future<void> save() async {
    if (guestNameController.text.trim().isEmpty) return;
    saving.value = true;
    try {
      final syncId = await AppServices.hotel.createReservation(
        guestName: guestNameController.text.trim(),
        checkIn: checkIn.value,
        checkOut: checkOut.value,
        roomSyncId: selectedRoom.value?.syncId,
        totalAmount: double.tryParse(totalAmountController.text) ?? 0,
      );
      Get.offNamed(AppRoutes.hotelReservationDetailPath(syncId));
    } finally {
      saving.value = false;
    }
  }

  @override
  void onClose() {
    guestNameController.dispose();
    totalAmountController.dispose();
    super.onClose();
  }
}
