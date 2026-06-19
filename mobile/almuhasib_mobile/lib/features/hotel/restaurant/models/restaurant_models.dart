class RestaurantCategory {
  const RestaurantCategory({
    required this.syncId,
    required this.name,
    required this.colorHex,
    required this.sortOrder,
  });

  final String syncId;
  final String name;
  final String colorHex;
  final int sortOrder;

  factory RestaurantCategory.fromJson(Map<String, dynamic> json) => RestaurantCategory(
        syncId: json['syncId'] as String? ?? '',
        name: json['name'] as String? ?? '',
        colorHex: json['colorHex'] as String? ?? '#00897B',
        sortOrder: json['sortOrder'] as int? ?? 0,
      );
}

class RestaurantMenuItem {
  const RestaurantMenuItem({
    required this.syncId,
    required this.categorySyncId,
    required this.name,
    required this.salePrice,
    this.barcode,
  });

  final String syncId;
  final String categorySyncId;
  final String name;
  final double salePrice;
  final String? barcode;

  factory RestaurantMenuItem.fromJson(Map<String, dynamic> json) => RestaurantMenuItem(
        syncId: json['syncId'] as String? ?? '',
        categorySyncId: json['categorySyncId'] as String? ?? '',
        name: json['name'] as String? ?? '',
        salePrice: (json['salePrice'] as num?)?.toDouble() ?? 0,
        barcode: json['barcode'] as String?,
      );
}

class RestaurantMenuData {
  const RestaurantMenuData({required this.categories, required this.items});

  final List<RestaurantCategory> categories;
  final List<RestaurantMenuItem> items;

  factory RestaurantMenuData.fromJson(Map<String, dynamic> json) => RestaurantMenuData(
        categories: (json['categories'] as List<dynamic>? ?? [])
            .map((e) => RestaurantCategory.fromJson(e as Map<String, dynamic>))
            .toList(),
        items: (json['items'] as List<dynamic>? ?? [])
            .map((e) => RestaurantMenuItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class RestaurantTable {
  const RestaurantTable({
    required this.syncId,
    required this.tableNumber,
    required this.capacity,
    required this.status,
  });

  final String syncId;
  final String tableNumber;
  final int capacity;
  final String status;

  factory RestaurantTable.fromJson(Map<String, dynamic> json) => RestaurantTable(
        syncId: json['syncId'] as String? ?? '',
        tableNumber: json['tableNumber'] as String? ?? '',
        capacity: json['capacity'] as int? ?? 4,
        status: json['status'] as String? ?? 'Available',
      );

  bool get isOccupied => status == 'Occupied';
}

class RestaurantOrder {
  const RestaurantOrder({
    required this.syncId,
    required this.orderNumber,
    required this.orderType,
    required this.status,
    required this.totalAmount,
    required this.orderDate,
  });

  final String syncId;
  final String orderNumber;
  final int orderType;
  final int status;
  final double totalAmount;
  final DateTime orderDate;

  factory RestaurantOrder.fromJson(Map<String, dynamic> json) => RestaurantOrder(
        syncId: json['syncId'] as String? ?? '',
        orderNumber: json['orderNumber'] as String? ?? '',
        orderType: json['orderType'] as int? ?? 0,
        status: json['status'] as int? ?? 0,
        totalAmount: (json['totalAmount'] as num?)?.toDouble() ?? 0,
        orderDate: DateTime.tryParse(json['orderDate'] as String? ?? '') ?? DateTime.now(),
      );
}

class RestaurantProfitSummary {
  const RestaurantProfitSummary({
    required this.revenue,
    required this.cogs,
    required this.grossProfit,
    required this.marginPercent,
    required this.orderCount,
  });

  final double revenue;
  final double cogs;
  final double grossProfit;
  final double marginPercent;
  final int orderCount;

  factory RestaurantProfitSummary.fromJson(Map<String, dynamic> json) => RestaurantProfitSummary(
        revenue: (json['revenue'] as num?)?.toDouble() ?? 0,
        cogs: (json['cogs'] as num?)?.toDouble() ?? 0,
        grossProfit: (json['grossProfit'] as num?)?.toDouble() ?? 0,
        marginPercent: (json['marginPercent'] as num?)?.toDouble() ?? 0,
        orderCount: json['orderCount'] as int? ?? 0,
      );
}

class RestaurantStockAlert {
  const RestaurantStockAlert({
    required this.syncId,
    required this.name,
    required this.quantity,
    required this.minQuantity,
    required this.unit,
  });

  final String syncId;
  final String name;
  final double quantity;
  final double minQuantity;
  final String unit;

  factory RestaurantStockAlert.fromJson(Map<String, dynamic> json) => RestaurantStockAlert(
        syncId: json['syncId'] as String? ?? '',
        name: json['name'] as String? ?? '',
        quantity: (json['quantity'] as num?)?.toDouble() ?? 0,
        minQuantity: (json['minQuantity'] as num?)?.toDouble() ?? 0,
        unit: json['unit'] as String? ?? '',
      );
}
