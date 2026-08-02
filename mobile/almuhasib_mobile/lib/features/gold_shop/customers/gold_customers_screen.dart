import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/theme/system_themes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/gold_customers_controller.dart';
import '../models/gold_shop_models.dart';

class GoldCustomersScreen extends GetView<GoldCustomersController> {
  const GoldCustomersScreen({super.key});

  @override
  final String? tag = 'gold_customers';

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => AppListPage<GoldCustomerListItem>(
        title: 'gold_customers_title'.tr(),
        isLoading: controller.isLoading,
        error: controller.error,
        items: controller.items,
        onRefresh: controller.load,
        onRetry: controller.load,
        emptyMessage: 'gold_no_customers'.tr(),
        emptyIcon: Icons.people_outline_rounded,
        filterPanel: AppFilterBar(
          onSearchChanged: controller.updateSearch,
        ),
        itemBuilder: (context, c, index) {
          final hasCredit =
              c.creditBalanceIqd > 0 || c.creditBalanceUsd > 0;
          return AppEntityCard(
            title: c.name,
            subtitle:
                '${c.phone.isEmpty ? '—' : c.phone}${c.openInvoiceCount > 0 ? '\n${'gold_open_invoices'.tr()}: ${c.openInvoiceCount}' : ''}',
            leading: Container(
              width: 46,
              height: 46,
              decoration: BoxDecoration(
                color: SystemThemes.goldPrimary.withValues(alpha: 0.14),
                shape: BoxShape.circle,
              ),
              child: Center(
                child: Text(
                  c.name.isEmpty ? '?' : c.name.substring(0, 1),
                  style: const TextStyle(
                    fontWeight: FontWeight.w800,
                    color: SystemThemes.goldPrimary,
                    fontSize: 18,
                  ),
                ),
              ),
            ),
            trailing: hasCredit
                ? Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        formatCurrency(c.creditBalanceIqd),
                        style: TextStyle(
                          fontWeight: FontWeight.w800,
                          color: Theme.of(context).colorScheme.error,
                        ),
                      ),
                      if (c.creditBalanceUsd > 0)
                        Text(
                          '${formatCurrency(c.creditBalanceUsd)} \$',
                          style: Theme.of(context).textTheme.bodySmall,
                        ),
                    ],
                  )
                : Icon(
                    c.isActive ? Icons.check_circle_outline : Icons.block,
                    color: c.isActive
                        ? const Color(0xFF2E7D32)
                        : Theme.of(context).disabledColor,
                  ),
          );
        },
      ),
    );
  }
}
