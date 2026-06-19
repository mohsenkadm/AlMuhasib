import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/hotel_models.dart';

class HotelCheckInOutController extends GetxController {
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final reservations = Rx<List<HotelReservation>>([]);

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      reservations.value = await AppServices.hotel.getTodayReservations();
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }

  Future<void> checkIn(HotelReservation reservation) async {
    await AppServices.hotel.checkIn(
      HotelCheckInRequest(reservationSyncId: reservation.syncId),
    );
    await load();
  }

  Future<void> checkOut(HotelReservation reservation) async {
    await AppServices.hotel.checkOut(
      HotelCheckOutRequest(reservationSyncId: reservation.syncId),
    );
    await load();
  }
}
