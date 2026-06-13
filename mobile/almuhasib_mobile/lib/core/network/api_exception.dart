class ApiException implements Exception {
  ApiException({required this.message, this.code, this.statusCode});

  final String message;
  final String? code;
  final int? statusCode;

  @override
  String toString() => message;
}

String mapApiErrorCode(String? code) {
  switch (code) {
    case 'INVALID_CREDENTIALS':
      return 'error_invalid_credentials';
    case 'SYNC_NOT_ENABLED':
    case 'LICENSE_DISABLED':
      return 'error_license_disabled';
    case 'LICENSE_EXPIRED':
      return 'error_license_expired';
    default:
      return 'error_network';
  }
}
