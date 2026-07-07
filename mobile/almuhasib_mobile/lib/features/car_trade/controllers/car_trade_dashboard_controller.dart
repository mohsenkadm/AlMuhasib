import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/car_trade_models.dart';

class CarTradeDashboardController extends GetxController {
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final data = Rxn<CarTradeDashboardDto>();

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      data.value = await AppServices.carTrade.getDashboard();
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
