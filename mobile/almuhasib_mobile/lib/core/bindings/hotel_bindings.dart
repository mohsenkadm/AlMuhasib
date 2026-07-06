import 'package:get/get.dart';

import '../../features/hotel/controllers/hotel_check_in_out_controller.dart';
import '../../features/hotel/controllers/hotel_dashboard_controller.dart';
import '../../features/hotel/controllers/hotel_guest_form_controller.dart';
import '../../features/hotel/controllers/hotel_guests_controller.dart';
import '../../features/hotel/controllers/hotel_operations_hub_controller.dart';
import '../../features/hotel/models/hotel_models.dart';
import '../../features/hotel/controllers/hotel_reservation_detail_controller.dart';
import '../../features/hotel/controllers/hotel_reservation_form_controller.dart';
import '../../features/hotel/controllers/hotel_reservations_controller.dart';
import '../../features/hotel/controllers/hotel_rooms_controller.dart';
import '../../features/hotel/controllers/hotel_shell_controller.dart';
import '../../features/hotel/restaurant/data/restaurant_pos_controller.dart';
import '../../features/settings/settings_controller.dart';

class HotelShellBinding extends Bindings {
  @override
  void dependencies() {
    if (!Get.isRegistered<HotelShellController>()) {
      Get.lazyPut<HotelShellController>(() => HotelShellController(), fenix: true);
    }
    if (!Get.isRegistered<HotelDashboardController>(tag: 'hotel_dashboard')) {
      Get.lazyPut(
        () => HotelDashboardController(),
        tag: 'hotel_dashboard',
        fenix: true,
      );
    }
    if (!Get.isRegistered<HotelReservationsController>(tag: 'hotel_reservations')) {
      Get.lazyPut(
        () => HotelReservationsController(),
        tag: 'hotel_reservations',
        fenix: true,
      );
    }
    if (!Get.isRegistered<HotelRoomsController>(tag: 'hotel_rooms')) {
      Get.lazyPut(
        () => HotelRoomsController(),
        tag: 'hotel_rooms',
        fenix: true,
      );
    }
    if (!Get.isRegistered<HotelOperationsHubController>()) {
      Get.lazyPut<HotelOperationsHubController>(
        () => HotelOperationsHubController(),
        fenix: true,
      );
    }
    if (!Get.isRegistered<HotelCheckInOutController>(tag: 'hotel_check_in_out')) {
      Get.lazyPut(
        () => HotelCheckInOutController(),
        tag: 'hotel_check_in_out',
        fenix: true,
      );
    }
    if (!Get.isRegistered<HotelGuestsController>(tag: 'hotel_guests')) {
      Get.lazyPut(
        () => HotelGuestsController(),
        tag: 'hotel_guests',
        fenix: true,
      );
    }
    if (!Get.isRegistered<RestaurantPosController>(tag: 'restaurant_hub')) {
      Get.lazyPut(
        () => RestaurantPosController(),
        tag: 'restaurant_hub',
        fenix: true,
      );
    }
    if (!Get.isRegistered<SettingsController>()) {
      Get.lazyPut<SettingsController>(() => SettingsController(), fenix: true);
    }
  }
}

class HotelReservationFormBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => HotelReservationFormController(),
      tag: 'hotel_reservation_form',
    );
  }
}

class HotelReservationDetailBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => HotelReservationDetailController(
        syncId: Get.parameters['syncId']!,
        initialReservation: Get.arguments is HotelReservation
            ? Get.arguments as HotelReservation
            : null,
      ),
      tag: 'hotel_reservation_detail',
    );
  }
}

class HotelGuestFormBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => HotelGuestFormController(
        guest: Get.arguments is HotelGuest ? Get.arguments as HotelGuest : null,
      ),
      tag: 'hotel_guest_form',
    );
  }
}
