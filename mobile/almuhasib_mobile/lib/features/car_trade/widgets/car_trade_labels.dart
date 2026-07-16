import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';

import '../../../core/constants/app_colors.dart';

String carTradeTypeLabel(String tradeType) {
  switch (tradeType.toLowerCase()) {
    case 'buy':
      return 'car_trade_type_buy'.tr();
    case 'sell':
      return 'car_trade_type_sell'.tr();
    default:
      return tradeType;
  }
}

String carTradePaymentStatusLabel(String name) {
  switch (name) {
    case 'Fully Paid':
      return 'car_trade_status_fully_paid'.tr();
    case 'Partially Paid':
      return 'car_trade_status_partial'.tr();
    case 'Unpaid':
      return 'car_trade_status_unpaid'.tr();
    case 'SellerDebt':
      return 'car_trade_seller_debt'.tr();
    case 'BuyerDebt':
      return 'car_trade_buyer_debt'.tr();
    default:
      return name;
  }
}

Color carTradePaymentStatusColor(String name) {
  switch (name) {
    case 'Fully Paid':
      return AppColors.success;
    case 'Partially Paid':
      return AppColors.warning;
    case 'Unpaid':
      return AppColors.error;
    case 'SellerDebt':
      return AppColors.moduleOrange;
    case 'BuyerDebt':
      return AppColors.moduleCyan;
    default:
      return AppColors.primary;
  }
}
