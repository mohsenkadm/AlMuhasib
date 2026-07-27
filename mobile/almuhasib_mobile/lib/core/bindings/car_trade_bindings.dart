import 'package:get/get.dart';

import '../../features/car_trade/controllers/car_trade_dashboard_controller.dart';
import '../../features/car_trade/controllers/car_trade_payments_controller.dart';
import '../../features/car_trade/controllers/car_trade_party_statement_controller.dart';
import '../../features/car_trade/controllers/car_trade_report_controller.dart';
import '../../features/car_trade/controllers/car_trade_shell_controller.dart';
import '../../features/car_trade/controllers/car_trade_transaction_detail_controller.dart';
import '../../features/car_trade/controllers/car_trade_transaction_form_controller.dart';
import '../../features/car_trade/controllers/car_trade_transactions_controller.dart';
import '../../features/settings/settings_controller.dart';

class CarTradeShellBinding extends Bindings {
  @override
  void dependencies() {
    if (!Get.isRegistered<CarTradeShellController>()) {
      Get.lazyPut<CarTradeShellController>(
        () => CarTradeShellController(),
        fenix: true,
      );
    }
    if (!Get.isRegistered<CarTradeDashboardController>(tag: 'car_trade_dashboard')) {
      Get.lazyPut(
        () => CarTradeDashboardController(),
        tag: 'car_trade_dashboard',
        fenix: true,
      );
    }
    if (!Get.isRegistered<CarTradeTransactionsController>(
      tag: 'car_trade_transactions',
    )) {
      Get.lazyPut(
        () => CarTradeTransactionsController(),
        tag: 'car_trade_transactions',
        fenix: true,
      );
    }
    if (!Get.isRegistered<CarTradePaymentsController>(tag: 'car_trade_payments')) {
      Get.lazyPut(
        () => CarTradePaymentsController(),
        tag: 'car_trade_payments',
        fenix: true,
      );
    }
    if (!Get.isRegistered<CarTradeReportController>(tag: 'car_trade_report')) {
      Get.lazyPut(
        () => CarTradeReportController(),
        tag: 'car_trade_report',
        fenix: true,
      );
    }
    if (!Get.isRegistered<SettingsController>()) {
      Get.lazyPut<SettingsController>(() => SettingsController(), fenix: true);
    }
  }
}

class CarTradeTransactionFormBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => CarTradeTransactionFormController(),
      tag: 'car_trade_transaction_form',
    );
  }
}

class CarTradeTransactionDetailBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => CarTradeTransactionDetailController(
        syncId: Get.parameters['syncId']!,
      ),
      tag: 'car_trade_transaction_detail',
    );
  }
}

class CarTradePartyStatementBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => CarTradePartyStatementController(),
      tag: 'car_trade_party_statement',
    );
  }
}
