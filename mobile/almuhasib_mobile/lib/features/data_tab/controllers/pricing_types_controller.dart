import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../../../shared/models/master_data_models.dart';

class PricingTypesController extends GetxController {
  final search = ''.obs;
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final items = <PricingTypeLookupItem>[].obs;

  @override
  void onInit() {
    super.onInit();
    ever(search, (_) => load());
    load();
  }

  void updateSearch(String value) => search.value = value;

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      items.value = await AppServices.data.getPricingTypes(
        search: search.value.trim(),
      );
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
