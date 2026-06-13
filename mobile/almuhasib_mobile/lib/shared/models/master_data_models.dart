class LookupItem {
  LookupItem({
    required this.id,
    required this.syncId,
    required this.name,
    this.extra,
  });

  factory LookupItem.fromJson(Map<String, dynamic> json) {
    return LookupItem(
      id: json['id'] as int? ?? 0,
      syncId: json['syncId']?.toString() ?? '',
      name: json['name'] as String? ?? '',
      extra: json['extra'] as String?,
    );
  }

  final int id;
  final String syncId;
  final String name;
  final String? extra;
}

class ProductLookupItem extends LookupItem {
  ProductLookupItem({
    required super.id,
    required super.syncId,
    required super.name,
    super.extra,
    this.barcode,
    required this.categorySyncId,
    required this.categoryName,
  });

  factory ProductLookupItem.fromJson(Map<String, dynamic> json) {
    return ProductLookupItem(
      id: json['id'] as int? ?? 0,
      syncId: json['syncId']?.toString() ?? '',
      name: json['name'] as String? ?? '',
      extra: json['extra'] as String?,
      barcode: json['barcode'] as String?,
      categorySyncId: json['categorySyncId'] as String? ?? '',
      categoryName: json['categoryName'] as String? ?? '',
    );
  }

  final String? barcode;
  final String categorySyncId;
  final String categoryName;
}

class InvoiceDetailResponse {
  InvoiceDetailResponse({
    required this.id,
    required this.syncId,
    required this.invoiceNumber,
    required this.invoiceType,
    this.customerName,
    this.supplierName,
    this.warehouseName,
    required this.paymentMethod,
    required this.netAmount,
    required this.date,
    required this.paidAmount,
    required this.remainingAmount,
    required this.items,
  });

  factory InvoiceDetailResponse.fromJson(Map<String, dynamic> json) {
    return InvoiceDetailResponse(
      id: json['id'] as int? ?? 0,
      syncId: json['syncId']?.toString() ?? '',
      invoiceNumber: json['invoiceNumber'] as String? ?? '',
      invoiceType: json['invoiceType'] as int? ?? 0,
      customerName: json['customerName'] as String?,
      supplierName: json['supplierName'] as String?,
      warehouseName: json['warehouseName'] as String?,
      paymentMethod: json['paymentMethod'] as int? ?? 0,
      netAmount: _num(json['netAmount']),
      date: DateTime.parse(json['date'] as String),
      paidAmount: _num(json['paidAmount']),
      remainingAmount: _num(json['remainingAmount']),
      items: (json['items'] as List<dynamic>? ?? [])
          .map((e) => InvoiceItemDetail.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  final int id;
  final String syncId;
  final String invoiceNumber;
  final int invoiceType;
  final String? customerName;
  final String? supplierName;
  final String? warehouseName;
  final int paymentMethod;
  final double netAmount;
  final DateTime date;
  final double paidAmount;
  final double remainingAmount;
  final List<InvoiceItemDetail> items;
}

class InvoiceItemDetail {
  InvoiceItemDetail({
    required this.syncId,
    required this.itemName,
    required this.quantity,
    required this.unitPrice,
    required this.totalPrice,
  });

  factory InvoiceItemDetail.fromJson(Map<String, dynamic> json) {
    return InvoiceItemDetail(
      syncId: json['syncId']?.toString() ?? '',
      itemName: json['itemName'] as String? ?? '',
      quantity: _num(json['quantity']),
      unitPrice: _num(json['unitPrice']),
      totalPrice: _num(json['totalPrice']),
    );
  }

  final String syncId;
  final String itemName;
  final double quantity;
  final double unitPrice;
  final double totalPrice;
}

double _num(dynamic value) {
  if (value == null) return 0;
  if (value is num) return value.toDouble();
  return double.tryParse(value.toString()) ?? 0;
}
