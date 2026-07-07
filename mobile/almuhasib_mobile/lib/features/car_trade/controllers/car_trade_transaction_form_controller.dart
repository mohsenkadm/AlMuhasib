import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../models/car_trade_models.dart';

class CarTradeTransactionFormController extends GetxController {
  final formKey = GlobalKey<FormState>();
  final transactionNumber = TextEditingController();
  final sellerName = TextEditingController();
  final buyerName = TextEditingController();
  final plateNumber = TextEditingController();
  final carType = TextEditingController();
  final carName = TextEditingController();
  final totalAmount = TextEditingController();
  final amountPaid = TextEditingController(text: '0');

  final tradeType = 'Buy'.obs;
  final saving = false.obs;

  @override
  void onClose() {
    transactionNumber.dispose();
    sellerName.dispose();
    buyerName.dispose();
    plateNumber.dispose();
    carType.dispose();
    carName.dispose();
    totalAmount.dispose();
    amountPaid.dispose();
    super.onClose();
  }

  void setTradeType(String type) => tradeType.value = type;

  Future<void> save() async {
    if (!(formKey.currentState?.validate() ?? false)) return;
    saving.value = true;
    try {
      final amount = double.tryParse(totalAmount.text) ?? 0;
      final paid = double.tryParse(amountPaid.text) ?? 0;
      final syncId = await AppServices.carTrade.createTransaction(
        CreateCarTradeTransactionRequest(
          transactionNumber: transactionNumber.text.trim(),
          transactionDate: DateTime.now(),
          tradeType: tradeType.value,
          sellerName: sellerName.text.trim(),
          buyerName: buyerName.text.trim(),
          plateNumber: plateNumber.text.trim(),
          carType: carType.text.trim(),
          carName: carName.text.trim(),
          totalAmount: amount,
          amountPaid: paid,
          purchasePrice: tradeType.value == 'Buy' ? amount : 0,
          salePrice: tradeType.value == 'Sell' ? amount : 0,
        ),
      );
      AppExceptionHandler.showSuccess('settings_saved'.tr());
      Get.offNamed(AppRoutes.carTradeTransactionDetailPath(syncId));
    } catch (e) {
      AppExceptionHandler.showError(e);
    } finally {
      saving.value = false;
    }
  }
}
