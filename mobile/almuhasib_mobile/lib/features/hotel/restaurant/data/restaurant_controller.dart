import 'package:get/get.dart';

import '../../../../core/getx/app_services.dart';
import '../models/restaurant_models.dart';

class RestaurantController extends GetxController {
  final isMenuLoading = true.obs;
  final menuError = Rxn<Object>();
  final menu = Rxn<RestaurantMenuData>();

  final isProfitLoading = true.obs;
  final profitError = Rxn<Object>();
  final profit = Rxn<RestaurantProfitSummary>();

  final isAlertsLoading = true.obs;
  final alertsError = Rxn<Object>();
  final alerts = Rx<List<RestaurantStockAlert>>([]);

  @override
  void onInit() {
    super.onInit();
    loadMenu();
    loadProfit();
    loadStockAlerts();
  }

  Future<void> loadMenu() async {
    isMenuLoading.value = true;
    menuError.value = null;
    try {
      menu.value = await AppServices.restaurant.getMenu();
    } catch (e) {
      menuError.value = e;
    } finally {
      isMenuLoading.value = false;
    }
  }

  Future<void> loadProfit() async {
    isProfitLoading.value = true;
    profitError.value = null;
    try {
      profit.value = await AppServices.restaurant.getProfitSummary(
        from: DateTime.now().subtract(const Duration(days: 30)),
        to: DateTime.now(),
      );
    } catch (e) {
      profitError.value = e;
    } finally {
      isProfitLoading.value = false;
    }
  }

  Future<void> loadStockAlerts() async {
    isAlertsLoading.value = true;
    alertsError.value = null;
    try {
      alerts.value = await AppServices.restaurant.getStockAlerts();
    } catch (e) {
      alertsError.value = e;
    } finally {
      isAlertsLoading.value = false;
    }
  }
}
