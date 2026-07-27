import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../models/real_estate_models.dart';

class RealEstateContractFormController extends GetxController {
  final formKey = GlobalKey<FormState>();
  final contractNumber = TextEditingController();
  final sellerName = TextEditingController();
  final buyerName = TextEditingController();
  final propertyLocation = TextEditingController();
  final propertyAddress = TextEditingController();
  final propertyAreaSqm = TextEditingController();
  final totalPrice = TextEditingController();
  final downPayment = TextEditingController(text: '0');
  final amountPaid = TextEditingController(text: '0');
  final witnessOneName = TextEditingController();
  final witnessTwoName = TextEditingController();
  final notes = TextEditingController();

  final contractType = 0.obs;
  final propertyType = 0.obs;
  final paymentMode = 0.obs;
  final debtorParty = 0.obs;
  final saving = false.obs;

  @override
  void onClose() {
    contractNumber.dispose();
    sellerName.dispose();
    buyerName.dispose();
    propertyLocation.dispose();
    propertyAddress.dispose();
    propertyAreaSqm.dispose();
    totalPrice.dispose();
    downPayment.dispose();
    amountPaid.dispose();
    witnessOneName.dispose();
    witnessTwoName.dispose();
    notes.dispose();
    super.onClose();
  }

  void setContractType(int value) => contractType.value = value;
  void setPropertyType(int value) => propertyType.value = value;
  void setPaymentMode(int value) {
    paymentMode.value = value;
    if (value == 0) debtorParty.value = 0;
  }

  void setDebtorParty(int value) => debtorParty.value = value;

  Future<void> save() async {
    if (!(formKey.currentState?.validate() ?? false)) return;
    saving.value = true;
    try {
      final syncId = await AppServices.realEstate.createContract(
        CreateRealEstateContractRequest(
          contractNumber: contractNumber.text.trim(),
          contractDate: DateTime.now(),
          contractType: contractType.value,
          propertyType: propertyType.value,
          propertyLocation: propertyLocation.text.trim(),
          propertyAddress: propertyAddress.text.trim(),
          propertyAreaSqm: double.tryParse(propertyAreaSqm.text) ?? 0,
          sellerName: sellerName.text.trim(),
          buyerName: buyerName.text.trim(),
          totalPrice: double.tryParse(totalPrice.text) ?? 0,
          downPayment: double.tryParse(downPayment.text) ?? 0,
          amountPaid: double.tryParse(amountPaid.text) ?? 0,
          paymentMode: paymentMode.value,
          debtorParty: paymentMode.value == 1 ? debtorParty.value : 0,
          witnessOneName: witnessOneName.text.trim(),
          witnessTwoName: witnessTwoName.text.trim(),
          notes: notes.text.trim(),
        ),
      );
      AppExceptionHandler.showSuccess('settings_saved'.tr());
      Get.offNamed(AppRoutes.realEstateContractDetailPath(syncId));
    } catch (e) {
      AppExceptionHandler.showError(e);
    } finally {
      saving.value = false;
    }
  }
}
