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

class RealEstateDashboardDto {
  RealEstateDashboardDto({
    required this.todayContracts,
    required this.monthContracts,
    required this.unpaidContracts,
    required this.totalValue,
    required this.totalReceived,
    required this.totalRemaining,
    this.totalContracts = 0,
    this.overdueDebts = 0,
    this.monthlyContracts = const [],
    this.paymentStatusChart = const [],
    this.byContractType = const [],
    this.byPropertyType = const [],
    this.recentContracts = const [],
  });

  factory RealEstateDashboardDto.fromJson(Map<String, dynamic> json) {
    return RealEstateDashboardDto(
      todayContracts: json['todayContracts'] as int? ?? 0,
      monthContracts: json['monthContracts'] as int? ?? 0,
      unpaidContracts: json['unpaidContracts'] as int? ?? 0,
      totalContracts: json['totalContracts'] as int? ?? 0,
      overdueDebts: json['overdueDebts'] as int? ?? 0,
      totalValue: _num(json['totalValue']),
      totalReceived: _num(json['totalReceived']),
      totalRemaining: _num(json['totalRemaining']),
      monthlyContracts: _countPoints(json['monthlyContracts']),
      paymentStatusChart: _amountPoints(json['paymentStatusChart']),
      byContractType: _countPoints(json['byContractType']),
      byPropertyType: _countPoints(json['byPropertyType']),
      recentContracts: (json['recentContracts'] as List<dynamic>?)
              ?.map(
                (e) => RealEstateContractListItem.fromJson(
                  e as Map<String, dynamic>,
                ),
              )
              .toList() ??
          [],
    );
  }

  final int todayContracts;
  final int monthContracts;
  final int unpaidContracts;
  final int totalContracts;
  final int overdueDebts;
  final double totalValue;
  final double totalReceived;
  final double totalRemaining;
  final List<NameCountPoint> monthlyContracts;
  final List<NameAmountPoint> paymentStatusChart;
  final List<NameCountPoint> byContractType;
  final List<NameCountPoint> byPropertyType;
  final List<RealEstateContractListItem> recentContracts;
}

class RealEstateReportDto {
  RealEstateReportDto({
    this.rows = const [],
    this.contractCount = 0,
    this.totalValue = 0,
    this.totalReceived = 0,
    this.totalRemaining = 0,
    this.monthlyContracts = const [],
    this.collectedVsRemaining = const [],
    this.byPropertyType = const [],
    this.byContractType = const [],
  });

  factory RealEstateReportDto.fromJson(Map<String, dynamic> json) {
    if (json.containsKey('rows') || json.containsKey('contractCount')) {
      final rows = (json['rows'] as List<dynamic>?)
              ?.map(
                (e) => RealEstateContractListItem.fromJson(
                  e as Map<String, dynamic>,
                ),
              )
              .toList() ??
          [];
      return RealEstateReportDto(
        rows: rows,
        contractCount: json['contractCount'] as int? ?? rows.length,
        totalValue: _num(json['totalValue']),
        totalReceived: _num(json['totalReceived']),
        totalRemaining: _num(json['totalRemaining']),
        monthlyContracts: _countPoints(json['monthlyContracts']),
        collectedVsRemaining: _amountPoints(json['collectedVsRemaining']),
        byPropertyType: _countPoints(json['byPropertyType']),
        byContractType: _countPoints(json['byContractType']),
      );
    }
    return RealEstateReportDto();
  }

  final List<RealEstateContractListItem> rows;
  final int contractCount;
  final double totalValue;
  final double totalReceived;
  final double totalRemaining;
  final List<NameCountPoint> monthlyContracts;
  final List<NameAmountPoint> collectedVsRemaining;
  final List<NameCountPoint> byPropertyType;
  final List<NameCountPoint> byContractType;
}

class RealEstateContractListItem {
  RealEstateContractListItem({
    required this.syncId,
    required this.contractNumber,
    required this.contractDate,
    required this.buyerName,
    required this.sellerName,
    required this.propertyType,
    required this.propertyLocation,
    required this.totalPrice,
    required this.amountPaid,
    required this.remainingAmount,
    required this.status,
    this.contractType = '',
    this.paymentMode = '',
    this.debtorParty = '',
    this.propertyAreaSqm = 0,
    this.dueDate,
  });

  factory RealEstateContractListItem.fromJson(Map<String, dynamic> json) {
    return RealEstateContractListItem(
      syncId: _syncId(json['syncId']),
      contractNumber: json['contractNumber'] as String? ?? '',
      contractDate: DateTime.tryParse(json['contractDate'] as String? ?? '') ??
          DateTime.now(),
      buyerName: json['buyerName'] as String? ?? '',
      sellerName: json['sellerName'] as String? ?? '',
      contractType: _enumLabel(json['contractType']),
      propertyType: _enumLabel(json['propertyType']),
      propertyLocation: json['propertyLocation'] as String? ?? '',
      propertyAreaSqm: _num(json['propertyAreaSqm']),
      totalPrice: _num(json['totalPrice']),
      amountPaid: _num(json['amountPaid']),
      remainingAmount: _num(json['remainingAmount']),
      paymentMode: _enumLabel(json['paymentMode']),
      debtorParty: _enumLabel(json['debtorParty']),
      dueDate: DateTime.tryParse(json['dueDate'] as String? ?? ''),
      status: _enumLabel(json['status'], fallback: 'Active'),
    );
  }

  final String syncId;
  final String contractNumber;
  final DateTime contractDate;
  final String buyerName;
  final String sellerName;
  final String contractType;
  final String propertyType;
  final String propertyLocation;
  final double propertyAreaSqm;
  final double totalPrice;
  final double amountPaid;
  final double remainingAmount;
  final String paymentMode;
  final String debtorParty;
  final DateTime? dueDate;
  final String status;

  String get propertySummary {
    final loc = propertyLocation.trim();
    final type = propertyType.trim();
    if (loc.isNotEmpty && type.isNotEmpty) return '$type • $loc';
    if (loc.isNotEmpty) return loc;
    if (type.isNotEmpty) return type;
    return '';
  }
}

class RealEstateContractDetail extends RealEstateContractListItem {
  RealEstateContractDetail({
    required super.syncId,
    required super.contractNumber,
    required super.contractDate,
    required super.buyerName,
    required super.sellerName,
    required super.propertyType,
    required super.propertyLocation,
    required super.totalPrice,
    required super.amountPaid,
    required super.remainingAmount,
    required super.status,
    super.contractType,
    super.paymentMode,
    super.debtorParty,
    super.propertyAreaSqm,
    super.dueDate,
    this.propertyAddress = '',
    this.propertyDescription = '',
    this.sellerAddress = '',
    this.sellerIdNumber = '',
    this.sellerPhone = '',
    this.buyerAddress = '',
    this.buyerIdNumber = '',
    this.buyerPhone = '',
    this.totalPriceInWords = '',
    this.downPayment = 0,
    this.witnessOneName = '',
    this.witnessTwoName = '',
    this.notes = '',
    this.contractTypeValue = 0,
    this.propertyTypeValue = 0,
    this.paymentModeValue = 0,
    this.debtorPartyValue = 0,
    this.payments = const [],
    this.clauses = const [],
  });

  factory RealEstateContractDetail.fromJson(Map<String, dynamic> json) {
    return RealEstateContractDetail(
      syncId: _syncId(json['syncId']),
      contractNumber: json['contractNumber'] as String? ?? '',
      contractDate: DateTime.tryParse(json['contractDate'] as String? ?? '') ??
          DateTime.now(),
      buyerName: json['buyerName'] as String? ?? '',
      sellerName: json['sellerName'] as String? ?? '',
      contractType: _enumLabel(json['contractType']),
      propertyType: _enumLabel(json['propertyType']),
      propertyLocation: json['propertyLocation'] as String? ?? '',
      propertyAddress: json['propertyAddress'] as String? ?? '',
      propertyAreaSqm: _num(json['propertyAreaSqm']),
      propertyDescription: json['propertyDescription'] as String? ?? '',
      sellerAddress: json['sellerAddress'] as String? ?? '',
      sellerIdNumber: json['sellerIdNumber'] as String? ?? '',
      sellerPhone: json['sellerPhone'] as String? ?? '',
      buyerAddress: json['buyerAddress'] as String? ?? '',
      buyerIdNumber: json['buyerIdNumber'] as String? ?? '',
      buyerPhone: json['buyerPhone'] as String? ?? '',
      totalPrice: _num(json['totalPrice']),
      totalPriceInWords: json['totalPriceInWords'] as String? ?? '',
      downPayment: _num(json['downPayment']),
      amountPaid: _num(json['amountPaid']),
      remainingAmount: _num(json['remainingAmount']),
      paymentMode: _enumLabel(json['paymentMode']),
      debtorParty: _enumLabel(json['debtorParty']),
      dueDate: DateTime.tryParse(json['dueDate'] as String? ?? ''),
      witnessOneName: json['witnessOneName'] as String? ?? '',
      witnessTwoName: json['witnessTwoName'] as String? ?? '',
      notes: json['notes'] as String? ?? '',
      status: _enumLabel(json['status'], fallback: 'Active'),
      contractTypeValue: _enumInt(json['contractType']),
      propertyTypeValue: _enumInt(json['propertyType']),
      paymentModeValue: _enumInt(json['paymentMode']),
      debtorPartyValue: _enumInt(json['debtorParty']),
      payments: (json['payments'] as List<dynamic>?)
              ?.map(
                (e) => RealEstatePaymentItem.fromJson(
                  e as Map<String, dynamic>,
                ),
              )
              .toList() ??
          [],
      clauses: (json['clauses'] as List<dynamic>?)
              ?.map(
                (e) => RealEstateClauseItem.fromJson(
                  e as Map<String, dynamic>,
                ),
              )
              .toList() ??
          [],
    );
  }

  final String propertyAddress;
  final String propertyDescription;
  final String sellerAddress;
  final String sellerIdNumber;
  final String sellerPhone;
  final String buyerAddress;
  final String buyerIdNumber;
  final String buyerPhone;
  final String totalPriceInWords;
  final double downPayment;
  final String witnessOneName;
  final String witnessTwoName;
  final String notes;
  final int contractTypeValue;
  final int propertyTypeValue;
  final int paymentModeValue;
  final int debtorPartyValue;
  final List<RealEstatePaymentItem> payments;
  final List<RealEstateClauseItem> clauses;
}

class RealEstatePaymentItem {
  RealEstatePaymentItem({
    required this.syncId,
    required this.amount,
    required this.paymentDate,
    this.notes = '',
  });

  factory RealEstatePaymentItem.fromJson(Map<String, dynamic> json) {
    return RealEstatePaymentItem(
      syncId: _syncId(json['syncId']),
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

class RealEstateClauseItem {
  RealEstateClauseItem({
    this.syncId = '',
    this.sortOrder = 0,
    this.title = '',
    this.body = '',
  });

  factory RealEstateClauseItem.fromJson(Map<String, dynamic> json) {
    return RealEstateClauseItem(
      syncId: _syncId(json['syncId']),
      sortOrder: json['sortOrder'] as int? ?? 0,
      title: json['title'] as String? ?? '',
      body: json['body'] as String? ?? '',
    );
  }

  final String syncId;
  final int sortOrder;
  final String title;
  final String body;
}

class RealEstateDebtItem {
  RealEstateDebtItem({
    required this.contractNumber,
    required this.contractDate,
    required this.debtorName,
    required this.remainingAmount,
    this.syncId = '',
    this.debtorPhone = '',
    this.debtorParty = '',
    this.counterpartyName = '',
    this.dueDate,
    this.isOverdue = false,
    this.daysOverdue = 0,
  });

  factory RealEstateDebtItem.fromJson(Map<String, dynamic> json) {
    return RealEstateDebtItem(
      syncId: _syncId(json['syncId'] ?? json['contractSyncId']),
      contractNumber: json['contractNumber'] as String? ?? '',
      contractDate: DateTime.tryParse(json['contractDate'] as String? ?? '') ??
          DateTime.now(),
      debtorName: json['debtorName'] as String? ?? '',
      debtorPhone: json['debtorPhone'] as String? ?? '',
      debtorParty: _enumLabel(json['debtorParty']),
      counterpartyName: json['counterpartyName'] as String? ?? '',
      remainingAmount: _num(json['remainingAmount']),
      dueDate: DateTime.tryParse(json['dueDate'] as String? ?? ''),
      isOverdue: json['isOverdue'] as bool? ?? false,
      daysOverdue: json['daysOverdue'] as int? ?? 0,
    );
  }

  final String syncId;
  final String contractNumber;
  final DateTime contractDate;
  final String debtorName;
  final String debtorPhone;
  final String debtorParty;
  final String counterpartyName;
  final double remainingAmount;
  final DateTime? dueDate;
  final bool isOverdue;
  final int daysOverdue;
}

class CreateRealEstateContractRequest {
  CreateRealEstateContractRequest({
    required this.contractNumber,
    required this.contractDate,
    required this.sellerName,
    required this.buyerName,
    required this.totalPrice,
    required this.amountPaid,
    this.contractType = 0,
    this.propertyType = 0,
    this.propertyLocation = '',
    this.propertyAddress = '',
    this.propertyAreaSqm = 0,
    this.propertyDescription = '',
    this.sellerPhone = '',
    this.sellerAddress = '',
    this.sellerIdNumber = '',
    this.buyerPhone = '',
    this.buyerAddress = '',
    this.buyerIdNumber = '',
    this.downPayment = 0,
    this.paymentMode = 0,
    this.debtorParty = 0,
    this.dueDate,
    this.witnessOneName = '',
    this.witnessTwoName = '',
    this.notes = '',
  });

  Map<String, dynamic> toJson() => {
        'contractNumber': contractNumber,
        'contractDate': contractDate.toIso8601String(),
        'contractType': contractType,
        'propertyType': propertyType,
        'propertyLocation': propertyLocation,
        'propertyAddress': propertyAddress,
        'propertyAreaSqm': propertyAreaSqm,
        'propertyDescription': propertyDescription,
        'sellerName': sellerName,
        'sellerPhone': sellerPhone,
        'sellerAddress': sellerAddress,
        'sellerIdNumber': sellerIdNumber,
        'buyerName': buyerName,
        'buyerPhone': buyerPhone,
        'buyerAddress': buyerAddress,
        'buyerIdNumber': buyerIdNumber,
        'totalPrice': totalPrice,
        'downPayment': downPayment,
        'amountPaid': amountPaid,
        'paymentMode': paymentMode,
        'debtorParty': debtorParty,
        if (dueDate != null) 'dueDate': dueDate!.toIso8601String(),
        'witnessOneName': witnessOneName,
        'witnessTwoName': witnessTwoName,
        'notes': notes,
      };

  final String contractNumber;
  final DateTime contractDate;
  final int contractType;
  final int propertyType;
  final String propertyLocation;
  final String propertyAddress;
  final double propertyAreaSqm;
  final String propertyDescription;
  final String sellerName;
  final String sellerPhone;
  final String sellerAddress;
  final String sellerIdNumber;
  final String buyerName;
  final String buyerPhone;
  final String buyerAddress;
  final String buyerIdNumber;
  final double totalPrice;
  final double downPayment;
  final double amountPaid;
  final int paymentMode;
  final int debtorParty;
  final DateTime? dueDate;
  final String witnessOneName;
  final String witnessTwoName;
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

String _syncId(dynamic v) {
  if (v == null) return '';
  return v.toString();
}

int _enumInt(dynamic v) {
  if (v is int) return v;
  if (v is num) return v.toInt();
  final s = (v?.toString() ?? '').trim().toLowerCase();
  final asInt = int.tryParse(s);
  if (asInt != null) return asInt;
  switch (s) {
    case 'sale':
    case 'cash':
    case 'none':
    case 'house':
    case 'active':
      return 0;
    case 'purchase':
    case 'credit':
    case 'buyer':
    case 'land':
    case 'completed':
      return 1;
    case 'seller':
    case 'other':
    case 'cancelled':
      return 2;
    default:
      return 0;
  }
}

String _enumLabel(dynamic v, {String fallback = ''}) {
  if (v == null) return fallback;
  if (v is String) return v.isEmpty ? fallback : v;
  return v.toString();
}
