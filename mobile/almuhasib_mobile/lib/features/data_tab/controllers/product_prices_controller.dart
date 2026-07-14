import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../../../shared/models/master_data_models.dart';

class ProductPricesController extends GetxController {
  ProductPricesController({this.productSyncId});

  final String? productSyncId;

  final search = ''.obs;
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final items = <ProductPriceLookupItem>[].obs;

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
      final loaded = await AppServices.data.getProductPrices(
        productSyncId: productSyncId,
      );
      final term = search.value.trim().toLowerCase();
      if (term.isEmpty) {
        items.value = loaded;
      } else {
        items.value = loaded
            .where(
              (e) =>
                  e.productName.toLowerCase().contains(term) ||
                  e.pricingTypeName.toLowerCase().contains(term),
            )
            .toList();
      }
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
