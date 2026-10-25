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

class BranchInfo {
  BranchInfo({
    required this.branchId,
    required this.name,
    required this.code,
    this.syncId,
    this.isMain = false,
    this.isDefault = false,
  });

  factory BranchInfo.fromJson(Map<String, dynamic> json) {
    final rawSync = json['syncId'];
    return BranchInfo(
      branchId: json['branchId'] as int? ?? 0,
      syncId: rawSync?.toString(),
      name: json['name'] as String? ?? '',
      code: json['code'] as String? ?? '',
      isMain: json['isMain'] as bool? ?? false,
      isDefault: json['isDefault'] as bool? ?? false,
    );
  }

  final int branchId;
  final String? syncId;
  final String name;
  final String code;
  final bool isMain;
  final bool isDefault;
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
    this.allowedBranches = const [],
    this.defaultBranchId,
    this.requiresBranchSelection = false,
    this.currentBranchId,
    this.canViewAllBranches = false,
  });

  factory TenantLoginResponse.fromJson(Map<String, dynamic> json) {
    final branchesJson = json['allowedBranches'] as List<dynamic>? ?? const [];
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
      allowedBranches: branchesJson
          .whereType<Map<String, dynamic>>()
          .map(BranchInfo.fromJson)
          .toList(),
      defaultBranchId: json['defaultBranchId'] as int?,
      requiresBranchSelection: json['requiresBranchSelection'] as bool? ?? false,
      currentBranchId: json['currentBranchId'] as int?,
      canViewAllBranches: json['canViewAllBranches'] as bool? ?? false,
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
  final List<BranchInfo> allowedBranches;
  final int? defaultBranchId;
  final bool requiresBranchSelection;
  final int? currentBranchId;
  final bool canViewAllBranches;
}

class SelectBranchResponse {
  SelectBranchResponse({
    required this.accessToken,
    required this.accessTokenExpiresAt,
    this.currentBranchId,
    this.isAllBranchesMode = false,
    this.branch,
  });

  factory SelectBranchResponse.fromJson(Map<String, dynamic> json) {
    return SelectBranchResponse(
      accessToken: json['accessToken'] as String? ?? '',
      accessTokenExpiresAt: DateTime.parse(
        json['accessTokenExpiresAt'] as String,
      ),
      currentBranchId: json['currentBranchId'] as int?,
      isAllBranchesMode: json['isAllBranchesMode'] as bool? ?? false,
      branch: json['branch'] is Map<String, dynamic>
          ? BranchInfo.fromJson(json['branch'] as Map<String, dynamic>)
          : null,
    );
  }

  final String accessToken;
  final DateTime accessTokenExpiresAt;
  final int? currentBranchId;
  final bool isAllBranchesMode;
  final BranchInfo? branch;
}

class AllowedBranchesResponse {
  AllowedBranchesResponse({
    required this.allowedBranches,
    this.defaultBranchId,
    this.currentBranchId,
  });

  factory AllowedBranchesResponse.fromJson(Map<String, dynamic> json) {
    final branchesJson = json['allowedBranches'] as List<dynamic>? ?? const [];
    return AllowedBranchesResponse(
      allowedBranches: branchesJson
          .whereType<Map<String, dynamic>>()
          .map(BranchInfo.fromJson)
          .toList(),
      defaultBranchId: json['defaultBranchId'] as int?,
      currentBranchId: json['currentBranchId'] as int?,
    );
  }

  final List<BranchInfo> allowedBranches;
  final int? defaultBranchId;
  final int? currentBranchId;
}

class LatestExchangeRateResponse {
  LatestExchangeRateResponse({
    required this.rateDate,
    required this.usdToIqd,
    this.notes = '',
    this.syncId,
  });

  factory LatestExchangeRateResponse.fromJson(Map<String, dynamic> json) {
    return LatestExchangeRateResponse(
      rateDate: DateTime.tryParse(json['rateDate'] as String? ?? '') ??
          DateTime.now(),
      usdToIqd: (json['usdToIqd'] as num?)?.toDouble() ?? 0,
      notes: json['notes'] as String? ?? '',
      syncId: json['syncId']?.toString(),
    );
  }

  final DateTime rateDate;
  final double usdToIqd;
  final String notes;
  final String? syncId;
}

class CreateExchangeRateRequest {
  CreateExchangeRateRequest({
    this.syncId,
    required this.rateDate,
    required this.usdToIqd,
    this.notes,
  });

  Map<String, dynamic> toJson() => {
        if (syncId != null) 'syncId': syncId,
        'rateDate': rateDate.toIso8601String(),
        'usdToIqd': usdToIqd,
        if (notes != null) 'notes': notes,
      };

  final String? syncId;
  final DateTime rateDate;
  final double usdToIqd;
  final String? notes;
}

class TenantAccountListItem {
  TenantAccountListItem({
    required this.id,
    required this.username,
    required this.isActive,
    required this.createdAt,
    this.isOwner = false,
  });

  factory TenantAccountListItem.fromJson(Map<String, dynamic> json) {
    return TenantAccountListItem(
      id: json['id'] as int? ?? 0,
      username: json['username'] as String? ?? '',
      isActive: json['isActive'] as bool? ?? false,
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ??
          DateTime.fromMillisecondsSinceEpoch(0),
      isOwner: json['isOwner'] as bool? ?? false,
    );
  }

  final int id;
  final String username;
  final bool isActive;
  final DateTime createdAt;
  final bool isOwner;
}

class TenantAccountBranchesResponse {
  TenantAccountBranchesResponse({
    required this.accountId,
    required this.username,
    required this.branches,
    this.defaultBranchId,
  });

  factory TenantAccountBranchesResponse.fromJson(Map<String, dynamic> json) {
    final branchesJson = json['branches'] as List<dynamic>? ?? const [];
    return TenantAccountBranchesResponse(
      accountId: json['accountId'] as int? ?? 0,
      username: json['username'] as String? ?? '',
      branches: branchesJson
          .whereType<Map<String, dynamic>>()
          .map(BranchInfo.fromJson)
          .toList(),
      defaultBranchId: json['defaultBranchId'] as int?,
    );
  }

  final int accountId;
  final String username;
  final List<BranchInfo> branches;
  final int? defaultBranchId;
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
