import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/car_models.dart';

class CarContractDetailController extends GetxController {
  CarContractDetailController({required this.syncId});

  final String syncId;

  final isLoading = true.obs;
  final error = Rxn<Object>();
  final contract = Rxn<CarContractDetail>();

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      contract.value = await AppServices.car.getContract(syncId);
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
