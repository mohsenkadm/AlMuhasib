import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/router/app_routes.dart';
import '../../../core/theme/system_themes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/gold_sales_controller.dart';
import '../models/gold_shop_models.dart';
import '../widgets/gold_labels.dart';

class GoldSalesScreen extends GetView<GoldSalesController> {
  const GoldSalesScreen({super.key});

  @override
  final String? tag = 'gold_sales';

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => AppListPage<GoldInvoiceListItem>(
        title: 'gold_sales_title'.tr(),
        isLoading: controller.isLoading,
        error: controller.error,
        items: controller.items,
        onRefresh: controller.load,
        onRetry: controller.load,
        emptyMessage: 'gold_no_sales'.tr(),
        emptyIcon: Icons.receipt_long_outlined,
        filterPanel: AppFilterBar(
          onSearchChanged: controller.updateSearch,
          filterChips: [
            FilterChipOption(id: '0', label: 'gold_status_completed'.tr()),
            FilterChipOption(id: '1', label: 'gold_status_open'.tr()),
            FilterChipOption(id: '2', label: 'gold_status_partial'.tr()),
            FilterChipOption(id: '3', label: 'gold_status_cancelled'.tr()),
          ],
          onFilterSelected: (id) {
            controller.updateStatusFilter(
              id == null ? null : int.tryParse(id),
            );
          },
          onClear: controller.clearFilters,
        ),
        itemBuilder: (context, inv, index) => AppEntityCard(
          title: inv.invoiceNumber,
          subtitle:
              '${inv.customerName.isEmpty ? '—' : inv.customerName}\n${formatDate(inv.invoiceDate)} • ${goldPaymentMethodLabel(inv.paymentMethod)}',
          leading: Container(
            width: 46,
            height: 46,
            decoration: BoxDecoration(
              color: SystemThemes.goldPrimary.withValues(alpha: 0.14),
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.receipt_long_outlined,
              color: SystemThemes.goldPrimary,
            ),
          ),
          trailing: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                formatCurrency(inv.totalAmountIqd),
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
              ),
              Text(
                goldInvoiceStatusLabel(inv.status),
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: goldInvoiceStatusColor(inv.status),
                      fontWeight: FontWeight.w600,
                    ),
              ),
            ],
          ),
          onTap: () => Get.toNamed(AppRoutes.goldShopSaleDetailPath(inv.id)),
        ),
      ),
    );
  }
}
