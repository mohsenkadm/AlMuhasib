import 'dart:io';

import 'package:dio/dio.dart';
import 'package:dio/io.dart';
import 'package:flutter/foundation.dart';

import '../../shared/models/auth_models.dart';
import '../network/api_exception.dart';
import '../storage/secure_storage_service.dart';

typedef BaseUrlResolver = String Function();

class ApiClient {
  ApiClient({
    required SecureStorageService secureStorage,
    required BaseUrlResolver baseUrlResolver,
  })  : _secureStorage = secureStorage,
        _baseUrlResolver = baseUrlResolver {
    _dio = Dio(
      BaseOptions(
        connectTimeout: const Duration(seconds: 15),
        receiveTimeout: const Duration(seconds: 30),
        headers: {'Content-Type': 'application/json'},
      ),
    );
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: _onRequest,
        onError: _onError,
      ),
    );
    _configureDevHttps();
  }

  void _configureDevHttps() {
    if (!kDebugMode) return;
    final adapter = _dio.httpClientAdapter;
    if (adapter is! IOHttpClientAdapter) return;
    adapter.createHttpClient = () {
      final client = HttpClient();
      client.badCertificateCallback = (cert, host, port) {
        return host == '10.0.2.2' ||
            host == 'localhost' ||
            host == '127.0.0.1';
      };
      return client;
    };
  }

  late final Dio _dio;
  final SecureStorageService _secureStorage;
  final BaseUrlResolver _baseUrlResolver;
  bool _isRefreshing = false;

  Dio get dio => _dio;

  String get baseUrl => _baseUrlResolver();

  void updateBaseUrl() {
    _dio.options.baseUrl = baseUrl;
  }

  Future<void> _onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    options.baseUrl = baseUrl;
    if (!options.path.contains('/auth/login') &&
        !options.path.contains('/auth/refresh')) {
      final token = await _secureStorage.getAccessToken();
      if (token != null && token.isNotEmpty) {
        options.headers['Authorization'] = 'Bearer $token';
      }
    }
    handler.next(options);
  }

  Future<void> _onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    if (err.response?.statusCode == 401 &&
        !err.requestOptions.path.contains('/auth/')) {
      try {
        final refreshed = await _refreshToken();
        if (refreshed) {
          final token = await _secureStorage.getAccessToken();
          err.requestOptions.headers['Authorization'] = 'Bearer $token';
          final response = await _dio.fetch(err.requestOptions);
          return handler.resolve(response);
        }
      } catch (_) {
        // fall through
      }
    }
    handler.next(err);
  }

  Future<bool> _refreshToken() async {
    if (_isRefreshing) return false;
    _isRefreshing = true;
    try {
      final refreshToken = await _secureStorage.getRefreshToken();
      if (refreshToken == null || refreshToken.isEmpty) return false;

      final response = await _dio.post(
        '/api/auth/refresh',
        data: RefreshTokenRequest(refreshToken: refreshToken).toJson(),
      );
      final login = TenantLoginResponse.fromJson(
        response.data as Map<String, dynamic>,
      );
      await _secureStorage.saveTokens(
        accessToken: login.accessToken,
        refreshToken: login.refreshToken,
        expiresAt: login.accessTokenExpiresAt.toIso8601String(),
      );
      return true;
    } finally {
      _isRefreshing = false;
    }
  }

  Future<T> get<T>(
    String path, {
    Map<String, dynamic>? queryParameters,
    required T Function(dynamic data) parser,
  }) async {
    try {
      final response = await _dio.get(path, queryParameters: queryParameters);
      return parser(response.data);
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }

  Future<T> post<T>(
    String path, {
    dynamic data,
    required T Function(dynamic data) parser,
  }) async {
    try {
      final response = await _dio.post(path, data: data);
      return parser(response.data);
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }

  Future<void> postVoid(String path, {dynamic data}) async {
    try {
      await _dio.post(path, data: data);
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }

  Future<T> put<T>(
    String path, {
    dynamic data,
    required T Function(dynamic data) parser,
  }) async {
    try {
      final response = await _dio.put(path, data: data);
      return parser(response.data);
    } on DioException catch (e) {
      throw _mapError(e);
    }
  }

  ApiException _mapError(DioException e) {
    final data = e.response?.data;
    if (data is Map<String, dynamic>) {
      final error = ApiErrorResponse.fromJson(data);
      return ApiException(
        message: error.message,
        code: error.code,
        statusCode: e.response?.statusCode,
      );
    }
    return ApiException(
      message: e.message ?? 'Network error',
      statusCode: e.response?.statusCode,
    );
  }
}
