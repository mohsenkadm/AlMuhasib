import 'package:get/get.dart';

import '../../features/car/controllers/car_contract_detail_controller.dart';
import '../../features/car/controllers/car_contract_form_controller.dart';
import '../../features/car/controllers/car_contracts_controller.dart';
import '../../features/car/controllers/car_dashboard_controller.dart';
import '../../features/car/controllers/car_payments_controller.dart';
import '../../features/car/controllers/car_report_controller.dart';
import '../../features/car/controllers/car_shell_controller.dart';
import '../../features/settings/settings_controller.dart';

class CarShellBinding extends Bindings {
  @override
  void dependencies() {
    if (!Get.isRegistered<CarShellController>()) {
      Get.lazyPut<CarShellController>(() => CarShellController(), fenix: true);
    }
    if (!Get.isRegistered<CarDashboardController>(tag: 'car_dashboard')) {
      Get.lazyPut(
        () => CarDashboardController(),
        tag: 'car_dashboard',
        fenix: true,
      );
    }
    if (!Get.isRegistered<CarContractsController>(tag: 'car_contracts')) {
      Get.lazyPut(
        () => CarContractsController(),
        tag: 'car_contracts',
        fenix: true,
      );
    }
    if (!Get.isRegistered<CarPaymentsController>(tag: 'car_payments')) {
      Get.lazyPut(
        () => CarPaymentsController(),
        tag: 'car_payments',
        fenix: true,
      );
    }
    if (!Get.isRegistered<CarReportController>(tag: 'car_report')) {
      Get.lazyPut(
        () => CarReportController(),
        tag: 'car_report',
        fenix: true,
      );
    }
    if (!Get.isRegistered<SettingsController>()) {
      Get.lazyPut<SettingsController>(() => SettingsController(), fenix: true);
    }
  }
}

class CarContractFormBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => CarContractFormController(),
      tag: 'car_contract_form',
    );
  }
}

class CarContractDetailBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => CarContractDetailController(
        syncId: Get.parameters['syncId']!,
      ),
      tag: 'car_contract_detail',
    );
  }
}
