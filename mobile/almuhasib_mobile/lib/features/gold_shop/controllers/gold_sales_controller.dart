import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/gold_shop_models.dart';

class GoldSalesController extends GetxController {
  final search = ''.obs;
  final statusFilter = RxnInt();
  final typeFilter = RxnInt(0);
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final items = <GoldInvoiceListItem>[].obs;

  @override
  void onInit() {
    super.onInit();
    ever(search, (_) => load());
    load();
  }

  void updateSearch(String value) => search.value = value;

  void updateStatusFilter(int? status) {
    statusFilter.value = status;
    load();
  }

  void updateTypeFilter(int? type) {
    typeFilter.value = type;
    load();
  }

  void clearFilters() {
    search.value = '';
    statusFilter.value = null;
    typeFilter.value = 0;
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      items.value = await AppServices.goldShop.getInvoices(
        search: search.value.trim(),
        status: statusFilter.value,
        invoiceType: typeFilter.value,
      );
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
