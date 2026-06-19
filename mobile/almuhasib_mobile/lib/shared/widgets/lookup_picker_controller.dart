import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../models/master_data_models.dart';

class LookupPickerController<T extends LookupItem> extends GetxController {
  LookupPickerController({required this.loadItems});

  final Future<List<T>> Function(String search) loadItems;

  final searchController = TextEditingController();
  final items = <T>[].obs;
  final loading = true.obs;
  final error = Rxn<Object>();

  @override
  void onInit() {
    super.onInit();
    load('');
    searchController.addListener(() => load(searchController.text));
  }

  Future<void> load(String search) async {
    loading.value = true;
    error.value = null;
    try {
      items.value = await loadItems(search);
    } catch (e) {
      error.value = e;
    } finally {
      loading.value = false;
    }
  }

  void clearSearch() {
    searchController.clear();
    load('');
  }

  @override
  void onClose() {
    searchController.dispose();
    super.onClose();
  }
}
