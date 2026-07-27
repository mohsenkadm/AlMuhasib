import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/real_estate_models.dart';

class RealEstatePaymentsController extends GetxController {
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final unpaid = <RealEstateContractListItem>[].obs;

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      final all = await AppServices.realEstate.getContracts(hasRemaining: true);
      unpaid.value = all;
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
