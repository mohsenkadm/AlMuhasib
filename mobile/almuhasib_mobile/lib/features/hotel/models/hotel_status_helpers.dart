import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';

import '../../../core/constants/app_colors.dart';

abstract final class HotelRoomStatus {
  static const available = 0;
  static const occupied = 1;
  static const dirty = 2;
  static const maintenance = 3;
  static const outOfOrder = 4;
}

abstract final class HotelReservationStatus {
  static const confirmed = 0;
  static const checkedIn = 1;
  static const checkedOut = 2;
  static const cancelled = 3;
  static const noShow = 4;
}

String hotelRoomStatusLabel(int status) {
  switch (status) {
    case HotelRoomStatus.available:
      return 'hotel_room_available'.tr();
    case HotelRoomStatus.occupied:
      return 'hotel_room_occupied'.tr();
    case HotelRoomStatus.dirty:
      return 'hotel_room_dirty'.tr();
    case HotelRoomStatus.maintenance:
      return 'hotel_room_maintenance'.tr();
    case HotelRoomStatus.outOfOrder:
      return 'hotel_room_out_of_order'.tr();
    default:
      return 'hotel_room_available'.tr();
  }
}

Color hotelRoomStatusColor(int status) {
  switch (status) {
    case HotelRoomStatus.available:
      return AppColors.success;
    case HotelRoomStatus.occupied:
      return AppColors.primaryLight;
    case HotelRoomStatus.dirty:
      return AppColors.warning;
    case HotelRoomStatus.maintenance:
      return AppColors.accent;
    case HotelRoomStatus.outOfOrder:
      return AppColors.error;
    default:
      return AppColors.textMuted;
  }
}

String hotelReservationStatusLabel(int status) {
  switch (status) {
    case HotelReservationStatus.confirmed:
      return 'hotel_status_confirmed'.tr();
    case HotelReservationStatus.checkedIn:
      return 'hotel_status_checked_in'.tr();
    case HotelReservationStatus.checkedOut:
      return 'hotel_status_checked_out'.tr();
    case HotelReservationStatus.cancelled:
      return 'hotel_status_cancelled'.tr();
    case HotelReservationStatus.noShow:
      return 'hotel_status_no_show'.tr();
    default:
      return 'hotel_status_confirmed'.tr();
  }
}

Color hotelReservationStatusColor(int status) {
  switch (status) {
    case HotelReservationStatus.confirmed:
      return AppColors.accent;
    case HotelReservationStatus.checkedIn:
      return AppColors.success;
    case HotelReservationStatus.checkedOut:
      return AppColors.textMuted;
    case HotelReservationStatus.cancelled:
      return AppColors.error;
    case HotelReservationStatus.noShow:
      return AppColors.warning;
    default:
      return AppColors.primaryLight;
  }
}
