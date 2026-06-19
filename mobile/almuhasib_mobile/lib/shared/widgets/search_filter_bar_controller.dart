import 'package:flutter/material.dart';
import 'package:get/get.dart';

class SearchFilterBarController extends GetxController {
  SearchFilterBarController({required this.onSearchChanged});

  final ValueChanged<String> onSearchChanged;

  final searchController = TextEditingController();
  final selectedFilter = RxnString();
  final hasText = false.obs;

  void onChanged(String value) {
    hasText.value = value.isNotEmpty;
    Future.delayed(const Duration(milliseconds: 350), () {
      if (searchController.text == value) {
        onSearchChanged(value);
      }
    });
  }

  void clear() {
    searchController.clear();
    hasText.value = false;
    onSearchChanged('');
  }

  void toggleFilter(String chipId, bool wasSelected) {
    selectedFilter.value = wasSelected ? null : chipId;
  }

  @override
  void onClose() {
    searchController.dispose();
    super.onClose();
  }
}
