import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/hotel_models.dart';

class HotelReservationsController extends GetxController {
  final search = ''.obs;
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final page = Rxn<HotelReservationPage>();

  @override
  void onInit() {
    super.onInit();
    ever(search, (_) => load());
    load();
  }

  void updateSearch(String value) {
    search.value = value;
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      page.value = await AppServices.hotel.getReservations(
        search: search.value,
        pageSize: 50,
      );
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
