import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../../../shared/models/master_data_models.dart';

class DataHubController extends GetxController {
  final isLoading = true.obs;
  final settings = Rxn<BusinessSettings>();

  @override
  void onInit() {
    super.onInit();
    load();
  }

  bool get productPricingEnabled =>
      settings.value?.productPricingEnabled ?? false;

  Future<void> load() async {
    isLoading.value = true;
    try {
      settings.value = await AppServices.data.getBusinessSettings();
    } catch (_) {
      settings.value = null;
    } finally {
      isLoading.value = false;
    }
  }
}
