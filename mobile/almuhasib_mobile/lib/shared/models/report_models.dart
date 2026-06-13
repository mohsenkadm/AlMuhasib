class SalesReportResult {
  SalesReportResult({
    required this.totalSales,
    required this.invoiceCount,
    required this.averageInvoice,
    required this.rows,
  });

  factory SalesReportResult.fromJson(Map<String, dynamic> json) {
    return SalesReportResult(
      totalSales: _num(json['totalSales']),
      invoiceCount: json['invoiceCount'] as int? ?? 0,
      averageInvoice: _num(json['averageInvoice']),
      rows: (json['rows'] as List<dynamic>? ?? [])
          .map((e) => SalesReportRow.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  final double totalSales;
  final int invoiceCount;
  final double averageInvoice;
  final List<SalesReportRow> rows;
}

class SalesReportRow {
  SalesReportRow({
    required this.invoiceNumber,
    required this.date,
    required this.customerName,
    required this.netAmount,
  });

  factory SalesReportRow.fromJson(Map<String, dynamic> json) {
    return SalesReportRow(
      invoiceNumber: json['invoiceNumber'] as String? ?? '',
      date: DateTime.parse(json['date'] as String),
      customerName: json['customerName'] as String? ?? '',
      netAmount: _num(json['netAmount']),
    );
  }

  final String invoiceNumber;
  final DateTime date;
  final String customerName;
  final double netAmount;
}

class PurchasesReportResult {
  PurchasesReportResult({
    required this.totalPurchases,
    required this.invoiceCount,
    required this.rows,
  });

  factory PurchasesReportResult.fromJson(Map<String, dynamic> json) {
    return PurchasesReportResult(
      totalPurchases: _num(json['totalPurchases']),
      invoiceCount: json['invoiceCount'] as int? ?? 0,
      rows: (json['rows'] as List<dynamic>? ?? [])
          .map((e) => PurchasesReportRow.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  final double totalPurchases;
  final int invoiceCount;
  final List<PurchasesReportRow> rows;
}

class PurchasesReportRow {
  PurchasesReportRow({
    required this.invoiceNumber,
    required this.date,
    required this.supplierName,
    required this.netAmount,
  });

  factory PurchasesReportRow.fromJson(Map<String, dynamic> json) {
    return PurchasesReportRow(
      invoiceNumber: json['invoiceNumber'] as String? ?? '',
      date: DateTime.parse(json['date'] as String),
      supplierName: json['supplierName'] as String? ?? '',
      netAmount: _num(json['netAmount']),
    );
  }

  final String invoiceNumber;
  final DateTime date;
  final String supplierName;
  final double netAmount;
}

class ProfitReportResult {
  ProfitReportResult({
    required this.totalSales,
    required this.totalPurchases,
    required this.grossProfit,
    required this.totalExpenses,
    required this.netProfit,
    required this.profitMargin,
  });

  factory ProfitReportResult.fromJson(Map<String, dynamic> json) {
    return ProfitReportResult(
      totalSales: _num(json['totalSales']),
      totalPurchases: _num(json['totalPurchases']),
      grossProfit: _num(json['grossProfit']),
      totalExpenses: _num(json['totalExpenses']),
      netProfit: _num(json['netProfit']),
      profitMargin: _num(json['profitMargin']),
    );
  }

  final double totalSales;
  final double totalPurchases;
  final double grossProfit;
  final double totalExpenses;
  final double netProfit;
  final double profitMargin;
}

class OverdueResult {
  OverdueResult({
    required this.overdueCustomerCount,
    required this.totalOverdueAmount,
    required this.rows,
  });

  factory OverdueResult.fromJson(Map<String, dynamic> json) {
    return OverdueResult(
      overdueCustomerCount: json['overdueCustomerCount'] as int? ?? 0,
      totalOverdueAmount: _num(json['totalOverdueAmount']),
      rows: (json['rows'] as List<dynamic>? ?? [])
          .map((e) => OverdueRow.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  final int overdueCustomerCount;
  final double totalOverdueAmount;
  final List<OverdueRow> rows;
}

class OverdueRow {
  OverdueRow({
    required this.customerName,
    required this.phone,
    required this.overdueAmount,
    required this.overdueDays,
    required this.dueDate,
  });

  factory OverdueRow.fromJson(Map<String, dynamic> json) {
    return OverdueRow(
      customerName: json['customerName'] as String? ?? '',
      phone: json['phone'] as String? ?? '',
      overdueAmount: _num(json['overdueAmount']),
      overdueDays: json['overdueDays'] as int? ?? 0,
      dueDate: DateTime.parse(json['dueDate'] as String),
    );
  }

  final String customerName;
  final String phone;
  final double overdueAmount;
  final int overdueDays;
  final DateTime dueDate;
}

class CustomerStatementResult {
  CustomerStatementResult({
    required this.customerName,
    required this.balance,
    required this.rows,
  });

  factory CustomerStatementResult.fromJson(Map<String, dynamic> json) {
    return CustomerStatementResult(
      customerName: json['customerName'] as String? ?? '',
      balance: _num(json['balance']),
      rows: (json['rows'] as List<dynamic>? ?? [])
          .map((e) => CustomerStatementRow.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  final String customerName;
  final double balance;
  final List<CustomerStatementRow> rows;
}

class CustomerStatementRow {
  CustomerStatementRow({
    required this.date,
    required this.description,
    required this.debit,
    required this.credit,
    required this.runningBalance,
  });

  factory CustomerStatementRow.fromJson(Map<String, dynamic> json) {
    return CustomerStatementRow(
      date: DateTime.parse(json['date'] as String),
      description: json['description'] as String? ?? '',
      debit: _num(json['debit']),
      credit: _num(json['credit']),
      runningBalance: _num(json['runningBalance']),
    );
  }

  final DateTime date;
  final String description;
  final double debit;
  final double credit;
  final double runningBalance;
}

class InvestorStatementResult {
  InvestorStatementResult({
    required this.investorName,
    required this.balance,
    required this.rows,
  });

  factory InvestorStatementResult.fromJson(Map<String, dynamic> json) {
    return InvestorStatementResult(
      investorName: json['investorName'] as String? ?? '',
      balance: _num(json['balance']),
      rows: (json['rows'] as List<dynamic>? ?? [])
          .map((e) => InvestorStatementRow.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  final String investorName;
  final double balance;
  final List<InvestorStatementRow> rows;
}

class InvestorStatementRow {
  InvestorStatementRow({
    required this.date,
    required this.description,
    required this.debit,
    required this.credit,
    required this.runningBalance,
  });

  factory InvestorStatementRow.fromJson(Map<String, dynamic> json) {
    return InvestorStatementRow(
      date: DateTime.parse(json['date'] as String),
      description: json['description'] as String? ?? '',
      debit: _num(json['debit']),
      credit: _num(json['credit']),
      runningBalance: _num(json['runningBalance']),
    );
  }

  final DateTime date;
  final String description;
  final double debit;
  final double credit;
  final double runningBalance;
}

class WarehouseStockRow {
  WarehouseStockRow({
    required this.productName,
    required this.warehouseName,
    required this.quantity,
    required this.totalValue,
  });

  factory WarehouseStockRow.fromJson(Map<String, dynamic> json) {
    return WarehouseStockRow(
      productName: json['productName'] as String? ?? '',
      warehouseName: json['warehouseName'] as String? ?? '',
      quantity: _num(json['quantity']),
      totalValue: _num(json['totalValue']),
    );
  }

  final String productName;
  final String warehouseName;
  final double quantity;
  final double totalValue;
}

class TopProductsReportResult {
  TopProductsReportResult({
    required this.totalRevenue,
    required this.productCount,
    required this.rows,
  });

  factory TopProductsReportResult.fromJson(Map<String, dynamic> json) {
    return TopProductsReportResult(
      totalRevenue: _num(json['totalRevenue']),
      productCount: json['productCount'] as int? ?? 0,
      rows: (json['rows'] as List<dynamic>? ?? [])
          .map((e) => TopProductRow.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  final double totalRevenue;
  final int productCount;
  final List<TopProductRow> rows;
}

class TopProductRow {
  TopProductRow({
    required this.rank,
    required this.productName,
    required this.quantitySold,
    required this.revenue,
  });

  factory TopProductRow.fromJson(Map<String, dynamic> json) {
    return TopProductRow(
      rank: json['rank'] as int? ?? 0,
      productName: json['productName'] as String? ?? '',
      quantitySold: _num(json['quantitySold']),
      revenue: _num(json['revenue']),
    );
  }

  final int rank;
  final String productName;
  final double quantitySold;
  final double revenue;
}

double _num(dynamic value) {
  if (value == null) return 0;
  if (value is num) return value.toDouble();
  return double.tryParse(value.toString()) ?? 0;
}
