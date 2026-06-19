import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/car_models.dart';

class CarContractsController extends GetxController {
  final searchController = TextEditingController();
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final items = <CarContractListItem>[].obs;

  @override
  void onInit() {
    super.onInit();
    load();
  }

  @override
  void onClose() {
    searchController.dispose();
    super.onClose();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      items.value = await AppServices.car.getContracts(
        search: searchController.text.trim(),
      );
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
