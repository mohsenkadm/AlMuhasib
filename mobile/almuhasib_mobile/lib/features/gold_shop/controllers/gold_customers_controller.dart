import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/gold_shop_models.dart';

class GoldCustomersController extends GetxController {
  final search = ''.obs;
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final items = <GoldCustomerListItem>[].obs;

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
      items.value = await AppServices.goldShop.getCustomers(
        search: search.value.trim(),
      );
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
