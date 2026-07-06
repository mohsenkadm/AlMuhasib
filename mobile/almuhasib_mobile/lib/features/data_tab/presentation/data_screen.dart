import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/config/system_profile.dart';
import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/design_system/design_system.dart';

class DataScreen extends StatelessWidget {
  const DataScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final profile = AppServices.prefs.systemProfile;
    final items = [
      _DataItem('customers', 'customers', Icons.people_outline, profile.primary),
      _DataItem('products', 'products', Icons.inventory_2_outlined, profile.accent),
      _DataItem('invoices', 'invoices', Icons.receipt_long, AppColors.success),
      _DataItem('suppliers', 'suppliers', Icons.local_shipping_outlined, AppColors.warning),
      _DataItem('investors', 'investors', Icons.savings_outlined, profile.secondary),
      _DataItem('warehouses', 'warehouses', Icons.warehouse_outlined, profile.accent),
    ];

    return AppPageScaffold(
      title: 'data_title'.tr(),
      subtitle: 'data_subtitle'.tr(),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _showQuickActions(context),
        icon: const Icon(Icons.add_rounded),
        label: Text('quick_add'.tr()),
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 120),
        children: [
          ...items.asMap().entries.map(
            (entry) => Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: AppEntityCard(
                title: entry.value.titleKey.tr(),
                leading: Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: entry.value.color.withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Icon(entry.value.icon, color: entry.value.color),
                ),
                trailing: const Icon(Icons.chevron_left),
                onTap: () => Get.toNamed(
                  AppRoutes.dataListPath(entry.value.type),
                ),
              ).fadeSlideInList(index: entry.key),
            ),
          ),
        ],
      ),
    );
  }

  void _showQuickActions(BuildContext context) {
    showModalBottomSheet(
      context: context,
      showDragHandle: true,
      builder: (ctx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
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
          ],
        ),
      ),
    );
  }
}

class _DataItem {
  const _DataItem(this.type, this.titleKey, this.icon, this.color);

  final String type;
  final String titleKey;
  final IconData icon;
  final Color color;
}
