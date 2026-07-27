import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/hotel_models.dart';
import '../models/hotel_status_helpers.dart';

class HotelRoomsController extends GetxController {
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final rooms = Rx<List<HotelRoom>>([]);
  final statusFilter = RxnInt();

  @override
  void onInit() {
    super.onInit();
    load();
  }

  void updateStatusFilter(int? status) {
    statusFilter.value = status;
    load();
  }

  void clearStatusFilter() {
    statusFilter.value = null;
    load();
  }

  String? get statusQuery {
    final status = statusFilter.value;
    if (status == null) return null;
    return switch (status) {
      HotelRoomStatus.available => 'Available',
      HotelRoomStatus.occupied => 'Occupied',
      HotelRoomStatus.dirty => 'Dirty',
      HotelRoomStatus.maintenance => 'Maintenance',
      HotelRoomStatus.outOfOrder => 'OutOfOrder',
      _ => null,
    };
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      rooms.value = await AppServices.hotel.getRooms(status: statusQuery);
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
