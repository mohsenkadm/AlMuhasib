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
  final channels = Rx<List<RestaurantChannelSales>>([]);
  final topItems = Rx<List<RestaurantTopItem>>([]);
  final overview = Rxn<RestaurantFinancialOverview>();

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
      final from = DateTime.now().subtract(const Duration(days: 30));
      final to = DateTime.now();
      final results = await Future.wait([
        AppServices.restaurant.getProfitSummary(from: from, to: to),
        AppServices.restaurant.getChannelSales(from: from, to: to),
        AppServices.restaurant.getTopItems(from: from, to: to),
        AppServices.restaurant.getOverview(from: from, to: to),
      ]);
      profit.value = results[0] as RestaurantProfitSummary;
      channels.value = results[1] as List<RestaurantChannelSales>;
      topItems.value = results[2] as List<RestaurantTopItem>;
      overview.value = results[3] as RestaurantFinancialOverview;
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
