import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../../core/getx/app_services.dart';
import '../models/restaurant_models.dart';
import 'restaurant_controller.dart';

class RestaurantPosController extends RestaurantController
    with GetSingleTickerProviderStateMixin {
  late final TabController tabController;

  final currentOrder = Rxn<RestaurantOrder>();
  final cart = RxMap<RestaurantMenuItem, double>({});
  final orderType = 0.obs;
  final selectedCategoryId = ''.obs;

  @override
  Future<void> loadMenu() async {
    await super.loadMenu();
    final categories = menu.value?.categories ?? [];
    if (categories.isNotEmpty &&
        !categories.any((c) => c.syncId == selectedCategoryId.value)) {
      selectedCategoryId.value = categories.first.syncId;
    }
  }

  void selectCategory(String syncId) => selectedCategoryId.value = syncId;

  double get cartTotal => cart.entries.fold(
        0.0,
        (sum, entry) => sum + entry.key.salePrice * entry.value,
      );

  @override
  void onInit() {
    super.onInit();
    tabController = TabController(length: 3, vsync: this);
  }

  Future<void> startOrder() async {
    final order =
        await AppServices.restaurant.createOrder(orderType: orderType.value);
    currentOrder.value = order;
    final ctx = Get.context;
    if (ctx != null) {
      ScaffoldMessenger.of(ctx).showSnackBar(
        SnackBar(
          content: Text(
            '${'restaurant_order_started'.tr()}: ${order.orderNumber}',
          ),
        ),
      );
    }
  }

  void setOrderType(int type) => orderType.value = type;

  Future<void> addToCart(RestaurantMenuItem item) async {
    if (currentOrder.value == null) {
      await startOrder();
    }
    if (currentOrder.value == null) return;

    cart[item] = (cart[item] ?? 0) + 1;
    cart.refresh();

    await AppServices.restaurant.addLine(
      currentOrder.value!.syncId,
      item.syncId,
      1,
    );
  }

  Future<void> pay() async {
    if (currentOrder.value == null || cart.isEmpty) return;
    await AppServices.restaurant.payOrder(
      currentOrder.value!.syncId,
      cartTotal,
      paymentMethod: orderType.value == 2 ? 2 : 0,
    );
    currentOrder.value = null;
    cart.clear();
    cart.refresh();

    final ctx = Get.context;
    if (ctx != null) {
      ScaffoldMessenger.of(ctx).showSnackBar(
        SnackBar(content: Text('restaurant_payment_done'.tr())),
      );
    }
  }

  @override
  void onClose() {
    tabController.dispose();
    super.onClose();
  }
}
