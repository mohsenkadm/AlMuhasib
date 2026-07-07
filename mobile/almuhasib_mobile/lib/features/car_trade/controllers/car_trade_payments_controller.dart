import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/car_trade_models.dart';

class CarTradePaymentsController extends GetxController {
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final unpaid = <CarTradeTransactionListItem>[].obs;

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      unpaid.value =
          await AppServices.carTrade.getTransactions(hasRemaining: true);
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
