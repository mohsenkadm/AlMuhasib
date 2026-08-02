import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/theme/system_themes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/gold_prices_controller.dart';
import '../widgets/gold_kpi_card.dart';
import '../widgets/gold_labels.dart';

class GoldPricesScreen extends GetView<GoldPricesController> {
  const GoldPricesScreen({super.key});

  @override
  final String? tag = 'gold_prices';

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('gold_prices_title'.tr())),
      body: Obx(() {
        if (controller.isLoading.value && controller.items.isEmpty) {
          return const Center(child: CircularProgressIndicator());
        }
        if (controller.error.value != null && controller.items.isEmpty) {
          return ErrorStateWidget(
            message:
                AppExceptionHandler.messageFor(controller.error.value),
            onRetry: controller.load,
          );
        }
        if (controller.items.isEmpty) {
          return EmptyStateWidget(
            message: 'gold_no_prices'.tr(),
            icon: Icons.monetization_on_outlined,
            onRetry: controller.load,
          );
        }

        return RefreshIndicator(
          onRefresh: controller.load,
          child: ListView.builder(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 40),
            itemCount: controller.items.length + 1,
            itemBuilder: (context, index) {
              if (index == 0) {
                return Padding(
                  padding: const EdgeInsets.only(bottom: 16),
                  child: GoldSectionHeader(title: 'gold_mithqal_prices'.tr()),
                );
              }
              final p = controller.items[index - 1];
              return Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: AppEntityCard(
                  title: goldKaratLabel(p.karatValue, karatName: p.karatName),
                  subtitle:
                      '${formatDate(p.priceDate)}${p.pricePerGram != null ? ' • ${formatCurrency(p.pricePerGram!)}/${'gold_gram'.tr()}' : ''}',
                  leading: Container(
                    width: 46,
                    height: 46,
                    decoration: BoxDecoration(
                      gradient: LinearGradient(
                        colors: [
                          SystemThemes.goldPrimary.withValues(alpha: 0.2),
                          SystemThemes.goldSecondary.withValues(alpha: 0.35),
                        ],
                      ),
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(
                      Icons.monetization_on_outlined,
                      color: SystemThemes.goldPrimary,
                    ),
                  ),
                  trailing: Text(
                    '${formatCurrency(p.pricePerMithqal)} ${goldCurrencyLabel(p.currency)}',
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.w800,
                          color: SystemThemes.goldPrimary,
                        ),
                  ),
                ),
              );
            },
          ),
        );
      }),
    );
  }
}
