import 'package:get/get.dart';

import '../../features/gold_shop/controllers/gold_create_sale_controller.dart';
import '../../features/gold_shop/controllers/gold_customers_controller.dart';
import '../../features/gold_shop/controllers/gold_dashboard_controller.dart';
import '../../features/gold_shop/controllers/gold_notifications_controller.dart';
import '../../features/gold_shop/controllers/gold_prices_controller.dart';
import '../../features/gold_shop/controllers/gold_sale_detail_controller.dart';
import '../../features/gold_shop/controllers/gold_sales_controller.dart';
import '../../features/gold_shop/controllers/gold_shell_controller.dart';
import '../../features/settings/settings_controller.dart';

class GoldShopShellBinding extends Bindings {
  @override
  void dependencies() {
    if (!Get.isRegistered<GoldShellController>()) {
      Get.lazyPut<GoldShellController>(() => GoldShellController(), fenix: true);
    }
    if (!Get.isRegistered<GoldDashboardController>(tag: 'gold_dashboard')) {
      Get.lazyPut(
        () => GoldDashboardController(),
        tag: 'gold_dashboard',
        fenix: true,
      );
    }
    if (!Get.isRegistered<GoldSalesController>(tag: 'gold_sales')) {
      Get.lazyPut(
        () => GoldSalesController(),
        tag: 'gold_sales',
        fenix: true,
      );
    }
    if (!Get.isRegistered<GoldCustomersController>(tag: 'gold_customers')) {
      Get.lazyPut(
        () => GoldCustomersController(),
        tag: 'gold_customers',
        fenix: true,
      );
    }
    if (!Get.isRegistered<SettingsController>()) {
      Get.lazyPut<SettingsController>(() => SettingsController(), fenix: true);
    }
  }
}

class GoldSaleDetailBinding extends Bindings {
  @override
  void dependencies() {
    final id = int.tryParse(Get.parameters['id'] ?? '') ?? 0;
    Get.lazyPut(
      () => GoldSaleDetailController(invoiceId: id),
      tag: 'gold_sale_detail',
    );
  }
}

class GoldPricesBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => GoldPricesController(),
      tag: 'gold_prices',
    );
  }
}

class GoldNotificationsBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => GoldNotificationsController(),
      tag: 'gold_notifications',
    );
  }
}

class GoldCreateSaleBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => GoldCreateSaleController(),
      tag: 'gold_create_sale',
    );
  }
}
