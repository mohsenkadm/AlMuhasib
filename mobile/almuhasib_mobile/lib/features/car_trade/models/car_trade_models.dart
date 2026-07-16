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

class CarTradeDashboardDto {
  CarTradeDashboardDto({
    required this.todayTransactions,
    required this.monthTransactions,
    required this.unpaidTransactions,
    required this.totalPaid,
    required this.totalRemaining,
    this.totalTransactions = 0,
    this.buyCount = 0,
    this.sellCount = 0,
    this.totalBuyValue = 0,
    this.totalSellValue = 0,
    this.monthlyBuy = const [],
    this.monthlySell = const [],
    this.paymentStatusChart = const [],
    this.topCarTypes = const [],
    this.recentTransactions = const [],
  });

  factory CarTradeDashboardDto.fromJson(Map<String, dynamic> json) {
    return CarTradeDashboardDto(
      todayTransactions: json['todayTransactions'] as int? ?? 0,
      monthTransactions: json['monthTransactions'] as int? ?? 0,
      unpaidTransactions: json['unpaidTransactions'] as int? ?? 0,
      totalTransactions: json['totalTransactions'] as int? ?? 0,
      buyCount: json['buyCount'] as int? ?? 0,
      sellCount: json['sellCount'] as int? ?? 0,
      totalBuyValue: _num(json['totalBuyValue']),
      totalSellValue: _num(json['totalSellValue']),
      totalPaid: _num(json['totalPaid']),
      totalRemaining: _num(json['totalRemaining']),
      monthlyBuy: _countPoints(json['monthlyBuy']),
      monthlySell: _countPoints(json['monthlySell']),
      paymentStatusChart: _amountPoints(json['paymentStatusChart']),
      topCarTypes: _countPoints(json['topCarTypes']),
      recentTransactions: (json['recentTransactions'] as List<dynamic>?)
              ?.map(
                (e) => CarTradeTransactionListItem.fromJson(
                  e as Map<String, dynamic>,
                ),
              )
              .toList() ??
          [],
    );
  }

  final int todayTransactions;
  final int monthTransactions;
  final int unpaidTransactions;
  final int totalTransactions;
  final int buyCount;
  final int sellCount;
  final double totalBuyValue;
  final double totalSellValue;
  final double totalPaid;
  final double totalRemaining;
  final List<NameCountPoint> monthlyBuy;
  final List<NameCountPoint> monthlySell;
  final List<NameAmountPoint> paymentStatusChart;
  final List<NameCountPoint> topCarTypes;
  final List<CarTradeTransactionListItem> recentTransactions;
}

class CarTradeReportDto {
  CarTradeReportDto({
    this.rows = const [],
    this.buyCount = 0,
    this.sellCount = 0,
    this.totalBuyValue = 0,
    this.totalSellValue = 0,
    this.totalPaid = 0,
    this.totalRemaining = 0,
    this.monthlyBuy = const [],
    this.monthlySell = const [],
    this.collectedVsRemaining = const [],
    this.byCarType = const [],
  });

  factory CarTradeReportDto.fromJson(Map<String, dynamic> json) {
    return CarTradeReportDto(
      rows: (json['rows'] as List<dynamic>?)
              ?.map(
                (e) => CarTradeTransactionListItem.fromJson(
                  e as Map<String, dynamic>,
                ),
              )
              .toList() ??
          [],
      buyCount: json['buyCount'] as int? ?? 0,
      sellCount: json['sellCount'] as int? ?? 0,
      totalBuyValue: _num(json['totalBuyValue']),
      totalSellValue: _num(json['totalSellValue']),
      totalPaid: _num(json['totalPaid']),
      totalRemaining: _num(json['totalRemaining']),
      monthlyBuy: _countPoints(json['monthlyBuy']),
      monthlySell: _countPoints(json['monthlySell']),
      collectedVsRemaining: _amountPoints(json['collectedVsRemaining']),
      byCarType: _countPoints(json['byCarType']),
    );
  }

  final List<CarTradeTransactionListItem> rows;
  final int buyCount;
  final int sellCount;
  final double totalBuyValue;
  final double totalSellValue;
  final double totalPaid;
  final double totalRemaining;
  final List<NameCountPoint> monthlyBuy;
  final List<NameCountPoint> monthlySell;
  final List<NameAmountPoint> collectedVsRemaining;
  final List<NameCountPoint> byCarType;

  double get totalValue => totalBuyValue + totalSellValue;
}

class CarTradeTransactionListItem {
  CarTradeTransactionListItem({
    required this.syncId,
    required this.transactionNumber,
    required this.transactionDate,
    required this.tradeType,
    required this.carName,
    required this.plateNumber,
    required this.carType,
    required this.sellerName,
    required this.buyerName,
    required this.totalAmount,
    required this.amountPaid,
    required this.remainingAmount,
    required this.status,
  });

  factory CarTradeTransactionListItem.fromJson(Map<String, dynamic> json) {
    return CarTradeTransactionListItem(
      syncId: json['syncId'] as String? ?? '',
      transactionNumber: json['transactionNumber'] as String? ?? '',
      transactionDate:
          DateTime.tryParse(json['transactionDate'] as String? ?? '') ??
              DateTime.now(),
      tradeType: json['tradeType'] as String? ?? 'Buy',
      carName: json['carName'] as String? ?? '',
      plateNumber: json['plateNumber'] as String? ?? '',
      carType: json['carType'] as String? ?? '',
      sellerName: json['sellerName'] as String? ?? '',
      buyerName: json['buyerName'] as String? ?? '',
      totalAmount: _num(json['totalAmount']),
      amountPaid: _num(json['amountPaid']),
      remainingAmount: _num(json['remainingAmount']),
      status: json['status'] as String? ?? 'Active',
    );
  }

  final String syncId;
  final String transactionNumber;
  final DateTime transactionDate;
  final String tradeType;
  final String carName;
  final String plateNumber;
  final String carType;
  final String sellerName;
  final String buyerName;
  final double totalAmount;
  final double amountPaid;
  final double remainingAmount;
  final String status;

  bool get isBuy => tradeType.toLowerCase() == 'buy';
}

class CarTradeTransactionDetail extends CarTradeTransactionListItem {
  CarTradeTransactionDetail({
    required super.syncId,
    required super.transactionNumber,
    required super.transactionDate,
    required super.tradeType,
    required super.carName,
    required super.plateNumber,
    required super.carType,
    required super.sellerName,
    required super.buyerName,
    required super.totalAmount,
    required super.amountPaid,
    required super.remainingAmount,
    required super.status,
    this.sellerPhone = '',
    this.buyerPhone = '',
    this.carColor = '',
    this.chassisNumber = '',
    this.purchasePrice = 0,
    this.salePrice = 0,
    this.paymentMode = 'FullCash',
    this.notes = '',
    this.payments = const [],
  });

  factory CarTradeTransactionDetail.fromJson(Map<String, dynamic> json) {
    return CarTradeTransactionDetail(
      syncId: json['syncId'] as String? ?? '',
      transactionNumber: json['transactionNumber'] as String? ?? '',
      transactionDate:
          DateTime.tryParse(json['transactionDate'] as String? ?? '') ??
              DateTime.now(),
      tradeType: json['tradeType'] as String? ?? 'Buy',
      carName: json['carName'] as String? ?? '',
      plateNumber: json['plateNumber'] as String? ?? '',
      carType: json['carType'] as String? ?? '',
      sellerName: json['sellerName'] as String? ?? '',
      buyerName: json['buyerName'] as String? ?? '',
      totalAmount: _num(json['totalAmount']),
      amountPaid: _num(json['amountPaid']),
      remainingAmount: _num(json['remainingAmount']),
      status: json['status'] as String? ?? 'Active',
      sellerPhone: json['sellerPhone'] as String? ?? '',
      buyerPhone: json['buyerPhone'] as String? ?? '',
      carColor: json['carColor'] as String? ?? '',
      chassisNumber: json['chassisNumber'] as String? ?? '',
      purchasePrice: _num(json['purchasePrice']),
      salePrice: _num(json['salePrice']),
      paymentMode: json['paymentMode'] as String? ?? 'FullCash',
      notes: json['notes'] as String? ?? '',
      payments: (json['payments'] as List<dynamic>?)
              ?.map(
                (e) => CarTradePaymentItem.fromJson(e as Map<String, dynamic>),
              )
              .toList() ??
          [],
    );
  }

  final String sellerPhone;
  final String buyerPhone;
  final String carColor;
  final String chassisNumber;
  final double purchasePrice;
  final double salePrice;
  final String paymentMode;
  final String notes;
  final List<CarTradePaymentItem> payments;
}

class CarTradePaymentItem {
  CarTradePaymentItem({
    required this.syncId,
    required this.amount,
    required this.paymentDate,
    this.notes = '',
  });

  factory CarTradePaymentItem.fromJson(Map<String, dynamic> json) {
    return CarTradePaymentItem(
      syncId: json['syncId'] as String? ?? '',
      amount: _num(json['amount']),
      paymentDate:
          DateTime.tryParse(json['paymentDate'] as String? ?? '') ??
              DateTime.now(),
      notes: json['notes'] as String? ?? '',
    );
  }

  final String syncId;
  final double amount;
  final DateTime paymentDate;
  final String notes;
}

class CarTradePartyStatementRowDto {
  CarTradePartyStatementRowDto({
    required this.transactionDate,
    required this.transactionNumber,
    required this.tradeType,
    required this.carName,
    required this.totalAmount,
    required this.amountPaid,
    required this.remainingAmount,
    required this.partyRole,
    this.debtKind = '',
  });

  factory CarTradePartyStatementRowDto.fromJson(Map<String, dynamic> json) {
    return CarTradePartyStatementRowDto(
      transactionDate:
          DateTime.tryParse(json['transactionDate'] as String? ?? '') ??
              DateTime.now(),
      transactionNumber: json['transactionNumber'] as String? ?? '',
      tradeType: json['tradeType'] as String? ?? '',
      carName: json['carName'] as String? ?? '',
      totalAmount: _num(json['totalAmount']),
      amountPaid: _num(json['amountPaid']),
      remainingAmount: _num(json['remainingAmount']),
      partyRole: json['partyRole'] as String? ?? '',
      debtKind: json['debtKind'] as String? ?? '',
    );
  }

  final DateTime transactionDate;
  final String transactionNumber;
  final String tradeType;
  final String carName;
  final double totalAmount;
  final double amountPaid;
  final double remainingAmount;
  final String partyRole;
  final String debtKind;
}

class CarTradePartyStatementDto {
  CarTradePartyStatementDto({
    required this.partyName,
    required this.totalDebit,
    required this.totalCredit,
    required this.balance,
    this.partyPhone = '',
    this.rows = const [],
  });

  factory CarTradePartyStatementDto.fromJson(Map<String, dynamic> json) {
    return CarTradePartyStatementDto(
      partyName: json['partyName'] as String? ?? '',
      partyPhone: json['partyPhone'] as String? ?? '',
      totalDebit: _num(json['totalDebit']),
      totalCredit: _num(json['totalCredit']),
      balance: _num(json['balance']),
      rows: (json['rows'] as List<dynamic>?)
              ?.map(
                (e) => CarTradePartyStatementRowDto.fromJson(
                  e as Map<String, dynamic>,
                ),
              )
              .toList() ??
          [],
    );
  }

  final String partyName;
  final String partyPhone;
  final double totalDebit;
  final double totalCredit;
  final double balance;
  final List<CarTradePartyStatementRowDto> rows;
}

class CreateCarTradeTransactionRequest {
  CreateCarTradeTransactionRequest({
    required this.transactionNumber,
    required this.transactionDate,
    required this.tradeType,
    required this.sellerName,
    required this.buyerName,
    required this.plateNumber,
    required this.carType,
    required this.carName,
    required this.totalAmount,
    required this.amountPaid,
    this.sellerPhone = '',
    this.buyerPhone = '',
    this.carColor = '',
    this.chassisNumber = '',
    this.purchasePrice = 0,
    this.salePrice = 0,
    this.paymentMode = 'FullCash',
    this.notes = '',
  });

  Map<String, dynamic> toJson() => {
        'transactionNumber': transactionNumber,
        'transactionDate': transactionDate.toIso8601String(),
        'tradeType': tradeType,
        'sellerName': sellerName,
        'buyerName': buyerName,
        'sellerPhone': sellerPhone,
        'buyerPhone': buyerPhone,
        'plateNumber': plateNumber,
        'carType': carType,
        'carName': carName,
        'carColor': carColor,
        'chassisNumber': chassisNumber,
        'purchasePrice': purchasePrice,
        'salePrice': salePrice,
        'totalAmount': totalAmount,
        'amountPaid': amountPaid,
        'paymentMode': paymentMode,
        'notes': notes,
      };

  final String transactionNumber;
  final DateTime transactionDate;
  final String tradeType;
  final String sellerName;
  final String buyerName;
  final String sellerPhone;
  final String buyerPhone;
  final String plateNumber;
  final String carType;
  final String carName;
  final String carColor;
  final String chassisNumber;
  final double purchasePrice;
  final double salePrice;
  final double totalAmount;
  final double amountPaid;
  final String paymentMode;
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
