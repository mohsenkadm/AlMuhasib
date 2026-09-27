import 'package:intl/intl.dart';

import '../models/master_data_models.dart';

final currencyFormat = NumberFormat('#,##0.00', 'ar');
final dateFormat = DateFormat('yyyy/MM/dd', 'ar');
final shortDateFormat = DateFormat('MM/dd', 'ar');

String formatCurrency(num value) => currencyFormat.format(value);

/// Customer outstanding: dual IQD|USD when USD present; else single IQD.
String formatCustomerOutstanding(LookupItem item) {
  final iqd = item.effectiveBalanceIqd;
  final usd = item.effectiveBalanceUsd;
  if (item.hasDualBalanceFields && usd != 0) {
    return '${formatCurrency(iqd)} د.ع | ${formatCurrency(usd)} \$';
  }
  if (item.balance != null || item.balanceIqd != null) {
    return formatCurrency(iqd);
  }
  return formatCurrency(0);
}

String formatDate(DateTime date) => dateFormat.format(date);

/// Parses lookup `extra` (e.g. "IQD"/"USD") into AccountingCurrency int.
int lookupCurrencyCode(String? extra) {
  final code = (extra ?? 'IQD').trim().toUpperCase();
  if (code == 'USD' || code == '1') return 1;
  return 0;
}

String currencyCodeLabel(int currency) => currency == 1 ? 'USD' : 'IQD';

String invoiceTypeLabel(int type) {
  switch (type) {
    case 0:
      return 'شراء';
    case 1:
      return 'بيع';
    case 2:
      return 'قسط';
    case 3:
      return 'مرتجع شراء';
    default:
      return 'فاتورة';
  }
}

String paymentMethodLabel(int method) {
  switch (method) {
    case 0:
      return 'نقدي';
    case 1:
      return 'آجل';
    case 2:
      return 'أقساط';
    default:
      return '—';
  }
}
