import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/hotel_models.dart';

class HotelDashboardController extends GetxController {
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final data = Rxn<HotelDashboardData>();

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      final repo = AppServices.hotel;
      try {
        data.value = await repo.getDashboard();
      } catch (_) {
        final occupancy = await repo.getOccupancy();
        data.value = HotelDashboardData(occupancy: occupancy);
      }
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
