import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/theme/system_themes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/gold_sale_detail_controller.dart';
import '../widgets/gold_kpi_card.dart';
import '../widgets/gold_labels.dart';

class GoldSaleDetailScreen extends GetView<GoldSaleDetailController> {
  const GoldSaleDetailScreen({super.key, required this.invoiceId});

  @override
  final String? tag = 'gold_sale_detail';

  final int invoiceId;

  @override
  Widget build(BuildContext context) {
    return Obx(() {
      if (controller.isLoading.value && controller.invoice.value == null) {
        return const Scaffold(
          body: Center(child: CircularProgressIndicator()),
        );
      }
      if (controller.error.value != null && controller.invoice.value == null) {
        return Scaffold(
          appBar: AppBar(title: Text('gold_sale_detail'.tr())),
          body: ErrorStateWidget(
            message: AppExceptionHandler.messageFor(controller.error.value),
            onRetry: controller.load,
          ),
        );
      }

      final inv = controller.invoice.value!;
      return Scaffold(
        appBar: AppBar(title: Text(inv.invoiceNumber)),
        body: RefreshIndicator(
          onRefresh: controller.load,
          child: ListView(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 40),
            children: [
              GoldHeroBanner(
                title: goldInvoiceTypeLabel(inv.invoiceType),
                value: '${formatCurrency(inv.totalAmountIqd)} د.ع',
                subtitle:
                    '${formatCurrency(inv.totalAmountUsd)} \$ • ${goldInvoiceStatusLabel(inv.status)}',
              ),
              const SizedBox(height: 16),
              _InfoRow(
                label: 'gold_customer'.tr(),
                value: inv.customerName.isEmpty ? '—' : inv.customerName,
              ),
              if (inv.customerPhone.isNotEmpty)
                _InfoRow(
                  label: 'phone'.tr(),
                  value: inv.customerPhone,
                ),
              _InfoRow(
                label: 'date'.tr(),
                value: formatDate(inv.invoiceDate),
              ),
              _InfoRow(
                label: 'gold_payment_method'.tr(),
                value: goldPaymentMethodLabel(inv.paymentMethod),
              ),
              _InfoRow(
                label: 'gold_weight'.tr(),
                value: '${formatCurrency(inv.totalWeightGrams)} غ',
              ),
              _InfoRow(
                label: 'gold_paid'.tr(),
                value: formatCurrency(inv.paidAmount),
              ),
              if (inv.remainingAmount > 0)
                _InfoRow(
                  label: 'gold_remaining'.tr(),
                  value: formatCurrency(inv.remainingAmount),
                  highlight: true,
                ),
              if (inv.notes.isNotEmpty)
                _InfoRow(label: 'notes'.tr(), value: inv.notes),
              if (inv.lines.isNotEmpty) ...[
                const SizedBox(height: 20),
                GoldSectionHeader(title: 'gold_invoice_lines'.tr()),
                const SizedBox(height: 8),
                ...inv.lines.map(
                  (line) => Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: AppEntityCard(
                      title: line.description.isEmpty
                          ? goldKaratLabel(
                              line.karatValue,
                              karatName: line.karatName,
                            )
                          : line.description,
                      subtitle:
                          '${formatCurrency(line.weightGrams)} غ • ${goldKaratLabel(line.karatValue, karatName: line.karatName)}',
                      trailing: Text(
                        formatCurrency(line.lineTotal),
                        style: const TextStyle(fontWeight: FontWeight.w800),
                      ),
                    ),
                  ),
                ),
              ],
              if (inv.payments.isNotEmpty) ...[
                const SizedBox(height: 20),
                GoldSectionHeader(title: 'gold_payment_history'.tr()),
                const SizedBox(height: 8),
                ...inv.payments.map(
                  (p) => ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: const Icon(
                      Icons.payments_outlined,
                      color: SystemThemes.goldPrimary,
                    ),
                    title: Text(formatCurrency(p.amount)),
                    subtitle: Text(formatDate(p.paymentDate)),
                    trailing: Text(goldCurrencyLabel(p.currency)),
                  ),
                ),
              ],
            ],
          ),
        ),
      );
    });
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({
    required this.label,
    required this.value,
    this.highlight = false,
  });

  final String label;
  final String value;
  final bool highlight;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Row(
        children: [
          Expanded(
            child: Text(
              label,
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: Theme.of(context)
                        .colorScheme
                        .onSurface
                        .withValues(alpha: 0.65),
                  ),
            ),
          ),
          Text(
            value,
            style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                  fontWeight: FontWeight.w700,
                  color: highlight
                      ? Theme.of(context).colorScheme.error
                      : null,
                ),
          ),
        ],
      ),
    );
  }
}
