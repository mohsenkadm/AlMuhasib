import '../../../shared/models/paged_result.dart';

abstract final class ApplicationSystemType {
  static const accounting = 0;
  static const carContracts = 1;
  static const hotelManagement = 2;
  static const carTrading = 3;
  static const realEstateContracts = 4;
}

class HotelOccupancySummary {
  HotelOccupancySummary({
    required this.totalRooms,
    required this.occupiedRooms,
    required this.availableRooms,
    required this.occupancyRate,
  });

  factory HotelOccupancySummary.fromJson(Map<String, dynamic> json) {
    return HotelOccupancySummary(
      totalRooms: json['totalRooms'] as int? ?? 0,
      occupiedRooms: json['occupiedRooms'] as int? ?? 0,
      availableRooms: json['availableRooms'] as int? ?? 0,
      occupancyRate: (json['occupancyRate'] as num?)?.toDouble() ?? 0,
    );
  }

  final int totalRooms;
  final int occupiedRooms;
  final int availableRooms;
  final double occupancyRate;
}

class HotelDashboardData {
  HotelDashboardData({
    required this.occupancy,
    this.todayArrivals = 0,
    this.todayDepartures = 0,
    this.pendingCheckIns = 0,
    this.inHouseGuests = 0,
    this.todayRevenue = 0,
  });

  factory HotelDashboardData.fromJson(Map<String, dynamic> json) {
    final occupancyJson = json['occupancy'];
    return HotelDashboardData(
      occupancy: occupancyJson is Map<String, dynamic>
          ? HotelOccupancySummary.fromJson(occupancyJson)
          : HotelOccupancySummary.fromJson(json),
      todayArrivals: json['todayArrivals'] as int? ?? 0,
      todayDepartures: json['todayDepartures'] as int? ?? 0,
      pendingCheckIns: json['pendingCheckIns'] as int? ?? 0,
      inHouseGuests: json['inHouseGuests'] as int? ?? 0,
      todayRevenue: (json['todayRevenue'] as num?)?.toDouble() ?? 0,
    );
  }

  final HotelOccupancySummary occupancy;
  final int todayArrivals;
  final int todayDepartures;
  final int pendingCheckIns;
  final int inHouseGuests;
  final double todayRevenue;
}

class HotelRoom {
  HotelRoom({
    required this.syncId,
    required this.roomNumber,
    this.floorName,
    this.roomTypeName,
    required this.status,
    this.notes,
  });

  factory HotelRoom.fromJson(Map<String, dynamic> json) {
    return HotelRoom(
      syncId: json['syncId']?.toString() ?? '',
      roomNumber: json['roomNumber'] as String? ?? '',
      floorName: json['floorName'] as String?,
      roomTypeName: json['roomTypeName'] as String?,
      status: _parseRoomStatus(json['status']),
      notes: json['notes'] as String?,
    );
  }

  final String syncId;
  final String roomNumber;
  final String? floorName;
  final String? roomTypeName;
  final int status;
  final String? notes;
}

class HotelGuest {
  HotelGuest({
    required this.syncId,
    required this.fullName,
    this.idNumber,
    this.phone,
    this.email,
    this.notes,
  });

  factory HotelGuest.fromJson(Map<String, dynamic> json) {
    return HotelGuest(
      syncId: json['syncId']?.toString() ?? '',
      fullName: json['fullName'] as String? ?? '',
      idNumber: json['idNumber'] as String?,
      phone: json['phone'] as String?,
      email: json['email'] as String?,
      notes: json['notes'] as String?,
    );
  }

  final String syncId;
  final String fullName;
  final String? idNumber;
  final String? phone;
  final String? email;
  final String? notes;
}

class HotelGuestUpsertRequest {
  HotelGuestUpsertRequest({
    required this.fullName,
    this.idNumber,
    this.phone,
    this.email,
    this.notes,
  });

  final String fullName;
  final String? idNumber;
  final String? phone;
  final String? email;
  final String? notes;

  Map<String, dynamic> toJson() => {
        'fullName': fullName,
        if (idNumber != null && idNumber!.isNotEmpty) 'idNumber': idNumber,
        if (phone != null && phone!.isNotEmpty) 'phone': phone,
        if (email != null && email!.isNotEmpty) 'email': email,
        if (notes != null && notes!.isNotEmpty) 'notes': notes,
      };
}

class HotelReservation {
  HotelReservation({
    required this.syncId,
    required this.reservationNumber,
    required this.guestName,
    this.roomNumber,
    required this.checkInDate,
    required this.checkOutDate,
    required this.status,
    this.remainingAmount = 0,
    this.totalAmount = 0,
    this.amountPaid = 0,
    this.guestCount = 1,
    this.notes,
    this.guestSyncId,
    this.roomSyncId,
  });

  factory HotelReservation.fromJson(Map<String, dynamic> json) {
    return HotelReservation(
      syncId: json['syncId']?.toString() ?? '',
      reservationNumber: json['reservationNumber'] as String? ?? '',
      guestName: json['guestName'] as String? ?? '',
      roomNumber: json['roomNumber'] as String?,
      checkInDate: DateTime.tryParse(json['checkInDate']?.toString() ?? '') ??
          DateTime.now(),
      checkOutDate: DateTime.tryParse(json['checkOutDate']?.toString() ?? '') ??
          DateTime.now(),
      status: _parseReservationStatus(json['status']),
      remainingAmount: (json['remainingAmount'] as num?)?.toDouble() ?? 0,
      totalAmount: (json['totalAmount'] as num?)?.toDouble() ?? 0,
      amountPaid: (json['amountPaid'] as num?)?.toDouble() ?? 0,
      guestCount: json['guestCount'] as int? ?? 1,
      notes: json['notes'] as String?,
      guestSyncId: json['guestSyncId']?.toString(),
      roomSyncId: json['roomSyncId']?.toString(),
    );
  }

  final String syncId;
  final String reservationNumber;
  final String guestName;
  final String? roomNumber;
  final DateTime checkInDate;
  final DateTime checkOutDate;
  final int status;
  final double remainingAmount;
  final double totalAmount;
  final double amountPaid;
  final int guestCount;
  final String? notes;
  final String? guestSyncId;
  final String? roomSyncId;
}

class HotelFloor {
  HotelFloor({
    required this.syncId,
    required this.name,
    this.sortOrder = 0,
  });

  factory HotelFloor.fromJson(Map<String, dynamic> json) {
    return HotelFloor(
      syncId: json['syncId']?.toString() ?? '',
      name: json['name'] as String? ?? '',
      sortOrder: json['sortOrder'] as int? ?? 0,
    );
  }

  final String syncId;
  final String name;
  final int sortOrder;
}

class HotelRoomType {
  HotelRoomType({
    required this.syncId,
    required this.name,
    this.description,
    this.capacity = 2,
    this.basePrice = 0,
  });

  factory HotelRoomType.fromJson(Map<String, dynamic> json) {
    return HotelRoomType(
      syncId: json['syncId']?.toString() ?? '',
      name: json['name'] as String? ?? '',
      description: json['description'] as String?,
      capacity: json['capacity'] as int? ?? 2,
      basePrice: (json['basePrice'] as num?)?.toDouble() ?? 0,
    );
  }

  final String syncId;
  final String name;
  final String? description;
  final int capacity;
  final double basePrice;
}

class HotelRatePlan {
  HotelRatePlan({
    required this.syncId,
    required this.name,
    this.roomTypeSyncId,
    this.basePrice = 0,
    this.isActive = true,
  });

  factory HotelRatePlan.fromJson(Map<String, dynamic> json) {
    return HotelRatePlan(
      syncId: json['syncId']?.toString() ?? '',
      name: json['name'] as String? ?? '',
      roomTypeSyncId: json['roomTypeSyncId']?.toString(),
      basePrice: (json['basePrice'] as num?)?.toDouble() ?? 0,
      isActive: json['isActive'] as bool? ?? true,
    );
  }

  final String syncId;
  final String name;
  final String? roomTypeSyncId;
  final double basePrice;
  final bool isActive;
}

class HotelCheckInRequest {
  HotelCheckInRequest({
    required this.reservationSyncId,
    this.roomSyncId,
    this.notes,
  });

  final String reservationSyncId;
  final String? roomSyncId;
  final String? notes;

  Map<String, dynamic> toJson() => {
        'reservationSyncId': reservationSyncId,
        if (roomSyncId != null) 'roomSyncId': roomSyncId,
        if (notes != null) 'notes': notes,
      };
}

class HotelCheckOutRequest {
  HotelCheckOutRequest({
    required this.reservationSyncId,
    this.notes,
  });

  final String reservationSyncId;
  final String? notes;

  Map<String, dynamic> toJson() => {
        'reservationSyncId': reservationSyncId,
        if (notes != null) 'notes': notes,
      };
}

class HotelPaymentRequest {
  HotelPaymentRequest({
    required this.reservationSyncId,
    required this.amount,
    this.paymentMethod,
    this.notes,
  });

  final String reservationSyncId;
  final double amount;
  final String? paymentMethod;
  final String? notes;

  Map<String, dynamic> toJson() => {
        'reservationSyncId': reservationSyncId,
        'amount': amount,
        if (paymentMethod != null) 'paymentMethod': paymentMethod,
        if (notes != null) 'notes': notes,
      };
}

typedef HotelGuestPage = PagedResult<HotelGuest>;
typedef HotelReservationPage = PagedResult<HotelReservation>;

int _parseRoomStatus(dynamic value) {
  if (value is int) return value;
  if (value is String) {
    switch (value.toLowerCase()) {
      case 'available':
        return 0;
      case 'occupied':
        return 1;
      case 'dirty':
        return 2;
      case 'maintenance':
        return 3;
      case 'outoforder':
      case 'out_of_order':
        return 4;
    }
  }
  return 0;
}

int _parseReservationStatus(dynamic value) {
  if (value is int) return value;
  if (value is String) {
    switch (value.toLowerCase()) {
      case 'confirmed':
        return 0;
      case 'checkedin':
      case 'checked_in':
        return 1;
      case 'checkedout':
      case 'checked_out':
        return 2;
      case 'cancelled':
        return 3;
      case 'noshow':
      case 'no_show':
        return 4;
    }
  }
  return 0;
}
