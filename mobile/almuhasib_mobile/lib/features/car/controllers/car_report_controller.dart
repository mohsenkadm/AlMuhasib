import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../models/car_models.dart';

class CarReportController extends GetxController {
  final from = DateTime.now().subtract(const Duration(days: 30)).obs;
  final to = DateTime.now().obs;
  final isLoading = false.obs;
  final error = Rxn<Object>();
  final rows = <CarContractListItem>[].obs;

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      rows.value = await AppServices.car.getReport(
        from: from.value,
        to: to.value,
      );
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }

  void setFrom(DateTime date) {
    from.value = date;
    load();
  }

  void setTo(DateTime date) {
    to.value = date;
    load();
  }

  double get total => rows.fold<double>(0, (s, r) => s + r.carPrice);
}
