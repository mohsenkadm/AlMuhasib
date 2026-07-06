import 'package:easy_localization/easy_localization.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../shared/models/dashboard_models.dart';

class DashboardController extends GetxController {
  final isLoading = true.obs;
  final Rxn<Object> error = Rxn<Object>();
  final Rxn<DashboardData> data = Rxn<DashboardData>();

  String get companyName =>
      AppServices.prefs.companyName ?? 'app_name'.tr();

  @override
  void onInit() {
    super.onInit();
    reload();
  }

  Future<void> reload() async {
    isLoading.value = true;
    error.value = null;
    try {
      data.value = await AppServices.dashboard.getDashboard();
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}
