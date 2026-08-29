import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';

String goldInvoiceTypeLabel(int type) {
  return switch (type) {
    0 => 'gold_invoice_type_sale'.tr(),
    1 => 'gold_invoice_type_purchase'.tr(),
    2 => 'gold_invoice_type_exchange'.tr(),
    3 => 'gold_invoice_type_return'.tr(),
    _ => 'gold_invoice'.tr(),
  };
}

String goldInvoiceStatusLabel(int status) {
  return switch (status) {
    0 => 'gold_status_completed'.tr(),
    1 => 'gold_status_open'.tr(),
    2 => 'gold_status_partial'.tr(),
    3 => 'gold_status_cancelled'.tr(),
    _ => '—',
  };
}

Color goldInvoiceStatusColor(int status) {
  return switch (status) {
    0 => const Color(0xFF2E7D32),
    1 => const Color(0xFFE65100),
    2 => const Color(0xFFF9A825),
    3 => const Color(0xFF757575),
    _ => const Color(0xFF757575),
  };
}

String goldPaymentMethodLabel(int method) {
  return switch (method) {
    0 => 'gold_payment_cash'.tr(),
    1 => 'gold_payment_credit'.tr(),
    _ => '—',
  };
}

String goldCurrencyLabel(int currency) {
  return switch (currency) {
    0 => 'د.ع',
    1 => '\$',
    _ => '',
  };
}

String goldKaratLabel(int karatValue, {String? karatName}) {
  if (karatName != null && karatName.isNotEmpty) return karatName;
  return 'عيار $karatValue';
}
