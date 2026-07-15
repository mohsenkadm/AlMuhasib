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

class ProductPriceLookupItem {
  ProductPriceLookupItem({
    required this.syncId,
    required this.productSyncId,
    required this.productName,
    required this.pricingTypeSyncId,
    required this.pricingTypeName,
    this.isDefaultPricingType = false,
    required this.salePrice,
    required this.purchasePrice,
  });

  factory ProductPriceLookupItem.fromJson(Map<String, dynamic> json) {
    return ProductPriceLookupItem(
      syncId: json['syncId']?.toString() ?? '',
      productSyncId: json['productSyncId']?.toString() ?? '',
      productName: json['productName'] as String? ?? '',
      pricingTypeSyncId: json['pricingTypeSyncId']?.toString() ?? '',
      pricingTypeName: json['pricingTypeName'] as String? ?? '',
      isDefaultPricingType: json['isDefaultPricingType'] as bool? ?? false,
      salePrice: _num(json['salePrice']),
      purchasePrice: _num(json['purchasePrice']),
    );
  }

  final String syncId;
  final String productSyncId;
  final String productName;
  final String pricingTypeSyncId;
  final String pricingTypeName;
  final bool isDefaultPricingType;
  final double salePrice;
  final double purchasePrice;
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
    this.prices = const [],
  });

  factory ProductLookupItem.fromJson(Map<String, dynamic> json) {
    final syncId = json['syncId']?.toString() ?? '';
    final name = json['name'] as String? ?? '';
    return ProductLookupItem(
      id: json['id'] as int? ?? 0,
      syncId: syncId,
      name: name,
      extra: json['extra'] as String?,
      barcode: json['barcode'] as String?,
      categorySyncId: json['categorySyncId'] as String? ?? '',
      categoryName: json['categoryName'] as String? ?? '',
      prices: (json['prices'] as List<dynamic>? ?? []).map((e) {
        final map = Map<String, dynamic>.from(e as Map);
        map.putIfAbsent('productSyncId', () => syncId);
        map.putIfAbsent('productName', () => name);
        return ProductPriceLookupItem.fromJson(map);
      }).toList(),
    );
  }

  final String? barcode;
  final String categorySyncId;
  final String categoryName;
  final List<ProductPriceLookupItem> prices;
}

class PricingTypeLookupItem extends LookupItem {
  PricingTypeLookupItem({
    required super.id,
    required super.syncId,
    required super.name,
    super.extra,
    this.isDefault = false,
    this.isActive = true,
  });

  factory PricingTypeLookupItem.fromJson(Map<String, dynamic> json) {
    return PricingTypeLookupItem(
      id: json['id'] as int? ?? 0,
      syncId: json['syncId']?.toString() ?? '',
      name: json['name'] as String? ?? '',
      extra: json['extra'] as String?,
      isDefault: json['isDefault'] as bool? ?? false,
      isActive: json['isActive'] as bool? ?? true,
    );
  }

  final bool isDefault;
  final bool isActive;
}

class BusinessSettings {
  BusinessSettings({
    required this.syncId,
    required this.productPricingEnabled,
    required this.updateProductPriceOnPurchase,
  });

  factory BusinessSettings.fromJson(Map<String, dynamic> json) {
    return BusinessSettings(
      syncId: json['syncId']?.toString() ?? '',
      productPricingEnabled: json['productPricingEnabled'] as bool? ?? false,
      updateProductPriceOnPurchase:
          json['updateProductPriceOnPurchase'] as bool? ?? false,
    );
  }

  final String syncId;
  final bool productPricingEnabled;
  final bool updateProductPriceOnPurchase;
}

/// Alias kept for clarity in pricing list/form screens.
typedef ProductPrice = ProductPriceLookupItem;
typedef PricingType = PricingTypeLookupItem;

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
