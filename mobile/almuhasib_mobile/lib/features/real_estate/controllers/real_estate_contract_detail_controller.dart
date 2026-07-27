import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/real_estate_models.dart';

class RealEstateContractDetailController extends GetxController {
  RealEstateContractDetailController({required this.syncId});

  final String syncId;

  final isLoading = true.obs;
  final error = Rxn<Object>();
  final contract = Rxn<RealEstateContractDetail>();

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      contract.value = await AppServices.realEstate.getContract(syncId);
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
