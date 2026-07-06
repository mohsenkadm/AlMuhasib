import 'package:get/get.dart';

import '../../features/auth/controllers/login_controller.dart';
import '../../features/onboarding/onboarding_controller.dart';

class AuthBinding extends Bindings {
  @override
  void dependencies() {
    Get.lazyPut<OnboardingController>(
      () => OnboardingController(),
      tag: 'onboarding',
    );
    Get.lazyPut<LoginController>(() => LoginController(), fenix: true);
  }
}
