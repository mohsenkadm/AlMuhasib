import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../core/constants/app_colors.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';

class DataScreen extends StatelessWidget {
  const DataScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final items = [
      _DataItem('customers', 'customers', Icons.people_outline, AppColors.primaryLight),
      _DataItem('products', 'products', Icons.inventory_2_outlined, AppColors.accent),
      _DataItem('invoices', 'invoices', Icons.receipt_long, AppColors.success),
      _DataItem('suppliers', 'suppliers', Icons.local_shipping_outlined, AppColors.warning),
      _DataItem('investors', 'investors', Icons.savings_outlined, const Color(0xFF00897B)),
      _DataItem('warehouses', 'warehouses', Icons.warehouse_outlined, const Color(0xFF7E57C2)),
    ];

    return Scaffold(
      appBar: AppBar(title: Text('data_title'.tr())),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _showQuickActions(context),
        icon: const Icon(Icons.add),
        label: Text('quick_add'.tr()),
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 120),
        children: [
          Text('data_subtitle'.tr(), style: Theme.of(context).textTheme.bodyMedium)
              .fadeSlideIn(),
          const SizedBox(height: 16),
          ...items.asMap().entries.map(
            (entry) => Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: GradientCard(
                child: ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: entry.value.color.withValues(alpha: 0.15),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Icon(entry.value.icon, color: entry.value.color),
                  ),
                  title: Text(entry.value.titleKey.tr()),
                  trailing: const Icon(Icons.chevron_left),
                  onTap: () => context.push('/data/list/${entry.value.type}'),
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
      builder: (ctx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.receipt_long),
              title: Text('new_invoice'.tr()),
              onTap: () {
                Navigator.pop(ctx);
                context.push('/data/invoice/new');
              },
            ),
            ListTile(
              leading: const Icon(Icons.person_add),
              title: Text('add_customer'.tr()),
              onTap: () {
                Navigator.pop(ctx);
                context.push('/data/customer/new');
              },
            ),
            ListTile(
              leading: const Icon(Icons.inventory_2),
              title: Text('add_product'.tr()),
              onTap: () {
                Navigator.pop(ctx);
                context.push('/data/product/new');
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
