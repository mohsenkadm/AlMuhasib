import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/gold_shop_models.dart';

class GoldSaleDetailController extends GetxController {
  GoldSaleDetailController({required this.invoiceId});

  final int invoiceId;
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final invoice = Rxn<GoldInvoiceDetail>();

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    final hasData = invoice.value != null;
    if (!hasData) isLoading.value = true;
    error.value = null;
    try {
      invoice.value = await AppServices.goldShop.getInvoice(invoiceId);
    } catch (e) {
      if (!hasData) error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
