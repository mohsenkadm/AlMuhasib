import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';

import '../../../core/constants/app_colors.dart';

String carPaymentStatusLabel(String name) {
  switch (name) {
    case 'Fully Paid':
      return 'car_status_fully_paid'.tr();
    case 'Partially Paid':
      return 'car_status_partial'.tr();
    case 'Unpaid':
      return 'car_status_unpaid'.tr();
    case 'Collected':
      return 'car_kpi_received'.tr();
    case 'Remaining':
      return 'car_remaining'.tr();
    default:
      return name;
  }
}

Color carPaymentStatusColor(String name) {
  switch (name) {
    case 'Fully Paid':
    case 'Collected':
      return AppColors.success;
    case 'Partially Paid':
      return AppColors.warning;
    case 'Unpaid':
    case 'Remaining':
      return AppColors.error;
    default:
      return AppColors.primary;
  }
}
