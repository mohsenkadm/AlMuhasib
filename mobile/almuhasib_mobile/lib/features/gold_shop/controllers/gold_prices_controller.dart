import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/gold_shop_models.dart';

class GoldPricesController extends GetxController {
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final items = <GoldMithqalPriceRow>[].obs;

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      items.value = await AppServices.goldShop.getPrices();
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
