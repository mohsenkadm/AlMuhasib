import '../../../core/network/api_client.dart';
import '../models/hotel_models.dart';

class HotelRepository {
  HotelRepository(this._apiClient);

  final ApiClient _apiClient;

  Future<HotelDashboardData> getDashboard() {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/hotel/dashboard',
      parser: (data) =>
          HotelDashboardData.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<HotelOccupancySummary> getOccupancy() {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/hotel/occupancy',
      parser: (data) =>
          HotelOccupancySummary.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<List<HotelReservation>> getTodayReservations() {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/hotel/reservations/today',
      parser: (data) => (data as List<dynamic>? ?? [])
          .map((e) => HotelReservation.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<HotelRoom>> getRooms() {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/hotel/rooms',
      parser: (data) => (data as List<dynamic>? ?? [])
          .map((e) => HotelRoom.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<HotelGuestPage> getGuests({
    int page = 1,
    int pageSize = 20,
    String search = '',
  }) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/hotel/guests',
      queryParameters: {
        'page': page,
        'pageSize': pageSize,
        if (search.isNotEmpty) 'search': search,
      },
      parser: (data) => HotelGuestPage.fromJson(
        data as Map<String, dynamic>,
        HotelGuest.fromJson,
      ),
    );
  }

  Future<HotelGuest> createGuest(HotelGuestUpsertRequest request) {
    _apiClient.updateBaseUrl();
    return _apiClient.post(
      '/api/hotel/guests',
      data: request.toJson(),
      parser: (data) => HotelGuest.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<void> updateGuest(String syncId, HotelGuestUpsertRequest request) {
    _apiClient.updateBaseUrl();
    return _apiClient.put<void>(
      '/api/hotel/guests/$syncId',
      data: request.toJson(),
      parser: (_) {},
    );
  }

  Future<HotelReservationPage> getReservations({
    int page = 1,
    int pageSize = 20,
    String search = '',
  }) {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/hotel/reservations',
      queryParameters: {
        'page': page,
        'pageSize': pageSize,
        if (search.isNotEmpty) 'search': search,
      },
      parser: (data) => HotelReservationPage.fromJson(
        data as Map<String, dynamic>,
        HotelReservation.fromJson,
      ),
    );
  }

  Future<void> checkIn(HotelCheckInRequest request) {
    _apiClient.updateBaseUrl();
    return _apiClient.postVoid(
      '/api/hotel/operations/check-in',
      data: request.toJson(),
    );
  }

  Future<void> checkOut(HotelCheckOutRequest request) {
    _apiClient.updateBaseUrl();
    return _apiClient.postVoid(
      '/api/hotel/operations/check-out',
      data: request.toJson(),
    );
  }

  Future<void> recordPayment(HotelPaymentRequest request) {
    _apiClient.updateBaseUrl();
    return _apiClient.postVoid(
      '/api/hotel/operations/payment',
      data: request.toJson(),
    );
  }

  Future<List<HotelFloor>> getFloors() {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/hotel/master-data/floors',
      parser: (data) => (data as List<dynamic>? ?? [])
          .map((e) => HotelFloor.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<HotelRoomType>> getRoomTypes() {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/hotel/master-data/room-types',
      parser: (data) => (data as List<dynamic>? ?? [])
          .map((e) => HotelRoomType.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<List<HotelRatePlan>> getRatePlans() {
    _apiClient.updateBaseUrl();
    return _apiClient.get(
      '/api/hotel/master-data/rate-plans',
      parser: (data) => (data as List<dynamic>? ?? [])
          .map((e) => HotelRatePlan.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
