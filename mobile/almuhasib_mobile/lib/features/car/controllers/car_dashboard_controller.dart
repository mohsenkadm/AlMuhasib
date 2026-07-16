import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/car_models.dart';

class CarDashboardController extends GetxController {
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final data = Rxn<CarDashboardDto>();

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    final hasData = data.value != null;
    if (!hasData) {
      isLoading.value = true;
    }
    error.value = null;
    try {
      data.value = await AppServices.car.getDashboard();
    } catch (e) {
      if (!hasData) {
        error.value = e;
      }
    } finally {
      isLoading.value = false;
    }
  }
}
