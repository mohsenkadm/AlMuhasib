import 'package:get/get.dart';

import '../../features/data_tab/controllers/data_list_controller.dart';
import '../../features/data_tab/controllers/invoice_detail_controller.dart';
import '../../features/data_tab/controllers/pricing_type_form_controller.dart';
import '../../features/data_tab/controllers/pricing_types_controller.dart';
import '../../features/data_tab/controllers/product_price_form_controller.dart';
import '../../features/data_tab/controllers/product_prices_controller.dart';
import '../../features/finance/presentation/finance_list_screen.dart';
import '../../features/installments/presentation/installments_screens.dart';
import '../../features/offline/presentation/pending_sync_screen.dart';
import '../../features/operations/controllers/customer_form_controller.dart';
import '../../features/operations/controllers/entity_form_controllers.dart';
import '../../features/operations/controllers/invoice_wizard_controller.dart';
import '../../features/operations/presentation/forms/finance/finance_entity_forms.dart';
import '../../features/operations/presentation/forms/finance/finance_transaction_forms.dart';
import '../../features/reports/controllers/report_detail_controller.dart';
import '../../shared/models/mobile_models.dart';

class DataListBinding extends Bindings {
  @override
  void dependencies() {
    final type = Get.parameters['type']!;
    Get.lazyPut(
      () => DataListController(listType: type),
      tag: 'data_list_$type',
      fenix: true,
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
    Get.lazyPut(() => InvoiceWizardController(), fenix: true);
  }
}

class CustomerFormBinding extends Bindings {
  @override
  void dependencies() {
    final syncId = Get.parameters['syncId'];
    Get.lazyPut(() => CustomerFormController(syncId: syncId), fenix: true);
  }
}

class SupplierFormBinding extends Bindings {
  @override
  void dependencies() {
    final syncId = Get.parameters['syncId'];
    Get.lazyPut(() => SupplierFormController(syncId: syncId), fenix: true);
  }
}

class ProductFormBinding extends Bindings {
  @override
  void dependencies() {
    final syncId = Get.parameters['syncId'];
    Get.lazyPut(() => ProductFormController(syncId: syncId), fenix: true);
  }
}

class InvestorFormBinding extends Bindings {
  @override
  void dependencies() {
    final syncId = Get.parameters['syncId'];
    Get.lazyPut(() => InvestorFormController(syncId: syncId), fenix: true);
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
    Get.lazyPut(
      () => PricingTypeFormController(syncId: syncId),
      fenix: true,
    );
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
      fenix: true,
    );
  }
}

class FinanceListBinding extends Bindings {
  @override
  void dependencies() {
    final type = Get.parameters['type']!;
    Get.lazyPut(
      () => FinanceListController(listType: type),
      tag: 'finance_list_$type',
      fenix: true,
    );
  }
}

class VoucherFormBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(() => VoucherFormController(), fenix: true);
  }
}

class ExpenseFormBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(() => ExpenseFormController(), fenix: true);
  }
}

class TransferFormBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(() => TransferFormController(), fenix: true);
  }
}

class CashBoxFormBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => CashBoxFormController(syncId: Get.parameters['syncId']),
      fenix: true,
    );
  }
}

class BankAccountFormBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => BankAccountFormController(syncId: Get.parameters['syncId']),
      fenix: true,
    );
  }
}

class ExpenseTypeFormBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => ExpenseTypeFormController(syncId: Get.parameters['syncId']),
      fenix: true,
    );
  }
}

class WarehouseFormBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(
      () => WarehouseFormController(syncId: Get.parameters['syncId']),
      fenix: true,
    );
  }
}

class WarehouseTransferFormBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(() => WarehouseTransferFormController(), fenix: true);
  }
}

class StockAdjustmentFormBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(() => StockAdjustmentFormController(), fenix: true);
  }
}

class InstallmentsBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(() => InstallmentsController(), fenix: true);
  }
}

class InstallmentPlanDetailBinding extends Bindings {
  @override
  void dependencies() {
    final syncId = Get.parameters['syncId']!;
    Get.lazyPut(
      () => InstallmentPlanDetailController(syncId: syncId),
      tag: 'plan_$syncId',
    );
  }
}

class InstallmentPayBinding extends Bindings {
  @override
  void dependencies() {
    final syncId = Get.parameters['syncId']!;
    final item = Get.arguments is InstallmentListItem
        ? Get.arguments as InstallmentListItem
        : null;
    Get.lazyPut(
      () => InstallmentPayController(syncId: syncId, item: item),
      fenix: true,
    );
  }
}

class PendingSyncBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut(() => PendingSyncController(), fenix: true);
  }
}

