import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../models/car_models.dart';

class CarContractFormController extends GetxController {
  final formKey = GlobalKey<FormState>();
  final contractNumber = TextEditingController();
  final sellerName = TextEditingController();
  final buyerName = TextEditingController();
  final plateNumber = TextEditingController();
  final carType = TextEditingController();
  final carPrice = TextEditingController();
  final amountReceived = TextEditingController(text: '0');

  final saving = false.obs;

  @override
  void onClose() {
    contractNumber.dispose();
    sellerName.dispose();
    buyerName.dispose();
    plateNumber.dispose();
    carType.dispose();
    carPrice.dispose();
    amountReceived.dispose();
    super.onClose();
  }

  Future<void> save() async {
    if (!(formKey.currentState?.validate() ?? false)) return;
    saving.value = true;
    try {
      final syncId = await AppServices.car.createContract(
        CreateCarContractRequest(
          contractNumber: contractNumber.text.trim(),
          contractDate: DateTime.now(),
          sellerName: sellerName.text.trim(),
          buyerName: buyerName.text.trim(),
          plateNumber: plateNumber.text.trim(),
          carType: carType.text.trim(),
          carPrice: double.tryParse(carPrice.text) ?? 0,
          amountReceived: double.tryParse(amountReceived.text) ?? 0,
        ),
      );
      Get.offNamed(AppRoutes.carContractDetailPath(syncId));
    } finally {
      saving.value = false;
    }
  }
}
