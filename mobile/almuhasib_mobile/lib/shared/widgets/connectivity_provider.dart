import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:get/get.dart';

import '../../core/getx/app_services.dart';

/// Reactive offline flag backed by [ConnectivityController].
bool get isOffline => AppServices.connectivity.isOffline.value;

/// Widget helper — pass [isOffline] from [Obx] watching connectivity.
class ConnectivityState {
  ConnectivityState._();

  static bool get offline => AppServices.connectivity.isOffline.value;

  static void watch() => AppServices.connectivity.isOffline.value;
}
