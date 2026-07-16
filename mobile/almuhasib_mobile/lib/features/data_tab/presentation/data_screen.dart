import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../controllers/data_hub_controller.dart';

class DataScreen extends GetView<DataHubController> {
  const DataScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Obx(() {
      final items = <_DataItem>[
        const _DataItem(
          'customers',
          'customers',
          Icons.people_outline_rounded,
          AppColors.primary,
          'data_customers_desc',
        ),
        const _DataItem(
          'products',
          'products',
          Icons.inventory_2_outlined,
          AppColors.moduleGreen,
          'data_products_desc',
        ),
        const _DataItem(
          'invoices',
          'invoices',
          Icons.receipt_long_rounded,
          AppColors.moduleOrange,
          'data_invoices_desc',
        ),
        const _DataItem(
          'suppliers',
          'suppliers',
          Icons.local_shipping_outlined,
          AppColors.modulePurple,
          'data_suppliers_desc',
        ),
        const _DataItem(
          'investors',
          'investors',
          Icons.savings_outlined,
          AppColors.moduleCyan,
          'data_investors_desc',
        ),
        const _DataItem(
          'warehouses',
          'warehouses',
          Icons.warehouse_outlined,
          AppColors.moduleIndigo,
          'data_warehouses_desc',
        ),
        if (controller.productPricingEnabled) ...[
          const _DataItem(
            'pricing-types',
            'pricing_types',
            Icons.sell_outlined,
            AppColors.modulePink,
            'data_pricing_desc',
            route: AppRoutes.pricingTypes,
          ),
          const _DataItem(
            'product-prices',
            'product_prices',
            Icons.price_change_outlined,
            AppColors.warning,
            'data_prices_desc',
            route: AppRoutes.productPrices,
          ),
        ],
      ];

      return AppPageScaffold(
        title: 'data_title'.tr(),
        subtitle: 'data_subtitle'.tr(),
        actions: [
          IconButton(
            tooltip: 'quick_add'.tr(),
            onPressed: () => _showQuickActions(context),
            icon: const Icon(Icons.add_circle_outline_rounded),
          ),
        ],
        body: RefreshIndicator(
          onRefresh: controller.load,
          child: GridView.builder(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 110),
            gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: 2,
              mainAxisSpacing: 14,
              crossAxisSpacing: 14,
              childAspectRatio: 0.92,
            ),
            itemCount: items.length,
            itemBuilder: (context, index) {
              final item = items[index];
              return AppModuleTile(
                title: item.titleKey.tr(),
                subtitle: item.subtitleKey.tr(),
                icon: item.icon,
                color: item.color,
                onTap: () {
                  if (item.route != null) {
                    Get.toNamed(item.route!);
                  } else {
                    Get.toNamed(AppRoutes.dataListPath(item.type));
                  }
                },
              ).fadeSlideInList(index: index);
            },
          ),
        ),
      );
    });
  }

  void _showQuickActions(BuildContext context) {
    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (ctx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
              child: Text(
                'quick_add'.tr(),
                style: Theme.of(ctx).textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
              ),
            ),
            ListTile(
              leading: const Icon(Icons.receipt_long_rounded),
              title: Text('new_invoice'.tr()),
              onTap: () {
                Navigator.pop(ctx);
                Get.toNamed(AppRoutes.invoiceNew);
              },
            ),
            ListTile(
              leading: const Icon(Icons.person_add_outlined),
              title: Text('new_customer'.tr()),
              onTap: () {
                Navigator.pop(ctx);
                Get.toNamed(AppRoutes.customerNew);
              },
            ),
            ListTile(
              leading: const Icon(Icons.inventory_2_outlined),
              title: Text('new_product'.tr()),
              onTap: () {
                Navigator.pop(ctx);
                Get.toNamed(AppRoutes.productNew);
              },
            ),
            ListTile(
              leading: const Icon(Icons.local_shipping_outlined),
              title: Text('new_supplier'.tr()),
              onTap: () {
                Navigator.pop(ctx);
                Get.toNamed(AppRoutes.supplierNew);
              },
            ),
            ListTile(
              leading: const Icon(Icons.savings_outlined),
              title: Text('new_investor'.tr()),
              onTap: () {
                Navigator.pop(ctx);
                Get.toNamed(AppRoutes.investorNew);
              },
            ),
            if (controller.productPricingEnabled) ...[
              ListTile(
                leading: const Icon(Icons.sell_outlined),
                title: Text('add_pricing_type'.tr()),
                onTap: () {
                  Navigator.pop(ctx);
                  Get.toNamed(AppRoutes.pricingTypeNew);
                },
              ),
              ListTile(
                leading: const Icon(Icons.price_change_outlined),
                title: Text('add_product_price'.tr()),
                onTap: () {
                  Navigator.pop(ctx);
                  Get.toNamed(AppRoutes.productPriceNew);
                },
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _DataItem {
  const _DataItem(
    this.type,
    this.titleKey,
    this.icon,
    this.color,
    this.subtitleKey, {
    this.route,
  });

  final String type;
  final String titleKey;
  final String subtitleKey;
  final IconData icon;
  final Color color;
  final String? route;
}
