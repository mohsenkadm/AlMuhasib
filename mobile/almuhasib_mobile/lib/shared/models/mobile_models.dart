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
    required this.itemName,
    required this.quantity,
    required this.unitPrice,
    this.discountAmount = 0,
  });

  Map<String, dynamic> toJson() => {
        if (productSyncId != null) 'productSyncId': productSyncId,
        'itemName': itemName,
        'quantity': quantity,
        'unitPrice': unitPrice,
        'discountAmount': discountAmount,
      };

  final String? productSyncId;
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
