import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/car_models.dart';

class CarPaymentsController extends GetxController {
  final isLoading = true.obs;
  final unpaid = <CarContractListItem>[].obs;

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    try {
      final all = await AppServices.car.getContracts();
      unpaid.value = all.where((c) => c.remainingAmount > 0).toList();
    } finally {
      isLoading.value = false;
    }
  }
}
