class GoldDashboardDto {
  GoldDashboardDto({
    this.todaySalesIqd = 0,
    this.todaySalesUsd = 0,
    this.todayPurchasesIqd = 0,
    this.todayPurchasesUsd = 0,
    this.cashBalanceIqd = 0,
    this.cashBalanceUsd = 0,
    this.totalStockGrams = 0,
    this.totalStockValueIqd = 0,
    this.openCreditCount = 0,
    this.openCreditIqd = 0,
    this.openCreditUsd = 0,
    this.overdueCreditCount = 0,
    this.lowStockKaratCount = 0,
    this.pricesUpdatedToday = false,
    this.latestUsdToIqd,
    this.mithqalGrams = 5,
    this.stockByKarat = const [],
    this.recentInvoices = const [],
    this.alerts = const [],
    this.latestPrices = const [],
  });

  factory GoldDashboardDto.fromJson(Map<String, dynamic> json) {
    return GoldDashboardDto(
      todaySalesIqd: _num(json['todaySalesIqd']),
      todaySalesUsd: _num(json['todaySalesUsd']),
      todayPurchasesIqd: _num(json['todayPurchasesIqd']),
      todayPurchasesUsd: _num(json['todayPurchasesUsd']),
      cashBalanceIqd: _num(json['cashBalanceIqd']),
      cashBalanceUsd: _num(json['cashBalanceUsd']),
      totalStockGrams: _num(json['totalStockGrams']),
      totalStockValueIqd: _num(json['totalStockValueIqd']),
      openCreditCount: json['openCreditCount'] as int? ?? 0,
      openCreditIqd: _num(json['openCreditIqd']),
      openCreditUsd: _num(json['openCreditUsd']),
      overdueCreditCount: json['overdueCreditCount'] as int? ?? 0,
      lowStockKaratCount: json['lowStockKaratCount'] as int? ?? 0,
      pricesUpdatedToday: json['pricesUpdatedToday'] as bool? ?? false,
      latestUsdToIqd: json['latestUsdToIqd'] == null
          ? null
          : _num(json['latestUsdToIqd']),
      mithqalGrams: () {
        final v = _num(json['mithqalGrams']);
        return v > 0 ? v : 5;
      }(),
      stockByKarat: (json['stockByKarat'] as List<dynamic>?)
              ?.map((e) => GoldStockRow.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const [],
      recentInvoices: (json['recentInvoices'] as List<dynamic>?)
              ?.map(
                (e) => GoldInvoiceListItem.fromJson(e as Map<String, dynamic>),
              )
              .toList() ??
          const [],
      alerts: (json['alerts'] as List<dynamic>?)
              ?.map((e) => GoldAlertItem.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const [],
      latestPrices: (json['latestPrices'] as List<dynamic>?)
              ?.map(
                (e) => GoldMithqalPriceRow.fromJson(e as Map<String, dynamic>),
              )
              .toList() ??
          const [],
    );
  }

  final double todaySalesIqd;
  final double todaySalesUsd;
  final double todayPurchasesIqd;
  final double todayPurchasesUsd;
  final double cashBalanceIqd;
  final double cashBalanceUsd;
  final double totalStockGrams;
  final double totalStockValueIqd;
  final int openCreditCount;
  final double openCreditIqd;
  final double openCreditUsd;
  final int overdueCreditCount;
  final int lowStockKaratCount;
  final bool pricesUpdatedToday;
  final double? latestUsdToIqd;
  final double mithqalGrams;
  final List<GoldStockRow> stockByKarat;
  final List<GoldInvoiceListItem> recentInvoices;
  final List<GoldAlertItem> alerts;
  final List<GoldMithqalPriceRow> latestPrices;
}

class GoldStockRow {
  GoldStockRow({
    required this.karatValue,
    required this.karatName,
    required this.gramsOnHand,
    this.averageCostPerGram = 0,
    this.stockValue = 0,
    this.pieceCount = 0,
    this.isLowStock = false,
  });

  factory GoldStockRow.fromJson(Map<String, dynamic> json) {
    return GoldStockRow(
      karatValue: json['karatValue'] as int? ?? 0,
      karatName: json['karatName'] as String? ?? '',
      gramsOnHand: _num(json['gramsOnHand']),
      averageCostPerGram: _num(json['averageCostPerGram']),
      stockValue: _num(json['stockValue']),
      pieceCount: json['pieceCount'] as int? ?? 0,
      isLowStock: json['isLowStock'] as bool? ?? false,
    );
  }

  final int karatValue;
  final String karatName;
  final double gramsOnHand;
  final double averageCostPerGram;
  final double stockValue;
  final int pieceCount;
  final bool isLowStock;
}

class GoldInvoiceListItem {
  GoldInvoiceListItem({
    required this.id,
    required this.invoiceNumber,
    required this.invoiceDate,
    this.invoiceType = 0,
    this.paymentMethod = 0,
    this.status = 0,
    this.customerName = '',
    this.pricingCurrency = 0,
    this.paymentCurrency = 0,
    this.totalWeightGrams = 0,
    this.totalAmount = 0,
    this.totalAmountIqd = 0,
    this.totalAmountUsd = 0,
    this.paidAmount = 0,
    this.remainingAmount = 0,
    this.notes = '',
  });

  factory GoldInvoiceListItem.fromJson(Map<String, dynamic> json) {
    return GoldInvoiceListItem(
      id: json['id'] as int? ?? 0,
      invoiceNumber: json['invoiceNumber'] as String? ?? '',
      invoiceDate: DateTime.tryParse(json['invoiceDate'] as String? ?? '') ??
          DateTime.now(),
      invoiceType: _enumInt(json['invoiceType']),
      paymentMethod: _enumInt(json['paymentMethod']),
      status: _enumInt(json['status']),
      customerName: json['customerName'] as String? ?? '',
      pricingCurrency: _enumInt(json['pricingCurrency']),
      paymentCurrency: _enumInt(json['paymentCurrency']),
      totalWeightGrams: _num(json['totalWeightGrams']),
      totalAmount: _num(json['totalAmount']),
      totalAmountIqd: _num(json['totalAmountIqd']),
      totalAmountUsd: _num(json['totalAmountUsd']),
      paidAmount: _num(json['paidAmount']),
      remainingAmount: _num(json['remainingAmount']),
      notes: json['notes'] as String? ?? '',
    );
  }

  final int id;
  final String invoiceNumber;
  final DateTime invoiceDate;
  final int invoiceType;
  final int paymentMethod;
  final int status;
  final String customerName;
  final int pricingCurrency;
  final int paymentCurrency;
  final double totalWeightGrams;
  final double totalAmount;
  final double totalAmountIqd;
  final double totalAmountUsd;
  final double paidAmount;
  final double remainingAmount;
  final String notes;
}

class GoldInvoiceDetail extends GoldInvoiceListItem {
  GoldInvoiceDetail({
    required super.id,
    required super.invoiceNumber,
    required super.invoiceDate,
    super.invoiceType,
    super.paymentMethod,
    super.status,
    super.customerName,
    super.pricingCurrency,
    super.paymentCurrency,
    super.totalWeightGrams,
    super.totalAmount,
    super.totalAmountIqd,
    super.totalAmountUsd,
    super.paidAmount,
    super.remainingAmount,
    super.notes,
    this.fxRate = 0,
    this.totalGoldValue = 0,
    this.totalMakingCharge = 0,
    this.discountAmount = 0,
    this.customerPhone = '',
    this.lines = const [],
    this.payments = const [],
  });

  factory GoldInvoiceDetail.fromJson(Map<String, dynamic> json) {
    return GoldInvoiceDetail(
      id: json['id'] as int? ?? 0,
      invoiceNumber: json['invoiceNumber'] as String? ?? '',
      invoiceDate: DateTime.tryParse(json['invoiceDate'] as String? ?? '') ??
          DateTime.now(),
      invoiceType: _enumInt(json['invoiceType']),
      paymentMethod: _enumInt(json['paymentMethod']),
      status: _enumInt(json['status']),
      customerName: json['customerName'] as String? ?? '',
      pricingCurrency: _enumInt(json['pricingCurrency']),
      paymentCurrency: _enumInt(json['paymentCurrency']),
      totalWeightGrams: _num(json['totalWeightGrams']),
      totalAmount: _num(json['totalAmount']),
      totalAmountIqd: _num(json['totalAmountIqd']),
      totalAmountUsd: _num(json['totalAmountUsd']),
      paidAmount: _num(json['paidAmount']),
      remainingAmount: _num(json['remainingAmount']),
      notes: json['notes'] as String? ?? '',
      fxRate: _num(json['fxRate']),
      totalGoldValue: _num(json['totalGoldValue']),
      totalMakingCharge: _num(json['totalMakingCharge']),
      discountAmount: _num(json['discountAmount']),
      customerPhone: json['customerPhone'] as String? ?? '',
      lines: (json['lines'] as List<dynamic>?)
              ?.map(
                (e) => GoldInvoiceLineItem.fromJson(e as Map<String, dynamic>),
              )
              .toList() ??
          const [],
      payments: (json['payments'] as List<dynamic>?)
              ?.map(
                (e) => GoldPaymentItem.fromJson(e as Map<String, dynamic>),
              )
              .toList() ??
          const [],
    );
  }

  final double fxRate;
  final double totalGoldValue;
  final double totalMakingCharge;
  final double discountAmount;
  final String customerPhone;
  final List<GoldInvoiceLineItem> lines;
  final List<GoldPaymentItem> payments;
}

class GoldInvoiceLineItem {
  GoldInvoiceLineItem({
    required this.id,
    this.description = '',
    this.karatValue = 0,
    this.karatName = '',
    this.weightGrams = 0,
    this.mithqalPrice = 0,
    this.makingCharge = 0,
    this.lineTotal = 0,
  });

  factory GoldInvoiceLineItem.fromJson(Map<String, dynamic> json) {
    return GoldInvoiceLineItem(
      id: json['id'] as int? ?? 0,
      description: json['description'] as String? ?? '',
      karatValue: json['karatValue'] as int? ?? 0,
      karatName: json['karatName'] as String? ?? '',
      weightGrams: _num(json['weightGrams']),
      mithqalPrice: _num(json['mithqalPrice']),
      makingCharge: _num(json['makingCharge']),
      lineTotal: _num(json['lineTotal']),
    );
  }

  final int id;
  final String description;
  final int karatValue;
  final String karatName;
  final double weightGrams;
  final double mithqalPrice;
  final double makingCharge;
  final double lineTotal;
}

class GoldPaymentItem {
  GoldPaymentItem({
    required this.id,
    required this.amount,
    required this.paymentDate,
    this.currency = 0,
    this.notes = '',
  });

  factory GoldPaymentItem.fromJson(Map<String, dynamic> json) {
    return GoldPaymentItem(
      id: json['id'] as int? ?? 0,
      amount: _num(json['amount']),
      paymentDate: DateTime.tryParse(json['paymentDate'] as String? ?? '') ??
          DateTime.now(),
      currency: _enumInt(json['currency']),
      notes: json['notes'] as String? ?? '',
    );
  }

  final int id;
  final double amount;
  final DateTime paymentDate;
  final int currency;
  final String notes;
}

class GoldCustomerListItem {
  GoldCustomerListItem({
    required this.id,
    required this.name,
    this.phone = '',
    this.address = '',
    this.creditBalanceIqd = 0,
    this.creditBalanceUsd = 0,
    this.isActive = true,
    this.openInvoiceCount = 0,
    this.lastTransactionDate,
  });

  factory GoldCustomerListItem.fromJson(Map<String, dynamic> json) {
    return GoldCustomerListItem(
      id: json['id'] as int? ?? 0,
      name: json['name'] as String? ?? '',
      phone: json['phone'] as String? ?? '',
      address: json['address'] as String? ?? '',
      creditBalanceIqd: _num(json['creditBalanceIqd']),
      creditBalanceUsd: _num(json['creditBalanceUsd']),
      isActive: json['isActive'] as bool? ?? true,
      openInvoiceCount: json['openInvoiceCount'] as int? ?? 0,
      lastTransactionDate: json['lastTransactionDate'] == null
          ? null
          : DateTime.tryParse(json['lastTransactionDate'] as String? ?? ''),
    );
  }

  final int id;
  final String name;
  final String phone;
  final String address;
  final double creditBalanceIqd;
  final double creditBalanceUsd;
  final bool isActive;
  final int openInvoiceCount;
  final DateTime? lastTransactionDate;
}

class GoldMithqalPriceRow {
  GoldMithqalPriceRow({
    required this.id,
    required this.priceDate,
    required this.karatValue,
    this.karatName = '',
    this.pricePerMithqal = 0,
    this.currency = 0,
    this.fxRateUsed,
    this.pricePerGram,
    this.notes = '',
  });

  factory GoldMithqalPriceRow.fromJson(Map<String, dynamic> json) {
    return GoldMithqalPriceRow(
      id: json['id'] as int? ?? 0,
      priceDate: DateTime.tryParse(json['priceDate'] as String? ?? '') ??
          DateTime.now(),
      karatValue: json['karatValue'] as int? ?? 0,
      karatName: json['karatName'] as String? ?? '',
      pricePerMithqal: _num(json['pricePerMithqal']),
      currency: _enumInt(json['currency']),
      fxRateUsed:
          json['fxRateUsed'] == null ? null : _num(json['fxRateUsed']),
      pricePerGram:
          json['pricePerGram'] == null ? null : _num(json['pricePerGram']),
      notes: json['notes'] as String? ?? '',
    );
  }

  final int id;
  final DateTime priceDate;
  final int karatValue;
  final String karatName;
  final double pricePerMithqal;
  final int currency;
  final double? fxRateUsed;
  final double? pricePerGram;
  final String notes;
}

class GoldAlertItem {
  GoldAlertItem({
    this.notificationId,
    this.type = 5,
    required this.title,
    required this.message,
    this.relatedEntity,
    this.relatedId,
    required this.createdAt,
    this.isRead = false,
  });

  factory GoldAlertItem.fromJson(Map<String, dynamic> json) {
    return GoldAlertItem(
      notificationId: json['notificationId'] as int? ?? json['id'] as int?,
      type: _enumInt(json['type'], fallback: 5),
      title: json['title'] as String? ?? '',
      message: json['message'] as String? ?? '',
      relatedEntity: json['relatedEntity'] as String?,
      relatedId: json['relatedId'] as int?,
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ??
          DateTime.now(),
      isRead: json['isRead'] as bool? ?? false,
    );
  }

  final int? notificationId;
  final int type;
  final String title;
  final String message;
  final String? relatedEntity;
  final int? relatedId;
  final DateTime createdAt;
  final bool isRead;
}

class GoldNotificationItem {
  GoldNotificationItem({
    required this.id,
    this.type = 5,
    required this.title,
    required this.message,
    this.isRead = false,
    this.readAt,
    this.relatedEntity,
    this.relatedId,
    required this.createdAt,
  });

  factory GoldNotificationItem.fromJson(Map<String, dynamic> json) {
    return GoldNotificationItem(
      id: json['id'] as int? ?? 0,
      type: _enumInt(json['type'], fallback: 5),
      title: json['title'] as String? ?? '',
      message: json['message'] as String? ?? '',
      isRead: json['isRead'] as bool? ?? false,
      readAt: json['readAt'] == null
          ? null
          : DateTime.tryParse(json['readAt'] as String? ?? ''),
      relatedEntity: json['relatedEntity'] as String?,
      relatedId: json['relatedId'] as int?,
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ??
          DateTime.now(),
    );
  }

  final int id;
  final int type;
  final String title;
  final String message;
  final bool isRead;
  final DateTime? readAt;
  final String? relatedEntity;
  final int? relatedId;
  final DateTime createdAt;
}

class GoldWarehouseItem {
  GoldWarehouseItem({
    required this.id,
    required this.name,
    this.isDefault = false,
    this.isActive = true,
    this.notes = '',
  });

  factory GoldWarehouseItem.fromJson(Map<String, dynamic> json) {
    return GoldWarehouseItem(
      id: json['id'] as int? ?? 0,
      name: json['name'] as String? ?? '',
      isDefault: json['isDefault'] as bool? ?? false,
      isActive: json['isActive'] as bool? ?? true,
      notes: json['notes'] as String? ?? '',
    );
  }

  final int id;
  final String name;
  final bool isDefault;
  final bool isActive;
  final String notes;
}

class GoldSupplierItem {
  GoldSupplierItem({
    required this.id,
    required this.name,
    this.phone = '',
    this.address = '',
    this.creditBalanceIqd = 0,
    this.creditBalanceUsd = 0,
    this.isActive = true,
  });

  factory GoldSupplierItem.fromJson(Map<String, dynamic> json) {
    return GoldSupplierItem(
      id: json['id'] as int? ?? 0,
      name: json['name'] as String? ?? '',
      phone: json['phone'] as String? ?? '',
      address: json['address'] as String? ?? '',
      creditBalanceIqd: _num(json['creditBalanceIqd']),
      creditBalanceUsd: _num(json['creditBalanceUsd']),
      isActive: json['isActive'] as bool? ?? true,
    );
  }

  final int id;
  final String name;
  final String phone;
  final String address;
  final double creditBalanceIqd;
  final double creditBalanceUsd;
  final bool isActive;
}

class CreateGoldSaleLineRequest {
  CreateGoldSaleLineRequest({
    required this.karatValue,
    required this.weightGrams,
    required this.mithqalPrice,
    this.makingCharge = 0,
    this.description = '',
    this.itemId,
    this.weightFromScale = false,
  });

  final int? itemId;
  final int karatValue;
  final double weightGrams;
  final double mithqalPrice;
  final double makingCharge;
  final String description;
  final bool weightFromScale;

  Map<String, dynamic> toJson() => {
        if (itemId != null) 'itemId': itemId,
        'karatValue': karatValue,
        'weightGrams': weightGrams,
        'mithqalPrice': mithqalPrice,
        'makingCharge': makingCharge,
        'description': description,
        'weightFromScale': weightFromScale,
      };
}

class CreateGoldSaleRequest {
  CreateGoldSaleRequest({
    required this.lines,
    this.invoiceDate,
    this.paymentMethod = 'Cash',
    this.customerId,
    this.supplierId,
    this.warehouseId,
    this.pricingCurrency = 'USD',
    this.paymentCurrency = 'IQD',
    this.fxRate = 0,
    this.discountAmount = 0,
    this.paidAmount = 0,
    this.cashBoxId,
    this.notes = '',
    this.weightFromScale = false,
  });

  final DateTime? invoiceDate;
  final String paymentMethod;
  final int? customerId;
  final int? supplierId;
  final int? warehouseId;
  final String pricingCurrency;
  final String paymentCurrency;
  final double fxRate;
  final double discountAmount;
  final double paidAmount;
  final int? cashBoxId;
  final String notes;
  final bool weightFromScale;
  final List<CreateGoldSaleLineRequest> lines;

  Map<String, dynamic> toJson() => {
        'invoiceDate': (invoiceDate ?? DateTime.now()).toIso8601String(),
        'paymentMethod': paymentMethod,
        if (customerId != null) 'customerId': customerId,
        if (supplierId != null) 'supplierId': supplierId,
        if (warehouseId != null) 'warehouseId': warehouseId,
        'pricingCurrency': pricingCurrency,
        'paymentCurrency': paymentCurrency,
        'fxRate': fxRate,
        'discountAmount': discountAmount,
        'paidAmount': paidAmount,
        if (cashBoxId != null) 'cashBoxId': cashBoxId,
        'notes': notes,
        'weightFromScale': weightFromScale,
        'lines': lines.map((e) => e.toJson()).toList(),
      };
}

double _num(dynamic v) {
  if (v is num) return v.toDouble();
  return double.tryParse(v?.toString() ?? '') ?? 0;
}

int _enumInt(dynamic v, {int fallback = 0}) {
  if (v is int) return v;
  if (v is num) return v.toInt();
  if (v is String) {
    final parsed = int.tryParse(v);
    if (parsed != null) return parsed;
    return switch (v.toLowerCase()) {
      'iqd' || 'cash' || 'sale' || 'completed' || 'instock' || 'receipt' => 0,
      'usd' || 'credit' || 'purchase' || 'open' || 'sold' || 'payment' => 1,
      'partiallypaid' || 'reserved' => 2,
      'cancelled' => 3,
      'pricenotupdated' => 0,
      'overduecredit' => 1,
      'lowstock' => 2,
      'scaledisconnected' => 3,
      'negativecash' => 4,
      'info' => 5,
      _ => fallback,
    };
  }
  return fallback;
}
