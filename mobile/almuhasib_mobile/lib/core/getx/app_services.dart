import 'package:get/get.dart';

import '../network/api_client.dart';
import '../services/app_info_service.dart';
import '../services/notification_service.dart';
import '../storage/preferences_service.dart';
import '../storage/secure_storage_service.dart';
import '../../features/auth/data/auth_repository.dart';
import '../../features/car/data/car_repository.dart';
import '../../features/dashboard/data/dashboard_repository.dart';
import '../../features/data_tab/data/data_repository.dart';
import '../../features/hotel/data/hotel_repository.dart';
import '../../features/hotel/restaurant/data/restaurant_repository.dart';
import '../../features/operations/data/mobile_operations_repository.dart';
import '../../features/reports/data/reports_repository.dart';
import 'controllers/auth_controller.dart';
import 'controllers/connectivity_controller.dart';
import 'controllers/theme_controller.dart';

/// Central GetX service registration and shortcuts.
class AppServices {
  AppServices._();

  static Future<void> init(PreferencesService prefs) async {
    Get.put<PreferencesService>(prefs, permanent: true);

    final secureStorage = SecureStorageService();
    Get.put<SecureStorageService>(secureStorage, permanent: true);

    Get.put<ConnectivityController>(ConnectivityController(), permanent: true);

    final apiClient = ApiClient(
      secureStorage: secureStorage,
      baseUrlResolver: () => prefs.apiBaseUrl,
    );
    apiClient.updateBaseUrl();
    Get.put<ApiClient>(apiClient, permanent: true);

    final notificationService = NotificationService(apiClient);
    Get.put<NotificationService>(notificationService, permanent: true);

    final authRepository = AuthRepository(
      apiClient: apiClient,
      secureStorage: secureStorage,
      preferences: prefs,
      notificationService: notificationService,
    );
    Get.put<AuthRepository>(authRepository, permanent: true);

    Get.put<AuthController>(
      AuthController(repository: authRepository, preferences: prefs),
      permanent: true,
    );

    Get.put<ThemeController>(ThemeController(prefs), permanent: true);

    Get.put<DashboardRepository>(
      DashboardRepository(apiClient),
      permanent: true,
    );
    Get.put<ReportsRepository>(ReportsRepository(apiClient), permanent: true);
    Get.put<DataRepository>(DataRepository(apiClient), permanent: true);
    Get.put<MobileOperationsRepository>(
      MobileOperationsRepository(apiClient),
      permanent: true,
    );
    Get.put<HotelRepository>(HotelRepository(apiClient), permanent: true);
    Get.put<CarRepository>(CarRepository(apiClient), permanent: true);
    Get.put<RestaurantRepository>(
      RestaurantRepository(apiClient),
      permanent: true,
    );

    Get.put<AppInfoService>(AppInfoService(), permanent: true);
  }

  static PreferencesService get prefs => Get.find<PreferencesService>();
  static AuthController get auth => Get.find<AuthController>();
  static AuthRepository get authRepository => Get.find<AuthRepository>();
  static ThemeController get theme => Get.find<ThemeController>();
  static ConnectivityController get connectivity =>
      Get.find<ConnectivityController>();
  static ApiClient get api => Get.find<ApiClient>();
  static NotificationService get notifications =>
      Get.find<NotificationService>();
  static DashboardRepository get dashboard => Get.find<DashboardRepository>();
  static ReportsRepository get reports => Get.find<ReportsRepository>();
  static DataRepository get data => Get.find<DataRepository>();
  static MobileOperationsRepository get operations =>
      Get.find<MobileOperationsRepository>();
  static HotelRepository get hotel => Get.find<HotelRepository>();
  static CarRepository get car => Get.find<CarRepository>();
  static RestaurantRepository get restaurant => Get.find<RestaurantRepository>();
  static AppInfoService get appInfo => Get.find<AppInfoService>();
}
