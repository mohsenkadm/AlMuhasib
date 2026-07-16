import 'package:get/get.dart';

import '../../features/data_tab/controllers/data_list_controller.dart';
import '../../features/data_tab/controllers/invoice_detail_controller.dart';
import '../../features/data_tab/controllers/pricing_type_form_controller.dart';
import '../../features/data_tab/controllers/pricing_types_controller.dart';
import '../../features/data_tab/controllers/product_price_form_controller.dart';
import '../../features/data_tab/controllers/product_prices_controller.dart';
import '../../features/operations/controllers/customer_form_controller.dart';
import '../../features/operations/controllers/entity_form_controllers.dart';
import '../../features/operations/controllers/invoice_wizard_controller.dart';
import '../../features/reports/controllers/report_detail_controller.dart';

class DataListBinding extends Bindings {
  @override
  void dependencies() {
    final type = Get.parameters['type']!;
    Get.lazyPut(
      () => DataListController(listType: type),
      tag: 'data_list_$type',
    );
  }
}

class ReportDetailBinding extends Bindings {
  @override
  void dependencies() {
    final type = Get.parameters['type']!;
    Get.lazyPut(
      () => ReportDetailController(reportType: type),
      tag: 'report_$type',
    );
  }
}

class InvoiceWizardBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(() => InvoiceWizardController());
  }
}

class CustomerFormBinding extends Bindings {
  @override
  void dependencies() {
    final syncId = Get.parameters['syncId'];
    Get.lazyPut(() => CustomerFormController(syncId: syncId));
  }
}

class SupplierFormBinding extends Bindings {
  @override
  void dependencies() {
    final syncId = Get.parameters['syncId'];
    Get.lazyPut(() => SupplierFormController(syncId: syncId));
  }
}

class ProductFormBinding extends Bindings {
  @override
  void dependencies() {
    final syncId = Get.parameters['syncId'];
    Get.lazyPut(() => ProductFormController(syncId: syncId));
  }
}

class InvestorFormBinding extends Bindings {
  @override
  void dependencies() {
    final syncId = Get.parameters['syncId'];
    Get.lazyPut(() => InvestorFormController(syncId: syncId));
  }
}

class InvoiceDetailBinding extends Bindings {
  @override
  void dependencies() {
    final syncId = Get.parameters['syncId']!;
    Get.lazyPut(
      () => InvoiceDetailController(syncId: syncId),
      tag: 'invoice_$syncId',
    );
  }
}

class PricingTypesBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => PricingTypesController(),
      tag: 'pricing_types',
    );
  }
}

class PricingTypeFormBinding extends Bindings {
  @override
  void dependencies() {
    final syncId = Get.parameters['syncId'];
    Get.lazyPut(() => PricingTypeFormController(syncId: syncId));
  }
}

class ProductPricesBinding extends Bindings {
  @override
  void dependencies() {
    final productSyncId = Get.parameters['productSyncId'] ??
        (Get.arguments is String ? Get.arguments as String : null);
    Get.lazyPut(
      () => ProductPricesController(productSyncId: productSyncId),
      tag: 'product_prices',
    );
  }
}

class ProductPriceFormBinding extends Bindings {
  @override
  void dependencies() {
    final syncId = Get.parameters['syncId'];
    final prefill = Get.arguments is String ? Get.arguments as String : null;
    Get.lazyPut(
      () => ProductPriceFormController(
        syncId: syncId,
        prefillProductSyncId: prefill,
      ),
    );
  }
}
