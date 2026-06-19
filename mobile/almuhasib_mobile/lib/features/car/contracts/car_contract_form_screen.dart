import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../shared/widgets/common_widgets.dart';
import '../controllers/car_contract_form_controller.dart';

class CarContractFormScreen extends StatelessWidget {
  const CarContractFormScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final controller =
        Get.put(CarContractFormController(), tag: 'car_contract_form');

    return Scaffold(
      appBar: AppBar(title: Text('car_new_contract'.tr())),
      body: Form(
        key: controller.formKey,
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            GradientCard(
              child: Column(
                children: [
                  TextFormField(
                    controller: controller.contractNumber,
                    decoration: InputDecoration(
                      labelText: 'car_contract_number'.tr(),
                    ),
                    validator: (v) =>
                        v == null || v.isEmpty ? 'required'.tr() : null,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: controller.sellerName,
                    decoration: InputDecoration(labelText: 'car_seller'.tr()),
                    validator: (v) =>
                        v == null || v.isEmpty ? 'required'.tr() : null,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: controller.buyerName,
                    decoration: InputDecoration(labelText: 'car_buyer'.tr()),
                    validator: (v) =>
                        v == null || v.isEmpty ? 'required'.tr() : null,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: controller.plateNumber,
                    decoration: InputDecoration(labelText: 'car_plate'.tr()),
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: controller.carType,
                    decoration: InputDecoration(labelText: 'car_type'.tr()),
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: controller.carPrice,
                    keyboardType: TextInputType.number,
                    decoration: InputDecoration(labelText: 'car_price'.tr()),
                    validator: (v) =>
                        v == null || v.isEmpty ? 'required'.tr() : null,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: controller.amountReceived,
                    keyboardType: TextInputType.number,
                    decoration: InputDecoration(labelText: 'car_received'.tr()),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),
            Obx(
              () => FilledButton(
                onPressed: controller.saving.value ? null : controller.save,
                child: controller.saving.value
                    ? const SizedBox(
                        width: 22,
                        height: 22,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : Text('save'.tr()),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
