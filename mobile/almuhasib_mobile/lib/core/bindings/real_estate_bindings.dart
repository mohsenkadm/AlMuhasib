import 'package:get/get.dart';

import '../../features/real_estate/controllers/real_estate_contract_detail_controller.dart';
import '../../features/real_estate/controllers/real_estate_contract_form_controller.dart';
import '../../features/real_estate/controllers/real_estate_contracts_controller.dart';
import '../../features/real_estate/controllers/real_estate_dashboard_controller.dart';
import '../../features/real_estate/controllers/real_estate_payments_controller.dart';
import '../../features/real_estate/controllers/real_estate_report_controller.dart';
import '../../features/real_estate/controllers/real_estate_shell_controller.dart';
import '../../features/settings/settings_controller.dart';

class RealEstateShellBinding extends Bindings {
  @override
  void dependencies() {
    if (!Get.isRegistered<RealEstateShellController>()) {
      Get.lazyPut<RealEstateShellController>(
        () => RealEstateShellController(),
        fenix: true,
      );
    }
    if (!Get.isRegistered<RealEstateDashboardController>(
      tag: 'real_estate_dashboard',
    )) {
      Get.lazyPut(
        () => RealEstateDashboardController(),
        tag: 'real_estate_dashboard',
        fenix: true,
      );
    }
    if (!Get.isRegistered<RealEstateContractsController>(
      tag: 'real_estate_contracts',
    )) {
      Get.lazyPut(
        () => RealEstateContractsController(),
        tag: 'real_estate_contracts',
        fenix: true,
      );
    }
    if (!Get.isRegistered<RealEstatePaymentsController>(
      tag: 'real_estate_payments',
    )) {
      Get.lazyPut(
        () => RealEstatePaymentsController(),
        tag: 'real_estate_payments',
        fenix: true,
      );
    }
    if (!Get.isRegistered<RealEstateReportController>(
      tag: 'real_estate_report',
    )) {
      Get.lazyPut(
        () => RealEstateReportController(),
        tag: 'real_estate_report',
        fenix: true,
      );
    }
    if (!Get.isRegistered<SettingsController>()) {
      Get.lazyPut<SettingsController>(() => SettingsController(), fenix: true);
    }
  }
}

class RealEstateContractFormBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => RealEstateContractFormController(),
      tag: 'real_estate_contract_form',
    );
  }
}

class RealEstateContractDetailBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => RealEstateContractDetailController(
        syncId: Get.parameters['syncId']!,
      ),
      tag: 'real_estate_contract_detail',
    );
  }
}
