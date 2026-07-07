class CarTradeDashboardDto {
  CarTradeDashboardDto({
    required this.todayTransactions,
    required this.monthTransactions,
    required this.unpaidTransactions,
    required this.totalPaid,
    required this.totalRemaining,
    this.recentTransactions = const [],
  });

  factory CarTradeDashboardDto.fromJson(Map<String, dynamic> json) {
    return CarTradeDashboardDto(
      todayTransactions: json['todayTransactions'] as int? ?? 0,
      monthTransactions: json['monthTransactions'] as int? ?? 0,
      unpaidTransactions: json['unpaidTransactions'] as int? ?? 0,
      totalPaid: _num(json['totalPaid']),
      totalRemaining: _num(json['totalRemaining']),
      recentTransactions: (json['recentTransactions'] as List<dynamic>?)
              ?.map(
                (e) =>
                    CarTradeTransactionListItem.fromJson(e as Map<String, dynamic>),
              )
              .toList() ??
          [],
    );
  }

  final int todayTransactions;
  final int monthTransactions;
  final int unpaidTransactions;
  final double totalPaid;
  final double totalRemaining;
  final List<CarTradeTransactionListItem> recentTransactions;
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

double _num(dynamic v) {
  if (v is num) return v.toDouble();
  return double.tryParse(v?.toString() ?? '') ?? 0;
}
