class DashboardData {
  DashboardData({
    required this.todaySales,
    required this.todayPurchases,
    required this.netProfit,
    required this.overdueInstallmentsCount,
    required this.salesLast30Days,
    required this.expenseDistribution,
    required this.recentTransactions,
    required this.upcomingInstallments,
    required this.investorBalance,
    required this.unpaidInstallmentsBalance,
    required this.customerCreditBalance,
    required this.cashBoxes,
    required this.bankBalance,
    required this.totalInventoryValue,
  });

  factory DashboardData.fromJson(Map<String, dynamic> json) {
    return DashboardData(
      todaySales: _toDecimal(json['todaySales']),
      todayPurchases: _toDecimal(json['todayPurchases']),
      netProfit: _toDecimal(json['netProfit']),
      overdueInstallmentsCount: json['overdueInstallmentsCount'] as int? ?? 0,
      salesLast30Days: (json['salesLast30Days'] as List<dynamic>? ?? [])
          .map((e) => DailySalesPoint.fromJson(e as Map<String, dynamic>))
          .toList(),
      expenseDistribution: (json['expenseDistribution'] as List<dynamic>? ?? [])
          .map((e) => ExpenseCategoryShare.fromJson(e as Map<String, dynamic>))
          .toList(),
      recentTransactions:
          (json['recentTransactions'] as List<dynamic>? ?? [])
              .map((e) => RecentTransaction.fromJson(e as Map<String, dynamic>))
              .toList(),
      upcomingInstallments:
          (json['upcomingInstallments'] as List<dynamic>? ?? [])
              .map((e) => UpcomingInstallment.fromJson(e as Map<String, dynamic>))
              .toList(),
      investorBalance: _toDecimal(json['investorBalance']),
      unpaidInstallmentsBalance: _toDecimal(json['unpaidInstallmentsBalance']),
      customerCreditBalance: _toDecimal(json['customerCreditBalance']),
      cashBoxes: (json['cashBoxes'] as List<dynamic>? ?? [])
          .map((e) => CashBoxSummary.fromJson(e as Map<String, dynamic>))
          .toList(),
      bankBalance: _toDecimal(json['bankBalance']),
      totalInventoryValue: _toDecimal(json['totalInventoryValue']),
    );
  }

  final double todaySales;
  final double todayPurchases;
  final double netProfit;
  final int overdueInstallmentsCount;
  final List<DailySalesPoint> salesLast30Days;
  final List<ExpenseCategoryShare> expenseDistribution;
  final List<RecentTransaction> recentTransactions;
  final List<UpcomingInstallment> upcomingInstallments;
  final double investorBalance;
  final double unpaidInstallmentsBalance;
  final double customerCreditBalance;
  final List<CashBoxSummary> cashBoxes;
  final double bankBalance;
  final double totalInventoryValue;
}

class DailySalesPoint {
  DailySalesPoint({required this.date, required this.amount});

  factory DailySalesPoint.fromJson(Map<String, dynamic> json) {
    return DailySalesPoint(
      date: DateTime.parse(json['date'] as String),
      amount: _toDecimal(json['amount']),
    );
  }

  final DateTime date;
  final double amount;
}

class ExpenseCategoryShare {
  ExpenseCategoryShare({required this.category, required this.amount});

  factory ExpenseCategoryShare.fromJson(Map<String, dynamic> json) {
    return ExpenseCategoryShare(
      category: json['category'] as String? ?? '',
      amount: _toDecimal(json['amount']),
    );
  }

  final String category;
  final double amount;
}

class RecentTransaction {
  RecentTransaction({
    required this.type,
    required this.number,
    required this.party,
    required this.amount,
    required this.date,
  });

  factory RecentTransaction.fromJson(Map<String, dynamic> json) {
    return RecentTransaction(
      type: json['type'] as String? ?? '',
      number: json['number'] as String? ?? '',
      party: json['party'] as String? ?? '',
      amount: _toDecimal(json['amount']),
      date: DateTime.parse(json['date'] as String),
    );
  }

  final String type;
  final String number;
  final String party;
  final double amount;
  final DateTime date;
}

class UpcomingInstallment {
  UpcomingInstallment({
    required this.customerName,
    required this.amount,
    required this.dueDate,
    required this.daysRemaining,
  });

  factory UpcomingInstallment.fromJson(Map<String, dynamic> json) {
    return UpcomingInstallment(
      customerName: json['customerName'] as String? ?? '',
      amount: _toDecimal(json['amount']),
      dueDate: DateTime.parse(json['dueDate'] as String),
      daysRemaining: json['daysRemaining'] as int? ?? 0,
    );
  }

  final String customerName;
  final double amount;
  final DateTime dueDate;
  final int daysRemaining;
}

class CashBoxSummary {
  CashBoxSummary({required this.name, required this.balance});

  factory CashBoxSummary.fromJson(Map<String, dynamic> json) {
    return CashBoxSummary(
      name: json['name'] as String? ?? '',
      balance: _toDecimal(json['balance']),
    );
  }

  final String name;
  final double balance;
}

double _toDecimal(dynamic value) {
  if (value == null) return 0;
  if (value is num) return value.toDouble();
  return double.tryParse(value.toString()) ?? 0;
}
