import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/hotel_models.dart';

class HotelRoomsController extends GetxController {
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final rooms = Rx<List<HotelRoom>>([]);

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      rooms.value = await AppServices.hotel.getRooms();
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
