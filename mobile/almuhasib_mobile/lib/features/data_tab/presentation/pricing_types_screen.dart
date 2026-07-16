import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/router/app_routes.dart';
import '../../../shared/models/master_data_models.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/pricing_types_controller.dart';

class PricingTypesScreen extends GetView<PricingTypesController> {
  const PricingTypesScreen({super.key});

  @override
  final String? tag = 'pricing_types';

  @override
  Widget build(BuildContext context) {
    return Obx(
      () => AppListPage<PricingTypeLookupItem>(
        title: 'pricing_types'.tr(),
        isLoading: controller.isLoading,
        error: controller.error,
        items: controller.items,
        onRefresh: controller.load,
        onRetry: controller.load,
        onSearchChanged: controller.updateSearch,
        fabLabel: 'add_pricing_type'.tr(),
        onFab: () async {
          final refreshed = await Get.toNamed<bool>(AppRoutes.pricingTypeNew);
          if (refreshed == true) controller.load();
        },
        emptyMessage: 'no_pricing_types'.tr(),
        emptyIcon: Icons.sell_outlined,
        itemBuilder: (context, item, index) => AppEntityCard(
          title: item.name,
          subtitle: [
            if (item.isDefault) 'pricing_type_default'.tr(),
            item.isActive
                ? 'pricing_type_active'.tr()
                : 'pricing_type_inactive'.tr(),
          ].join(' • '),
          trailing: const Icon(Icons.chevron_left),
          onTap: () async {
            final refreshed = await Get.toNamed<bool>(
              AppRoutes.pricingTypeEditPath(item.syncId),
              arguments: item,
            );
            if (refreshed == true) controller.load();
          },
        ).fadeSlideIn(delayMs: index * 40),
      ),
    );
  }
}
