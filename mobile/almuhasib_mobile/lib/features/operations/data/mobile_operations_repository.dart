import '../../../core/network/api_client.dart';
import '../../../core/offline/offline_write_queue.dart';
import '../../../shared/models/mobile_models.dart';
import 'package:get/get.dart';
import 'package:uuid/uuid.dart';

class MobileOperationsRepository {
  MobileOperationsRepository(this._apiClient);

  final ApiClient _apiClient;
  final _uuid = const Uuid();

  OfflineWriteService get _queue => Get.find<OfflineWriteService>();

  Future<MobileWriteResponse> _postOrQueue({
    required String operationType,
    required String path,
    required Map<String, dynamic> body,
    String? syncId,
  }) async {
    final clientSyncId = syncId ?? (body['syncId'] as String?) ?? _uuid.v4();
    final payload = Map<String, dynamic>.from(body);
    payload['syncId'] = clientSyncId;

    if (!_queue.isOnline) {
      return _queue.enqueue(
        operationType: operationType,
        path: path,
        body: payload,
        clientSyncId: clientSyncId,
      );
    }

    _apiClient.updateBaseUrl();
    try {
      return await _apiClient.post(
        path,
        data: payload,
        parser: (data) =>
            MobileWriteResponse.fromJson(data as Map<String, dynamic>),
      );
    } catch (e) {
      if (_queue.isNetworkError(e)) {
        return _queue.enqueue(
          operationType: operationType,
          path: path,
          body: payload,
          clientSyncId: clientSyncId,
        );
      }
      rethrow;
    }
  }

  Future<MobileWriteResponse> createCustomer(CreateCustomerRequest request) {
    return _postOrQueue(
      operationType: 'customer',
      path: '/api/mobile/customers',
      body: request.toJson(),
      syncId: request.syncId,
    );
  }

  Future<MobileWriteResponse> createSupplier(CreateSupplierRequest request) {
    return _postOrQueue(
      operationType: 'supplier',
      path: '/api/mobile/suppliers',
      body: request.toJson(),
      syncId: request.syncId,
    );
  }

  Future<MobileWriteResponse> createProduct(CreateProductRequest request) {
    return _postOrQueue(
      operationType: 'product',
      path: '/api/mobile/products',
      body: request.toJson(),
      syncId: request.syncId,
    );
  }

  Future<MobileWriteResponse> createInvestor(CreateInvestorRequest request) {
    return _postOrQueue(
      operationType: 'investor',
      path: '/api/mobile/investors',
      body: request.toJson(),
      syncId: request.syncId,
    );
  }

  Future<MobileWriteResponse> createInvoice(CreateInvoiceRequest request) {
    return _postOrQueue(
      operationType: 'invoice',
      path: '/api/mobile/invoices',
      body: request.toJson(),
    );
  }

  Future<MobileWriteResponse> upsertPricingType(UpsertPricingTypeRequest request) async {
    final syncId = request.syncId;
    if (syncId != null && syncId.isNotEmpty) {
      _apiClient.updateBaseUrl();
      if (!_queue.isOnline) {
        return _queue.enqueue(
          operationType: 'pricing_type',
          path: '/api/mobile/pricing-types/$syncId',
          method: 'PUT',
          body: request.toJson(),
          clientSyncId: syncId,
        );
      }
      try {
        return await _apiClient.put(
          '/api/mobile/pricing-types/$syncId',
          data: request.toJson(),
          parser: (data) =>
              MobileWriteResponse.fromJson(data as Map<String, dynamic>),
        );
      } catch (e) {
        if (_queue.isNetworkError(e)) {
          return _queue.enqueue(
            operationType: 'pricing_type',
            path: '/api/mobile/pricing-types/$syncId',
            method: 'PUT',
            body: request.toJson(),
            clientSyncId: syncId,
          );
        }
        rethrow;
      }
    }
    return _postOrQueue(
      operationType: 'pricing_type',
      path: '/api/mobile/pricing-types',
      body: request.toJson(),
      syncId: request.syncId,
    );
  }

  Future<MobileWriteResponse> deletePricingType(String syncId) {
    _apiClient.updateBaseUrl();
    return _apiClient.delete(
      '/api/mobile/pricing-types/$syncId',
      parser: (data) =>
          MobileWriteResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<MobileWriteResponse> upsertProductPrice(UpsertProductPriceRequest request) async {
    final syncId = request.syncId;
    if (syncId != null && syncId.isNotEmpty) {
      _apiClient.updateBaseUrl();
      if (!_queue.isOnline) {
        return _queue.enqueue(
          operationType: 'product_price',
          path: '/api/mobile/product-prices/$syncId',
          method: 'PUT',
          body: request.toJson(),
          clientSyncId: syncId,
        );
      }
      try {
        return await _apiClient.put(
          '/api/mobile/product-prices/$syncId',
          data: request.toJson(),
          parser: (data) =>
              MobileWriteResponse.fromJson(data as Map<String, dynamic>),
        );
      } catch (e) {
        if (_queue.isNetworkError(e)) {
          return _queue.enqueue(
            operationType: 'product_price',
            path: '/api/mobile/product-prices/$syncId',
            method: 'PUT',
            body: request.toJson(),
            clientSyncId: syncId,
          );
        }
        rethrow;
      }
    }
    return _postOrQueue(
      operationType: 'product_price',
      path: '/api/mobile/product-prices',
      body: request.toJson(),
      syncId: request.syncId,
    );
  }

  Future<MobileWriteResponse> deleteProductPrice(String syncId) {
    _apiClient.updateBaseUrl();
    return _apiClient.delete(
      '/api/mobile/product-prices/$syncId',
      parser: (data) =>
          MobileWriteResponse.fromJson(data as Map<String, dynamic>),
    );
  }

  Future<MobileWriteResponse> upsertCashBox(UpsertCashBoxRequest request) {
    return _postOrQueue(
      operationType: 'cash_box',
      path: '/api/mobile/cash-boxes',
      body: request.toJson(),
      syncId: request.syncId,
    );
  }

  Future<MobileWriteResponse> upsertBankAccount(UpsertBankAccountRequest request) {
    return _postOrQueue(
      operationType: 'bank_account',
      path: '/api/mobile/bank-accounts',
      body: request.toJson(),
      syncId: request.syncId,
    );
  }

  Future<MobileWriteResponse> upsertExpenseType(UpsertExpenseTypeRequest request) {
    return _postOrQueue(
      operationType: 'expense_type',
      path: '/api/mobile/expense-types',
      body: request.toJson(),
      syncId: request.syncId,
    );
  }

  Future<MobileWriteResponse> createVoucher(CreateVoucherRequest request) {
    return _postOrQueue(
      operationType: 'voucher',
      path: '/api/mobile/vouchers',
      body: request.toJson(),
      syncId: request.syncId,
    );
  }

  Future<MobileWriteResponse> createExpense(CreateExpenseRequest request) {
    return _postOrQueue(
      operationType: 'expense',
      path: '/api/mobile/expenses',
      body: request.toJson(),
      syncId: request.syncId,
    );
  }

  Future<MobileWriteResponse> createTransfer(CreateTransferRequest request) {
    return _postOrQueue(
      operationType: 'transfer',
      path: '/api/mobile/transfers',
      body: request.toJson(),
      syncId: request.syncId,
    );
  }

  Future<MobileWriteResponse> upsertWarehouse(UpsertWarehouseRequest request) {
    return _postOrQueue(
      operationType: 'warehouse',
      path: '/api/mobile/warehouses',
      body: request.toJson(),
      syncId: request.syncId,
    );
  }

  Future<MobileWriteResponse> createWarehouseTransfer(
    CreateWarehouseTransferRequest request,
  ) {
    return _postOrQueue(
      operationType: 'warehouse_transfer',
      path: '/api/mobile/warehouse-transfers',
      body: request.toJson(),
      syncId: request.syncId,
    );
  }

  Future<MobileWriteResponse> createStockAdjustment(
    CreateStockAdjustmentRequest request,
  ) {
    return _postOrQueue(
      operationType: 'stock_adjustment',
      path: '/api/mobile/stock-adjustments',
      body: request.toJson(),
    );
  }

  Future<MobileWriteResponse> payInstallment(
    String syncId,
    PayInstallmentRequest request,
  ) {
    return _postOrQueue(
      operationType: 'installment_pay',
      path: '/api/mobile/installments/$syncId/pay',
      body: request.toJson(),
    );
  }
}
