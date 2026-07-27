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
  final tables = Rx<List<RestaurantTable>>([]);
  final activeRooms = Rx<List<ActiveRoom>>([]);
  final openOrders = Rx<List<RestaurantOrder>>([]);
  final selectedTableSyncId = ''.obs;
  final selectedReservationSyncId = ''.obs;
  final isLookupsLoading = false.obs;

  @override
  Future<void> loadMenu() async {
    await super.loadMenu();
    final categories = menu.value?.categories ?? [];
    if (categories.isNotEmpty &&
        !categories.any((c) => c.syncId == selectedCategoryId.value)) {
      selectedCategoryId.value = categories.first.syncId;
    }
    await loadLookups();
  }

  Future<void> loadLookups() async {
    isLookupsLoading.value = true;
    try {
      final results = await Future.wait([
        AppServices.restaurant.getTables(),
        AppServices.restaurant.getActiveRooms(),
        AppServices.restaurant.getOpenOrders(),
      ]);
      tables.value = results[0] as List<RestaurantTable>;
      activeRooms.value = results[1] as List<ActiveRoom>;
      openOrders.value = results[2] as List<RestaurantOrder>;
    } catch (_) {
      // Lookups are best-effort; POS can still load menu.
    } finally {
      isLookupsLoading.value = false;
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

  void setOrderType(int type) {
    orderType.value = type;
    selectedTableSyncId.value = '';
    selectedReservationSyncId.value = '';
  }

  void selectTable(RestaurantTable table) {
    selectedTableSyncId.value = table.syncId;
    if (table.isOccupied) {
      final matches = openOrders.value
          .where((o) => o.tableSyncId == table.syncId)
          .toList();
      if (matches.isNotEmpty) {
        resumeOrder(matches.first);
      }
    }
  }

  void selectRoom(ActiveRoom room) {
    selectedReservationSyncId.value = room.reservationSyncId;
  }

  void resumeOrder(RestaurantOrder order) {
    currentOrder.value = order;
    orderType.value = order.orderType;
    if (order.tableSyncId != null) {
      selectedTableSyncId.value = order.tableSyncId!;
    }
    cart.clear();
    cart.refresh();
    final ctx = Get.context;
    if (ctx != null) {
      ScaffoldMessenger.of(ctx).showSnackBar(
        SnackBar(
          content: Text(
            '${'restaurant_order_resumed'.tr()}: ${order.orderNumber}',
          ),
        ),
      );
    }
  }

  Future<void> startOrder() async {
    if (orderType.value == 0 && selectedTableSyncId.value.isEmpty) {
      _toast('restaurant_select_table'.tr());
      return;
    }
    if (orderType.value == 2 && selectedReservationSyncId.value.isEmpty) {
      _toast('restaurant_select_room'.tr());
      return;
    }

    final order = await AppServices.restaurant.createOrder(
      orderType: orderType.value,
      tableSyncId:
          orderType.value == 0 ? selectedTableSyncId.value : null,
      reservationSyncId:
          orderType.value == 2 ? selectedReservationSyncId.value : null,
    );
    currentOrder.value = order;
    await loadLookups();
    _toast('${'restaurant_order_started'.tr()}: ${order.orderNumber}');
  }

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
    try {
      await AppServices.restaurant.payOrder(
        currentOrder.value!.syncId,
        cartTotal,
        paymentMethod: orderType.value == 2 ? 2 : 0,
      );
      currentOrder.value = null;
      cart.clear();
      cart.refresh();
      selectedTableSyncId.value = '';
      selectedReservationSyncId.value = '';
      await loadLookups();
      await loadProfit();
      _toast('restaurant_payment_done'.tr());
    } catch (e) {
      _toast('$e');
    }
  }

  void _toast(String message) {
    final ctx = Get.context;
    if (ctx != null) {
      ScaffoldMessenger.of(ctx).showSnackBar(SnackBar(content: Text(message)));
    }
  }

  @override
  void onClose() {
    tabController.dispose();
    super.onClose();
  }
}
