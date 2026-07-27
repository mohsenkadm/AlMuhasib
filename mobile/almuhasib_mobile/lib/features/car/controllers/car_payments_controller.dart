import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/car_models.dart';

class CarPaymentsController extends GetxController {
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final unpaid = <CarContractListItem>[].obs;

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      final all = await AppServices.car.getContracts(hasRemaining: true);
      unpaid.value = all;
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
