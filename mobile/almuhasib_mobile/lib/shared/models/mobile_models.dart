class MobileWriteResponse {
  MobileWriteResponse({
    required this.syncId,
    this.invoiceNumber,
    required this.message,
    this.conflicts = const [],
  });

  factory MobileWriteResponse.fromJson(Map<String, dynamic> json) {
    return MobileWriteResponse(
      syncId: json['syncId']?.toString() ?? '',
      invoiceNumber: json['invoiceNumber'] as String?,
      message: json['message'] as String? ?? '',
      conflicts: (json['conflicts'] as List<dynamic>? ?? [])
          .map((e) => e.toString())
          .toList(),
    );
  }

  final String syncId;
  final String? invoiceNumber;
  final String message;
  final List<String> conflicts;
}

class CreateCustomerRequest {
  CreateCustomerRequest({
    this.syncId,
    required this.name,
    this.phone,
    this.address,
    this.notes,
  });

  Map<String, dynamic> toJson() => {
        if (syncId != null) 'syncId': syncId,
        'name': name,
        if (phone != null) 'phone': phone,
        if (address != null) 'address': address,
        if (notes != null) 'notes': notes,
      };

  final String? syncId;
  final String name;
  final String? phone;
  final String? address;
  final String? notes;
}

class CreateSupplierRequest {
  CreateSupplierRequest({
    this.syncId,
    required this.name,
    this.phone,
    this.address,
    this.notes,
  });

  Map<String, dynamic> toJson() => {
        if (syncId != null) 'syncId': syncId,
        'name': name,
        if (phone != null) 'phone': phone,
        if (address != null) 'address': address,
        if (notes != null) 'notes': notes,
      };

  final String? syncId;
  final String name;
  final String? phone;
  final String? address;
  final String? notes;
}

class CreateProductRequest {
  CreateProductRequest({
    this.syncId,
    required this.name,
    required this.categorySyncId,
    this.barcode,
    this.description,
  });

  Map<String, dynamic> toJson() => {
        if (syncId != null) 'syncId': syncId,
        'name': name,
        'categorySyncId': categorySyncId,
        if (barcode != null) 'barcode': barcode,
        if (description != null) 'description': description,
      };

  final String? syncId;
  final String name;
  final String categorySyncId;
  final String? barcode;
  final String? description;
}

class CreateInvestorRequest {
  CreateInvestorRequest({
    this.syncId,
    required this.name,
    this.phone,
    this.profitPercentage = 0,
    this.openingBalance = 0,
  });

  Map<String, dynamic> toJson() => {
        if (syncId != null) 'syncId': syncId,
        'name': name,
        if (phone != null) 'phone': phone,
        'profitPercentage': profitPercentage,
        'openingBalance': openingBalance,
      };

  final String? syncId;
  final String name;
  final String? phone;
  final double profitPercentage;
  final double openingBalance;
}

class CreateInvoiceItemRequest {
  CreateInvoiceItemRequest({
    this.productSyncId,
    this.pricingTypeSyncId,
    required this.itemName,
    required this.quantity,
    required this.unitPrice,
    this.discountAmount = 0,
  });

  Map<String, dynamic> toJson() => {
        if (productSyncId != null) 'productSyncId': productSyncId,
        if (pricingTypeSyncId != null) 'pricingTypeSyncId': pricingTypeSyncId,
        'itemName': itemName,
        'quantity': quantity,
        'unitPrice': unitPrice,
        'discountAmount': discountAmount,
      };

  final String? productSyncId;
  final String? pricingTypeSyncId;
  final String itemName;
  final double quantity;
  final double unitPrice;
  final double discountAmount;
}

class CreateInstallmentPlanRequest {
  CreateInstallmentPlanRequest({
    required this.numberOfInstallments,
    required this.startDate,
    this.installmentType = 0,
    this.fileNumber,
  });

  Map<String, dynamic> toJson() => {
        'numberOfInstallments': numberOfInstallments,
        'startDate': startDate.toIso8601String(),
        'installmentType': installmentType,
        if (fileNumber != null) 'fileNumber': fileNumber,
      };

  final int numberOfInstallments;
  final DateTime startDate;
  final int installmentType;
  final String? fileNumber;
}

class CreateInvoiceRequest {
  CreateInvoiceRequest({
    required this.invoiceType,
    this.customerSyncId,
    this.supplierSyncId,
    required this.warehouseSyncId,
    required this.paymentMethod,
    this.cashBoxSyncId,
    required this.date,
    this.creditDueDate,
    this.discountAmount = 0,
    this.notes,
    required this.items,
    this.installmentPlan,
  });

  Map<String, dynamic> toJson() => {
        'invoiceType': invoiceType,
        if (customerSyncId != null) 'customerSyncId': customerSyncId,
        if (supplierSyncId != null) 'supplierSyncId': supplierSyncId,
        'warehouseSyncId': warehouseSyncId,
        'paymentMethod': paymentMethod,
        if (cashBoxSyncId != null) 'cashBoxSyncId': cashBoxSyncId,
        'date': date.toIso8601String(),
        if (creditDueDate != null) 'creditDueDate': creditDueDate!.toIso8601String(),
        'discountAmount': discountAmount,
        if (notes != null) 'notes': notes,
        'items': items.map((e) => e.toJson()).toList(),
        if (installmentPlan != null) 'installmentPlan': installmentPlan!.toJson(),
      };

  final int invoiceType;
  final String? customerSyncId;
  final String? supplierSyncId;
  final String warehouseSyncId;
  final int paymentMethod;
  final String? cashBoxSyncId;
  final DateTime date;
  final DateTime? creditDueDate;
  final double discountAmount;
  final String? notes;
  final List<CreateInvoiceItemRequest> items;
  final CreateInstallmentPlanRequest? installmentPlan;
}

class UpsertPricingTypeRequest {
  UpsertPricingTypeRequest({
    this.syncId,
    required this.name,
    this.isDefault = false,
    this.isActive = true,
  });

  Map<String, dynamic> toJson() => {
        if (syncId != null) 'syncId': syncId,
        'name': name,
        'isDefault': isDefault,
        'isActive': isActive,
      };

  final String? syncId;
  final String name;
  final bool isDefault;
  final bool isActive;
}

class UpsertProductPriceRequest {
  UpsertProductPriceRequest({
    this.syncId,
    required this.productSyncId,
    required this.pricingTypeSyncId,
    required this.salePrice,
    required this.purchasePrice,
  });

  Map<String, dynamic> toJson() => {
        if (syncId != null) 'syncId': syncId,
        'productSyncId': productSyncId,
        'pricingTypeSyncId': pricingTypeSyncId,
        'salePrice': salePrice,
        'purchasePrice': purchasePrice,
      };

  final String? syncId;
  final String productSyncId;
  final String pricingTypeSyncId;
  final double salePrice;
  final double purchasePrice;
}

class UpsertCashBoxRequest {
  UpsertCashBoxRequest({
    this.syncId,
    required this.name,
    this.openingBalance = 0,
  });

  Map<String, dynamic> toJson() => {
        if (syncId != null) 'syncId': syncId,
        'name': name,
        'openingBalance': openingBalance,
      };

  final String? syncId;
  final String name;
  final double openingBalance;
}

class UpsertBankAccountRequest {
  UpsertBankAccountRequest({
    this.syncId,
    required this.name,
    this.accountNumber,
    this.openingBalance = 0,
  });

  Map<String, dynamic> toJson() => {
        if (syncId != null) 'syncId': syncId,
        'name': name,
        if (accountNumber != null) 'accountNumber': accountNumber,
        'openingBalance': openingBalance,
      };

  final String? syncId;
  final String name;
  final String? accountNumber;
  final double openingBalance;
}

class UpsertExpenseTypeRequest {
  UpsertExpenseTypeRequest({this.syncId, required this.name});

  Map<String, dynamic> toJson() => {
        if (syncId != null) 'syncId': syncId,
        'name': name,
      };

  final String? syncId;
  final String name;
}

class CreateVoucherRequest {
  CreateVoucherRequest({
    this.syncId,
    required this.voucherType,
    required this.amount,
    this.bankFees = 0,
    this.customerSyncId,
    this.investorSyncId,
    required this.cashBoxSyncId,
    this.bankAccountSyncId,
    required this.date,
    this.notes,
  });

  Map<String, dynamic> toJson() => {
        if (syncId != null) 'syncId': syncId,
        'voucherType': voucherType,
        'amount': amount,
        'bankFees': bankFees,
        if (customerSyncId != null) 'customerSyncId': customerSyncId,
        if (investorSyncId != null) 'investorSyncId': investorSyncId,
        'cashBoxSyncId': cashBoxSyncId,
        if (bankAccountSyncId != null) 'bankAccountSyncId': bankAccountSyncId,
        'date': date.toIso8601String(),
        if (notes != null) 'notes': notes,
      };

  final String? syncId;
  final int voucherType;
  final double amount;
  final double bankFees;
  final String? customerSyncId;
  final String? investorSyncId;
  final String cashBoxSyncId;
  final String? bankAccountSyncId;
  final DateTime date;
  final String? notes;
}

class CreateExpenseRequest {
  CreateExpenseRequest({
    this.syncId,
    required this.expenseTypeSyncId,
    required this.amount,
    required this.date,
    required this.cashBoxSyncId,
    this.notes,
  });

  Map<String, dynamic> toJson() => {
        if (syncId != null) 'syncId': syncId,
        'expenseTypeSyncId': expenseTypeSyncId,
        'amount': amount,
        'date': date.toIso8601String(),
        'cashBoxSyncId': cashBoxSyncId,
        if (notes != null) 'notes': notes,
      };

  final String? syncId;
  final String expenseTypeSyncId;
  final double amount;
  final DateTime date;
  final String cashBoxSyncId;
  final String? notes;
}

class CreateTransferRequest {
  CreateTransferRequest({
    this.syncId,
    required this.fromType,
    required this.fromSyncId,
    required this.toType,
    required this.toSyncId,
    required this.amount,
    required this.date,
    this.notes,
  });

  Map<String, dynamic> toJson() => {
        if (syncId != null) 'syncId': syncId,
        'fromType': fromType,
        'fromSyncId': fromSyncId,
        'toType': toType,
        'toSyncId': toSyncId,
        'amount': amount,
        'date': date.toIso8601String(),
        if (notes != null) 'notes': notes,
      };

  final String? syncId;
  /// 0 = CashBox, 1 = Bank
  final int fromType;
  final String fromSyncId;
  final int toType;
  final String toSyncId;
  final double amount;
  final DateTime date;
  final String? notes;
}

class UpsertWarehouseRequest {
  UpsertWarehouseRequest({
    this.syncId,
    required this.name,
    this.location,
  });

  Map<String, dynamic> toJson() => {
        if (syncId != null) 'syncId': syncId,
        'name': name,
        if (location != null) 'location': location,
      };

  final String? syncId;
  final String name;
  final String? location;
}

class CreateWarehouseTransferItemRequest {
  CreateWarehouseTransferItemRequest({
    required this.productSyncId,
    required this.quantity,
  });

  Map<String, dynamic> toJson() => {
        'productSyncId': productSyncId,
        'quantity': quantity,
      };

  final String productSyncId;
  final double quantity;
}

class CreateWarehouseTransferRequest {
  CreateWarehouseTransferRequest({
    this.syncId,
    required this.fromWarehouseSyncId,
    required this.toWarehouseSyncId,
    required this.date,
    this.notes,
    required this.items,
  });

  Map<String, dynamic> toJson() => {
        if (syncId != null) 'syncId': syncId,
        'fromWarehouseSyncId': fromWarehouseSyncId,
        'toWarehouseSyncId': toWarehouseSyncId,
        'date': date.toIso8601String(),
        if (notes != null) 'notes': notes,
        'items': items.map((e) => e.toJson()).toList(),
      };

  final String? syncId;
  final String fromWarehouseSyncId;
  final String toWarehouseSyncId;
  final DateTime date;
  final String? notes;
  final List<CreateWarehouseTransferItemRequest> items;
}

class StockAdjustmentItemRequest {
  StockAdjustmentItemRequest({
    required this.productSyncId,
    required this.newQuantity,
  });

  Map<String, dynamic> toJson() => {
        'productSyncId': productSyncId,
        'newQuantity': newQuantity,
      };

  final String productSyncId;
  final double newQuantity;
}

class CreateStockAdjustmentRequest {
  CreateStockAdjustmentRequest({
    required this.warehouseSyncId,
    required this.items,
    this.notes,
  });

  Map<String, dynamic> toJson() => {
        'warehouseSyncId': warehouseSyncId,
        'items': items.map((e) => e.toJson()).toList(),
        if (notes != null) 'notes': notes,
      };

  final String warehouseSyncId;
  final List<StockAdjustmentItemRequest> items;
  final String? notes;
}

class PayInstallmentRequest {
  PayInstallmentRequest({
    required this.amount,
    required this.cashBoxSyncId,
    this.paymentDate,
    this.notes,
  });

  Map<String, dynamic> toJson() => {
        'amount': amount,
        'cashBoxSyncId': cashBoxSyncId,
        if (paymentDate != null) 'paymentDate': paymentDate!.toIso8601String(),
        if (notes != null) 'notes': notes,
      };

  final double amount;
  final String cashBoxSyncId;
  final DateTime? paymentDate;
  final String? notes;
}

double _mNum(dynamic v) {
  if (v == null) return 0;
  if (v is num) return v.toDouble();
  return double.tryParse(v.toString()) ?? 0;
}

DateTime? _mDate(dynamic v) {
  if (v == null) return null;
  if (v is DateTime) return v;
  return DateTime.tryParse(v.toString());
}

class VoucherListItem {
  VoucherListItem({
    required this.syncId,
    required this.voucherNumber,
    required this.voucherType,
    required this.amount,
    this.bankFees = 0,
    this.customerSyncId,
    this.customerName,
    this.investorSyncId,
    this.investorName,
    required this.cashBoxSyncId,
    required this.cashBoxName,
    this.bankAccountSyncId,
    this.bankAccountName,
    required this.date,
    this.notes,
  });

  factory VoucherListItem.fromJson(Map<String, dynamic> json) {
    return VoucherListItem(
      syncId: json['syncId']?.toString() ?? '',
      voucherNumber: json['voucherNumber'] as String? ?? '',
      voucherType: json['voucherType'] as int? ?? 0,
      amount: _mNum(json['amount']),
      bankFees: _mNum(json['bankFees']),
      customerSyncId: json['customerSyncId']?.toString(),
      customerName: json['customerName'] as String?,
      investorSyncId: json['investorSyncId']?.toString(),
      investorName: json['investorName'] as String?,
      cashBoxSyncId: json['cashBoxSyncId']?.toString() ?? '',
      cashBoxName: json['cashBoxName'] as String? ?? '',
      bankAccountSyncId: json['bankAccountSyncId']?.toString(),
      bankAccountName: json['bankAccountName'] as String?,
      date: _mDate(json['date']) ?? DateTime.now(),
      notes: json['notes'] as String?,
    );
  }

  final String syncId;
  final String voucherNumber;
  final int voucherType;
  final double amount;
  final double bankFees;
  final String? customerSyncId;
  final String? customerName;
  final String? investorSyncId;
  final String? investorName;
  final String cashBoxSyncId;
  final String cashBoxName;
  final String? bankAccountSyncId;
  final String? bankAccountName;
  final DateTime date;
  final String? notes;
}

class ExpenseListItem {
  ExpenseListItem({
    required this.syncId,
    required this.expenseTypeSyncId,
    required this.expenseTypeName,
    required this.amount,
    required this.date,
    required this.cashBoxSyncId,
    required this.cashBoxName,
    this.notes,
  });

  factory ExpenseListItem.fromJson(Map<String, dynamic> json) {
    return ExpenseListItem(
      syncId: json['syncId']?.toString() ?? '',
      expenseTypeSyncId: json['expenseTypeSyncId']?.toString() ?? '',
      expenseTypeName: json['expenseTypeName'] as String? ?? '',
      amount: _mNum(json['amount']),
      date: _mDate(json['date']) ?? DateTime.now(),
      cashBoxSyncId: json['cashBoxSyncId']?.toString() ?? '',
      cashBoxName: json['cashBoxName'] as String? ?? '',
      notes: json['notes'] as String?,
    );
  }

  final String syncId;
  final String expenseTypeSyncId;
  final String expenseTypeName;
  final double amount;
  final DateTime date;
  final String cashBoxSyncId;
  final String cashBoxName;
  final String? notes;
}

class TransferListItem {
  TransferListItem({
    required this.syncId,
    required this.fromType,
    this.fromSyncId,
    required this.fromName,
    required this.toType,
    this.toSyncId,
    required this.toName,
    required this.amount,
    required this.date,
    this.notes,
  });

  factory TransferListItem.fromJson(Map<String, dynamic> json) {
    return TransferListItem(
      syncId: json['syncId']?.toString() ?? '',
      fromType: json['fromType'] as int? ?? 0,
      fromSyncId: json['fromSyncId']?.toString(),
      fromName: json['fromName'] as String? ?? '',
      toType: json['toType'] as int? ?? 0,
      toSyncId: json['toSyncId']?.toString(),
      toName: json['toName'] as String? ?? '',
      amount: _mNum(json['amount']),
      date: _mDate(json['date']) ?? DateTime.now(),
      notes: json['notes'] as String?,
    );
  }

  final String syncId;
  final int fromType;
  final String? fromSyncId;
  final String fromName;
  final int toType;
  final String? toSyncId;
  final String toName;
  final double amount;
  final DateTime date;
  final String? notes;
}

class WarehouseStockListItem {
  WarehouseStockListItem({
    required this.syncId,
    required this.warehouseSyncId,
    required this.warehouseName,
    required this.productSyncId,
    required this.productName,
    required this.quantity,
    this.openingQuantity = 0,
    this.unitCost = 0,
  });

  factory WarehouseStockListItem.fromJson(Map<String, dynamic> json) {
    return WarehouseStockListItem(
      syncId: json['syncId']?.toString() ?? '',
      warehouseSyncId: json['warehouseSyncId']?.toString() ?? '',
      warehouseName: json['warehouseName'] as String? ?? '',
      productSyncId: json['productSyncId']?.toString() ?? '',
      productName: json['productName'] as String? ?? '',
      quantity: _mNum(json['quantity']),
      openingQuantity: _mNum(json['openingQuantity']),
      unitCost: _mNum(json['unitCost']),
    );
  }

  final String syncId;
  final String warehouseSyncId;
  final String warehouseName;
  final String productSyncId;
  final String productName;
  final double quantity;
  final double openingQuantity;
  final double unitCost;
}

class WarehouseTransferItemListItem {
  WarehouseTransferItemListItem({
    required this.syncId,
    required this.productSyncId,
    required this.productName,
    required this.quantity,
  });

  factory WarehouseTransferItemListItem.fromJson(Map<String, dynamic> json) {
    return WarehouseTransferItemListItem(
      syncId: json['syncId']?.toString() ?? '',
      productSyncId: json['productSyncId']?.toString() ?? '',
      productName: json['productName'] as String? ?? '',
      quantity: _mNum(json['quantity']),
    );
  }

  final String syncId;
  final String productSyncId;
  final String productName;
  final double quantity;
}

class WarehouseTransferListItem {
  WarehouseTransferListItem({
    required this.syncId,
    required this.transferNumber,
    required this.fromWarehouseSyncId,
    required this.fromWarehouseName,
    required this.toWarehouseSyncId,
    required this.toWarehouseName,
    required this.date,
    this.notes,
    this.items = const [],
  });

  factory WarehouseTransferListItem.fromJson(Map<String, dynamic> json) {
    return WarehouseTransferListItem(
      syncId: json['syncId']?.toString() ?? '',
      transferNumber: json['transferNumber'] as String? ?? '',
      fromWarehouseSyncId: json['fromWarehouseSyncId']?.toString() ?? '',
      fromWarehouseName: json['fromWarehouseName'] as String? ?? '',
      toWarehouseSyncId: json['toWarehouseSyncId']?.toString() ?? '',
      toWarehouseName: json['toWarehouseName'] as String? ?? '',
      date: _mDate(json['date']) ?? DateTime.now(),
      notes: json['notes'] as String?,
      items: (json['items'] as List<dynamic>? ?? [])
          .map((e) =>
              WarehouseTransferItemListItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  final String syncId;
  final String transferNumber;
  final String fromWarehouseSyncId;
  final String fromWarehouseName;
  final String toWarehouseSyncId;
  final String toWarehouseName;
  final DateTime date;
  final String? notes;
  final List<WarehouseTransferItemListItem> items;
}

class InstallmentListItem {
  InstallmentListItem({
    required this.syncId,
    required this.planSyncId,
    required this.customerSyncId,
    required this.customerName,
    this.fileNumber,
    required this.dueDate,
    required this.amount,
    required this.paidAmount,
    required this.remainingAmount,
    required this.status,
    this.paymentDate,
    this.cashBoxSyncId,
    this.cashBoxName,
  });

  factory InstallmentListItem.fromJson(Map<String, dynamic> json) {
    return InstallmentListItem(
      syncId: json['syncId']?.toString() ?? '',
      planSyncId: json['planSyncId']?.toString() ?? '',
      customerSyncId: json['customerSyncId']?.toString() ?? '',
      customerName: json['customerName'] as String? ?? '',
      fileNumber: json['fileNumber'] as String?,
      dueDate: _mDate(json['dueDate']) ?? DateTime.now(),
      amount: _mNum(json['amount']),
      paidAmount: _mNum(json['paidAmount']),
      remainingAmount: _mNum(json['remainingAmount']),
      status: json['status'] as int? ?? 0,
      paymentDate: _mDate(json['paymentDate']),
      cashBoxSyncId: json['cashBoxSyncId']?.toString(),
      cashBoxName: json['cashBoxName'] as String?,
    );
  }

  final String syncId;
  final String planSyncId;
  final String customerSyncId;
  final String customerName;
  final String? fileNumber;
  final DateTime dueDate;
  final double amount;
  final double paidAmount;
  final double remainingAmount;
  final int status;
  final DateTime? paymentDate;
  final String? cashBoxSyncId;
  final String? cashBoxName;
}

class InstallmentPlanDetail {
  InstallmentPlanDetail({
    required this.syncId,
    required this.invoiceSyncId,
    required this.invoiceNumber,
    required this.customerSyncId,
    required this.customerName,
    this.fileNumber,
    required this.totalAmount,
    required this.numberOfInstallments,
    required this.installmentAmount,
    required this.startDate,
    required this.installmentType,
    this.companyFeePercentage = 0,
    this.companyFeeAmount = 0,
    this.installments = const [],
  });

  factory InstallmentPlanDetail.fromJson(Map<String, dynamic> json) {
    return InstallmentPlanDetail(
      syncId: json['syncId']?.toString() ?? '',
      invoiceSyncId: json['invoiceSyncId']?.toString() ?? '',
      invoiceNumber: json['invoiceNumber'] as String? ?? '',
      customerSyncId: json['customerSyncId']?.toString() ?? '',
      customerName: json['customerName'] as String? ?? '',
      fileNumber: json['fileNumber'] as String?,
      totalAmount: _mNum(json['totalAmount']),
      numberOfInstallments: json['numberOfInstallments'] as int? ?? 0,
      installmentAmount: _mNum(json['installmentAmount']),
      startDate: _mDate(json['startDate']) ?? DateTime.now(),
      installmentType: json['installmentType'] as int? ?? 0,
      companyFeePercentage: _mNum(json['companyFeePercentage']),
      companyFeeAmount: _mNum(json['companyFeeAmount']),
      installments: (json['installments'] as List<dynamic>? ?? [])
          .map((e) => InstallmentListItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  final String syncId;
  final String invoiceSyncId;
  final String invoiceNumber;
  final String customerSyncId;
  final String customerName;
  final String? fileNumber;
  final double totalAmount;
  final int numberOfInstallments;
  final double installmentAmount;
  final DateTime startDate;
  final int installmentType;
  final double companyFeePercentage;
  final double companyFeeAmount;
  final List<InstallmentListItem> installments;
}
