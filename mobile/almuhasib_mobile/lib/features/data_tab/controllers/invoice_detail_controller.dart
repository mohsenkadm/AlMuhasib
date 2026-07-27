import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../../../shared/models/master_data_models.dart';

class InvoiceDetailController extends GetxController {
  InvoiceDetailController({required this.syncId});

  final String syncId;

  final isLoading = true.obs;
  final Rxn<Object> error = Rxn<Object>();
  final Rxn<InvoiceDetailResponse> invoice = Rxn<InvoiceDetailResponse>();

  @override
  void onInit() {
    super.onInit();
    reload();
  }

  Future<void> reload() async {
    isLoading.value = true;
    error.value = null;
    try {
      invoice.value = await AppServices.data.getInvoiceDetail(syncId);
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
