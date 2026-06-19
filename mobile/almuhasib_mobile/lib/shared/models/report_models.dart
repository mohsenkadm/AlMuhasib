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

class ProfitInvoiceDetailRow {
  ProfitInvoiceDetailRow({
    required this.invoiceNumber,
    required this.date,
    required this.customerName,
    required this.invoiceTypeLabel,
    required this.itemCount,
    required this.revenue,
    required this.cost,
    required this.grossProfit,
    required this.marginPercent,
  });

  factory ProfitInvoiceDetailRow.fromJson(Map<String, dynamic> json) {
    return ProfitInvoiceDetailRow(
      invoiceNumber: json['invoiceNumber'] as String? ?? '',
      date: DateTime.tryParse(json['date'] as String? ?? '') ?? DateTime.now(),
      customerName: json['customerName'] as String? ?? '',
      invoiceTypeLabel: json['invoiceTypeLabel'] as String? ?? '',
      itemCount: json['itemCount'] as int? ?? 0,
      revenue: _num(json['revenue']),
      cost: _num(json['cost']),
      grossProfit: _num(json['grossProfit']),
      marginPercent: _num(json['marginPercent']),
    );
  }

  final String invoiceNumber;
  final DateTime date;
  final String customerName;
  final String invoiceTypeLabel;
  final int itemCount;
  final double revenue;
  final double cost;
  final double grossProfit;
  final double marginPercent;
}

class BalanceSheetResult {
  BalanceSheetResult({
    required this.equityTotal,
    required this.liabilitiesTotal,
    required this.assetsTotal,
    required this.salesTotal,
    required this.costOfSales,
    required this.salesProfit,
    required this.expensesTotal,
    required this.supplierPayables,
    required this.investorDeposits,
    required this.cashBoxesTotal,
    required this.banksTotal,
    required this.customerDebts,
    required this.inventoryValue,
    required this.isBalanced,
    this.cashBoxes = const [],
    this.banks = const [],
  });

  factory BalanceSheetResult.fromJson(Map<String, dynamic> json) {
    return BalanceSheetResult(
      equityTotal: _num(json['equityTotal']),
      liabilitiesTotal: _num(json['liabilitiesTotal']),
      assetsTotal: _num(json['assetsTotal']),
      salesTotal: _num(json['salesTotal']),
      costOfSales: _num(json['costOfSales']),
      salesProfit: _num(json['salesProfit']),
      expensesTotal: _num(json['expensesTotal']),
      supplierPayables: _num(json['supplierPayables']),
      investorDeposits: _num(json['investorDeposits']),
      cashBoxesTotal: _num(json['cashBoxesTotal']),
      banksTotal: _num(json['banksTotal']),
      customerDebts: _num(json['customerDebts']),
      inventoryValue: _num(json['inventoryValue']),
      isBalanced: json['isBalanced'] as bool? ?? false,
      cashBoxes: (json['cashBoxes'] as List<dynamic>? ?? [])
          .map((e) => BalanceSheetNamedRow.fromJson(e as Map<String, dynamic>))
          .toList(),
      banks: (json['banks'] as List<dynamic>? ?? [])
          .map((e) => BalanceSheetNamedRow.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  final double equityTotal;
  final double liabilitiesTotal;
  final double assetsTotal;
  final double salesTotal;
  final double costOfSales;
  final double salesProfit;
  final double expensesTotal;
  final double supplierPayables;
  final double investorDeposits;
  final double cashBoxesTotal;
  final double banksTotal;
  final double customerDebts;
  final double inventoryValue;
  final bool isBalanced;
  final List<BalanceSheetNamedRow> cashBoxes;
  final List<BalanceSheetNamedRow> banks;
}

class BalanceSheetNamedRow {
  BalanceSheetNamedRow({required this.name, required this.balance});

  factory BalanceSheetNamedRow.fromJson(Map<String, dynamic> json) {
    return BalanceSheetNamedRow(
      name: json['name'] as String? ?? '',
      balance: _num(json['balance']),
    );
  }

  final String name;
  final double balance;
}

double _num(dynamic value) {
  if (value == null) return 0;
  if (value is num) return value.toDouble();
  return double.tryParse(value.toString()) ?? 0;
}
