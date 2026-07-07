import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/car_trade_models.dart';

class CarTradeTransactionDetailController extends GetxController {
  CarTradeTransactionDetailController({required this.syncId});

  final String syncId;

  final isLoading = true.obs;
  final error = Rxn<Object>();
  final transaction = Rxn<CarTradeTransactionDetail>();

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      transaction.value = await AppServices.carTrade.getTransaction(syncId);
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
