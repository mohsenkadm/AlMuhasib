import 'package:intl/intl.dart';

final currencyFormat = NumberFormat('#,##0.00', 'ar');
final dateFormat = DateFormat('yyyy/MM/dd', 'ar');
final shortDateFormat = DateFormat('MM/dd', 'ar');

String formatCurrency(num value) => currencyFormat.format(value);

String formatDate(DateTime date) => dateFormat.format(date);

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
