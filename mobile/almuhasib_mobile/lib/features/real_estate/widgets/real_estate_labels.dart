import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';

import '../../../core/constants/app_colors.dart';

String realEstatePaymentStatusLabel(String name) {
  switch (name) {
    case 'Fully Paid':
      return 'real_estate_status_fully_paid'.tr();
    case 'Partially Paid':
      return 'real_estate_status_partial'.tr();
    case 'Unpaid':
      return 'real_estate_status_unpaid'.tr();
    case 'Collected':
      return 'real_estate_kpi_received'.tr();
    case 'Remaining':
      return 'real_estate_remaining'.tr();
    case 'Sale':
    case '0':
      return 'real_estate_type_sale'.tr();
    case 'Purchase':
    case '1':
      return 'real_estate_type_purchase'.tr();
    case 'House':
      return 'real_estate_property_house'.tr();
    case 'Land':
      return 'real_estate_property_land'.tr();
    case 'Other':
    case '2':
      return 'real_estate_property_other'.tr();
    case 'Cash':
      return 'real_estate_payment_cash'.tr();
    case 'Credit':
      return 'real_estate_payment_credit'.tr();
    case 'Buyer':
      return 'real_estate_debtor_buyer'.tr();
    case 'Seller':
      return 'real_estate_debtor_seller'.tr();
    case 'None':
      return 'real_estate_debtor_none'.tr();
    default:
      return name;
  }
}

Color realEstatePaymentStatusColor(String name) {
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

String realEstateContractTypeLabel(int value) {
  return switch (value) {
    1 => 'real_estate_type_purchase'.tr(),
    _ => 'real_estate_type_sale'.tr(),
  };
}

String realEstatePropertyTypeLabel(int value) {
  return switch (value) {
    1 => 'real_estate_property_land'.tr(),
    2 => 'real_estate_property_other'.tr(),
    _ => 'real_estate_property_house'.tr(),
  };
}

String realEstatePaymentModeLabel(int value) {
  return switch (value) {
    1 => 'real_estate_payment_credit'.tr(),
    _ => 'real_estate_payment_cash'.tr(),
  };
}

String realEstateDebtorPartyLabel(int value) {
  return switch (value) {
    1 => 'real_estate_debtor_buyer'.tr(),
    2 => 'real_estate_debtor_seller'.tr(),
    _ => 'real_estate_debtor_none'.tr(),
  };
}
