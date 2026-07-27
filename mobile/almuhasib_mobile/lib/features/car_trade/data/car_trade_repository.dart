import '../../../core/network/api_client.dart';
import '../models/car_trade_models.dart';

class CarTradeRepository {
  CarTradeRepository(this._api);

  final ApiClient _api;

  Future<CarTradeDashboardDto> getDashboard() {
    return _api.get(
      '/api/car-trade/dashboard',
      parser: (data) =>
          CarTradeDashboardDto.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<List<CarTradeTransactionListItem>> getTransactions({
    String? search,
    String? status,
    String? tradeType,
    DateTime? from,
    DateTime? to,
    bool? hasRemaining,
    int page = 1,
    int pageSize = 50,
  }) {
    return _api.get(
      '/api/car-trade/transactions',
      queryParameters: {
        if (search != null && search.isNotEmpty) 'search': search,
        if (status != null && status.isNotEmpty) 'status': status,
        if (tradeType != null && tradeType.isNotEmpty) 'tradeType': tradeType,
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
        if (hasRemaining == true) 'hasRemaining': true,
        'page': page,
        'pageSize': pageSize,
      },
      parser: (data) => (data as List<dynamic>)
          .map(
            (e) => CarTradeTransactionListItem.fromJson(
              e as Map<String, dynamic>,
            ),
          )
          .toList(),
    );
  }

  Future<CarTradeTransactionDetail> getTransaction(String syncId) {
    return _api.get(
      '/api/car-trade/transactions/$syncId',
      parser: (data) =>
          CarTradeTransactionDetail.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<String> createTransaction(CreateCarTradeTransactionRequest request) {
    return _api.post(
      '/api/car-trade/transactions',
      data: request.toJson(),
      parser: (data) => (data as Map<String, dynamic>)['syncId'] as String,
    );
  }

  Future<void> recordPayment({
    required String transactionSyncId,
    required double amount,
    required DateTime paymentDate,
    String? notes,
  }) {
    return _api.postVoid(
      '/api/car-trade/transactions/$transactionSyncId/payments',
      data: {
        'amount': amount,
        'paymentDate': paymentDate.toIso8601String(),
        if (notes != null) 'notes': notes,
      },
    );
  }

  Future<CarTradeReportDto> getReport({
    DateTime? from,
    DateTime? to,
    String? status,
    String? tradeType,
  }) {
    return _api.get(
      '/api/car-trade/reports/transactions',
      queryParameters: {
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
        if (status != null) 'status': status,
        if (tradeType != null) 'tradeType': tradeType,
      },
      parser: (data) =>
          CarTradeReportDto.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<CarTradePartyStatementDto> getPartyStatement({
    required String partyName,
    String? partyPhone,
    DateTime? from,
    DateTime? to,
  }) {
    return _api.get(
      '/api/car-trade/reports/party-statement',
      queryParameters: {
        'partyName': partyName,
        if (partyPhone != null && partyPhone.isNotEmpty) 'partyPhone': partyPhone,
        if (from != null) 'from': from.toIso8601String(),
        if (to != null) 'to': to.toIso8601String(),
      },
      parser: (data) =>
          CarTradePartyStatementDto.fromJson(data as Map<String, dynamic>),
    );
  }
}
