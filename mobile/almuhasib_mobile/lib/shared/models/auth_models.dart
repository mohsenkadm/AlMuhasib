class TenantLoginRequest {
  TenantLoginRequest({required this.username, required this.password});

  final String username;
  final String password;

  Map<String, dynamic> toJson() => {
        'username': username,
        'password': password,
      };
}

class RefreshTokenRequest {
  RefreshTokenRequest({required this.refreshToken});

  final String refreshToken;

  Map<String, dynamic> toJson() => {'refreshToken': refreshToken};
}

class RegisterDeviceRequest {
  RegisterDeviceRequest({
    required this.playerId,
    this.deviceName,
    this.platform,
  });

  final String playerId;
  final String? deviceName;
  final String? platform;

  Map<String, dynamic> toJson() => {
        'playerId': playerId,
        if (deviceName != null) 'deviceName': deviceName,
        if (platform != null) 'platform': platform,
      };
}

class TenantLoginResponse {
  TenantLoginResponse({
    required this.accessToken,
    required this.refreshToken,
    required this.accessTokenExpiresAt,
    required this.tenantId,
    required this.companyName,
    required this.isMobileEnabled,
    this.licenseExpiresAt,
    this.accountExpiresAt,
    this.applicationSystemType = 0,
    this.tenantName,
  });

  factory TenantLoginResponse.fromJson(Map<String, dynamic> json) {
    return TenantLoginResponse(
      accessToken: json['accessToken'] as String? ?? '',
      refreshToken: json['refreshToken'] as String? ?? '',
      accessTokenExpiresAt: DateTime.parse(
        json['accessTokenExpiresAt'] as String,
      ),
      tenantId: json['tenantId'] as int? ?? 0,
      companyName: json['companyName'] as String? ?? '',
      isMobileEnabled: json['isMobileEnabled'] as bool? ?? false,
      licenseExpiresAt: json['licenseExpiresAt'] != null
          ? DateTime.tryParse(json['licenseExpiresAt'] as String)
          : null,
      accountExpiresAt: json['accountExpiresAt'] != null
          ? DateTime.tryParse(json['accountExpiresAt'] as String)
          : null,
      applicationSystemType: json['applicationSystemType'] as int? ?? 0,
      tenantName: json['tenantName'] as String?,
    );
  }

  final String accessToken;
  final String refreshToken;
  final DateTime accessTokenExpiresAt;
  final int tenantId;
  final String companyName;
  final bool isMobileEnabled;
  final DateTime? licenseExpiresAt;
  final DateTime? accountExpiresAt;
  final int applicationSystemType;
  final String? tenantName;
}

class LicenseStatusResponse {
  LicenseStatusResponse({
    required this.isActive,
    required this.isMobileEnabled,
    this.licenseExpiresAt,
    this.accountExpiresAt,
    this.statusCode,
    this.message,
  });

  factory LicenseStatusResponse.fromJson(Map<String, dynamic> json) {
    return LicenseStatusResponse(
      isActive: json['isActive'] as bool? ?? false,
      isMobileEnabled: json['isMobileEnabled'] as bool? ?? false,
      licenseExpiresAt: json['licenseExpiresAt'] != null
          ? DateTime.tryParse(json['licenseExpiresAt'] as String)
          : null,
      accountExpiresAt: json['accountExpiresAt'] != null
          ? DateTime.tryParse(json['accountExpiresAt'] as String)
          : null,
      statusCode: json['statusCode'] as String?,
      message: json['message'] as String?,
    );
  }

  final bool isActive;
  final bool isMobileEnabled;
  final DateTime? licenseExpiresAt;
  final DateTime? accountExpiresAt;
  final String? statusCode;
  final String? message;
}

class ApiErrorResponse {
  ApiErrorResponse({required this.code, required this.message});

  factory ApiErrorResponse.fromJson(Map<String, dynamic> json) {
    return ApiErrorResponse(
      code: json['code'] as String? ?? '',
      message: json['message'] as String? ?? '',
    );
  }

  final String code;
  final String message;
}
