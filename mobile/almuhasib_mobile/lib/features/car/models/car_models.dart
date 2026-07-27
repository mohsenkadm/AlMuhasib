class NameCountPoint {
  NameCountPoint({required this.name, required this.count});

  factory NameCountPoint.fromJson(Map<String, dynamic> json) {
    return NameCountPoint(
      name: json['name'] as String? ?? '',
      count: json['count'] as int? ?? 0,
    );
  }

  final String name;
  final int count;

  double get value => count.toDouble();
}

class NameAmountPoint {
  NameAmountPoint({required this.name, required this.amount});

  factory NameAmountPoint.fromJson(Map<String, dynamic> json) {
    return NameAmountPoint(
      name: json['name'] as String? ?? '',
      amount: _num(json['amount']),
    );
  }

  final String name;
  final double amount;
}

class CarDashboardDto {
  CarDashboardDto({
    required this.todayContracts,
    required this.monthContracts,
    required this.unpaidContracts,
    required this.totalCarValue,
    required this.totalReceived,
    required this.totalRemaining,
    this.totalContracts = 0,
    this.monthlyContracts = const [],
    this.paymentStatusChart = const [],
    this.topCarTypes = const [],
    this.topBuyers = const [],
    this.recentContracts = const [],
  });

  factory CarDashboardDto.fromJson(Map<String, dynamic> json) {
    return CarDashboardDto(
      todayContracts: json['todayContracts'] as int? ?? 0,
      monthContracts: json['monthContracts'] as int? ?? 0,
      unpaidContracts: json['unpaidContracts'] as int? ?? 0,
      totalContracts: json['totalContracts'] as int? ?? 0,
      totalCarValue: _num(json['totalCarValue']),
      totalReceived: _num(json['totalReceived']),
      totalRemaining: _num(json['totalRemaining']),
      monthlyContracts: _countPoints(json['monthlyContracts']),
      paymentStatusChart: _amountPoints(json['paymentStatusChart']),
      topCarTypes: _countPoints(json['topCarTypes']),
      topBuyers: _countPoints(json['topBuyers']),
      recentContracts: (json['recentContracts'] as List<dynamic>?)
              ?.map(
                (e) => CarContractListItem.fromJson(e as Map<String, dynamic>),
              )
              .toList() ??
          [],
    );
  }

  final int todayContracts;
  final int monthContracts;
  final int unpaidContracts;
  final int totalContracts;
  final double totalCarValue;
  final double totalReceived;
  final double totalRemaining;
  final List<NameCountPoint> monthlyContracts;
  final List<NameAmountPoint> paymentStatusChart;
  final List<NameCountPoint> topCarTypes;
  final List<NameCountPoint> topBuyers;
  final List<CarContractListItem> recentContracts;
}

class CarReportDto {
  CarReportDto({
    this.rows = const [],
    this.contractCount = 0,
    this.totalCarValue = 0,
    this.totalReceived = 0,
    this.totalRemaining = 0,
    this.monthlyContracts = const [],
    this.collectedVsRemaining = const [],
    this.byCarType = const [],
  });

  factory CarReportDto.fromJson(Map<String, dynamic> json) {
    // Backwards compatible: API used to return a bare list.
    if (json.containsKey('rows') || json.containsKey('contractCount')) {
      return CarReportDto(
        rows: (json['rows'] as List<dynamic>?)
                ?.map(
                  (e) => CarContractListItem.fromJson(e as Map<String, dynamic>),
                )
                .toList() ??
            [],
        contractCount: json['contractCount'] as int? ?? 0,
        totalCarValue: _num(json['totalCarValue']),
        totalReceived: _num(json['totalReceived']),
        totalRemaining: _num(json['totalRemaining']),
        monthlyContracts: _countPoints(json['monthlyContracts']),
        collectedVsRemaining: _amountPoints(json['collectedVsRemaining']),
        byCarType: _countPoints(json['byCarType']),
      );
    }
    return CarReportDto();
  }

  final List<CarContractListItem> rows;
  final int contractCount;
  final double totalCarValue;
  final double totalReceived;
  final double totalRemaining;
  final List<NameCountPoint> monthlyContracts;
  final List<NameAmountPoint> collectedVsRemaining;
  final List<NameCountPoint> byCarType;
}

class CarContractListItem {
  CarContractListItem({
    required this.syncId,
    required this.contractNumber,
    required this.contractDate,
    required this.buyerName,
    required this.sellerName,
    required this.plateNumber,
    required this.carType,
    required this.carPrice,
    required this.amountReceived,
    required this.remainingAmount,
    required this.status,
  });

  factory CarContractListItem.fromJson(Map<String, dynamic> json) {
    return CarContractListItem(
      syncId: json['syncId'] as String? ?? '',
      contractNumber: json['contractNumber'] as String? ?? '',
      contractDate: DateTime.tryParse(json['contractDate'] as String? ?? '') ??
          DateTime.now(),
      buyerName: json['buyerName'] as String? ?? '',
      sellerName: json['sellerName'] as String? ?? '',
      plateNumber: json['plateNumber'] as String? ?? '',
      carType: json['carType'] as String? ?? '',
      carPrice: _num(json['carPrice']),
      amountReceived: _num(json['amountReceived']),
      remainingAmount: _num(json['remainingAmount']),
      status: json['status'] as String? ?? 'Active',
    );
  }

  final String syncId;
  final String contractNumber;
  final DateTime contractDate;
  final String buyerName;
  final String sellerName;
  final String plateNumber;
  final String carType;
  final double carPrice;
  final double amountReceived;
  final double remainingAmount;
  final String status;
}

class CarContractDetail extends CarContractListItem {
  CarContractDetail({
    required super.syncId,
    required super.contractNumber,
    required super.contractDate,
    required super.buyerName,
    required super.sellerName,
    required super.plateNumber,
    required super.carType,
    required super.carPrice,
    required super.amountReceived,
    required super.remainingAmount,
    required super.status,
    this.sellerPhone = '',
    this.buyerPhone = '',
    this.carModel = '',
    this.carColor = '',
    this.chassisNumber = '',
    this.notes = '',
    this.payments = const [],
  });

  factory CarContractDetail.fromJson(Map<String, dynamic> json) {
    return CarContractDetail(
      syncId: json['syncId'] as String? ?? '',
      contractNumber: json['contractNumber'] as String? ?? '',
      contractDate: DateTime.tryParse(json['contractDate'] as String? ?? '') ??
          DateTime.now(),
      buyerName: json['buyerName'] as String? ?? '',
      sellerName: json['sellerName'] as String? ?? '',
      plateNumber: json['plateNumber'] as String? ?? '',
      carType: json['carType'] as String? ?? '',
      carPrice: _num(json['carPrice']),
      amountReceived: _num(json['amountReceived']),
      remainingAmount: _num(json['remainingAmount']),
      status: json['status'] as String? ?? 'Active',
      sellerPhone: json['sellerPhone'] as String? ?? '',
      buyerPhone: json['buyerPhone'] as String? ?? '',
      carModel: json['carModel'] as String? ?? '',
      carColor: json['carColor'] as String? ?? '',
      chassisNumber: json['chassisNumber'] as String? ?? '',
      notes: json['notes'] as String? ?? '',
      payments: (json['payments'] as List<dynamic>?)
              ?.map((e) => CarPaymentItem.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [],
    );
  }

  final String sellerPhone;
  final String buyerPhone;
  final String carModel;
  final String carColor;
  final String chassisNumber;
  final String notes;
  final List<CarPaymentItem> payments;
}

class CarPaymentItem {
  CarPaymentItem({
    required this.syncId,
    required this.amount,
    required this.paymentDate,
    this.notes = '',
  });

  factory CarPaymentItem.fromJson(Map<String, dynamic> json) {
    return CarPaymentItem(
      syncId: json['syncId'] as String? ?? '',
      amount: _num(json['amount']),
      paymentDate: DateTime.tryParse(json['paymentDate'] as String? ?? '') ??
          DateTime.now(),
      notes: json['notes'] as String? ?? '',
    );
  }

  final String syncId;
  final double amount;
  final DateTime paymentDate;
  final String notes;
}

class CreateCarContractRequest {
  CreateCarContractRequest({
    required this.contractNumber,
    required this.contractDate,
    required this.sellerName,
    required this.buyerName,
    required this.plateNumber,
    required this.carType,
    required this.carPrice,
    required this.amountReceived,
    this.sellerPhone = '',
    this.buyerPhone = '',
    this.carModel = '',
    this.carColor = '',
    this.chassisNumber = '',
    this.notes = '',
  });

  Map<String, dynamic> toJson() => {
        'contractNumber': contractNumber,
        'contractDate': contractDate.toIso8601String(),
        'sellerName': sellerName,
        'buyerName': buyerName,
        'sellerPhone': sellerPhone,
        'buyerPhone': buyerPhone,
        'plateNumber': plateNumber,
        'carType': carType,
        'carModel': carModel,
        'carColor': carColor,
        'chassisNumber': chassisNumber,
        'carPrice': carPrice,
        'amountReceived': amountReceived,
        'notes': notes,
      };

  final String contractNumber;
  final DateTime contractDate;
  final String sellerName;
  final String buyerName;
  final String sellerPhone;
  final String buyerPhone;
  final String plateNumber;
  final String carType;
  final String carModel;
  final String carColor;
  final String chassisNumber;
  final double carPrice;
  final double amountReceived;
  final String notes;
}

List<NameCountPoint> _countPoints(dynamic raw) {
  if (raw is! List) return const [];
  return raw
      .map((e) => NameCountPoint.fromJson(e as Map<String, dynamic>))
      .toList();
}

List<NameAmountPoint> _amountPoints(dynamic raw) {
  if (raw is! List) return const [];
  return raw
      .map((e) => NameAmountPoint.fromJson(e as Map<String, dynamic>))
      .toList();
}

double _num(dynamic v) {
  if (v is num) return v.toDouble();
  return double.tryParse(v?.toString() ?? '') ?? 0;
}
