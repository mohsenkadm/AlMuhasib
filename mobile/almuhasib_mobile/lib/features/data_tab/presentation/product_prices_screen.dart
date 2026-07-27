import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/models/master_data_models.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/product_prices_controller.dart';

class ProductPricesScreen extends GetView<ProductPricesController> {
  const ProductPricesScreen({super.key});

  @override
  final String? tag = 'product_prices';

  @override
  Widget build(BuildContext context) {
    return AppListPage<ProductPriceLookupItem>(
      title: 'product_prices'.tr(),
      isLoading: controller.isLoading,
      error: controller.error,
      items: controller.items,
      onRefresh: controller.load,
      onRetry: controller.load,
      onSearchChanged: controller.updateSearch,
      fabLabel: 'add_product_price'.tr(),
      onFab: () async {
        final refreshed = await Get.toNamed<bool>(
          AppRoutes.productPriceNew,
          arguments: controller.productSyncId,
        );
        if (refreshed == true) controller.load();
      },
      emptyMessage: 'no_product_prices'.tr(),
      emptyIcon: Icons.price_change_outlined,
      itemBuilder: (context, item, index) => AppEntityCard(
        title: item.productName.isEmpty ? item.pricingTypeName : item.productName,
        subtitle:
            '${item.pricingTypeName} • ${'sale_price'.tr()}: ${formatCurrency(item.salePrice)} • ${'purchase_price'.tr()}: ${formatCurrency(item.purchasePrice)}',
        leading: Container(
          width: 48,
          height: 48,
          decoration: BoxDecoration(
            color: AppColors.warning.withValues(alpha: 0.14),
            borderRadius: BorderRadius.circular(14),
          ),
          child: const Icon(
            Icons.price_change_outlined,
            color: AppColors.warning,
          ),
        ),
        trailing: Text(
          formatCurrency(item.salePrice),
          style: const TextStyle(
            fontWeight: FontWeight.w800,
            color: AppColors.primary,
          ),
        ),
        onTap: () async {
          final refreshed = await Get.toNamed<bool>(
            AppRoutes.productPriceEditPath(item.syncId),
            arguments: item,
          );
          if (refreshed == true) controller.load();
        },
      ).fadeSlideIn(delayMs: index * 40),
    );
  }
}
