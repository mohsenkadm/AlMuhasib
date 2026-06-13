import '../../../core/network/api_client.dart';
import '../../../shared/models/dashboard_models.dart';

class DashboardRepository {
  DashboardRepository(this._apiClient);

  final ApiClient _apiClient;

  Future<DashboardData> getDashboard() {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/dashboard',
      parser: (data) => DashboardData.fromJson(data as Map<String, dynamic>),
    );
  }
}
