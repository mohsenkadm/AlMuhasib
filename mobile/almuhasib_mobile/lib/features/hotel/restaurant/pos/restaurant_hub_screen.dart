import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../../core/config/system_profile.dart';
import '../../../../core/getx/app_services.dart';
import '../../../../shared/widgets/common_widgets.dart';
import '../../../../shared/widgets/design_system/design_system.dart';
import '../../../../shared/widgets/shimmer_widgets.dart';
import '../data/restaurant_pos_controller.dart';
import '../models/restaurant_models.dart';
import '../reports/restaurant_reports_screen.dart';

class RestaurantHubScreen extends GetView<RestaurantPosController> {
  const RestaurantHubScreen({super.key});

  @override
  final String? tag = 'restaurant_hub';

  @override
  Widget build(BuildContext context) {
    return _RestaurantHubView(controller: controller);
  }
}

class _RestaurantHubView extends StatelessWidget {
  const _RestaurantHubView({required this.controller});

  final RestaurantPosController controller;

  @override
  Widget build(BuildContext context) {
    final profile = AppServices.prefs.systemProfile;

    return Scaffold(
      appBar: AppBar(
        title: Text('restaurant_title'.tr()),
        flexibleSpace: DecoratedBox(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              colors: [profile.primary, profile.secondary],
            ),
          ),
        ),
        foregroundColor: Colors.white,
        bottom: TabBar(
          controller: controller.tabController,
          indicatorColor: Colors.white,
          labelColor: Colors.white,
          unselectedLabelColor: Colors.white70,
          tabs: [
            Tab(text: 'restaurant_pos_tab'.tr()),
            Tab(text: 'restaurant_reports_tab'.tr()),
            Tab(text: 'restaurant_stock_tab'.tr()),
          ],
        ),
      ),
      body: TabBarView(
        controller: controller.tabController,
        children: [
          Obx(() {
            if (controller.isMenuLoading.value) {
              return const ListShimmer();
            }
            final error = controller.menuError.value;
            if (error != null) {
              return ErrorStateWidget(
                message: AppExceptionHandler.messageFor(error),
                onRetry: controller.loadMenu,
              );
            }
            final menu = controller.menu.value;
            if (menu == null) {
              return const SizedBox.shrink();
            }
            return _PosPanel(controller: controller, menu: menu);
          }),
          RestaurantReportsScreen(controller: controller),
          _StockAlertsPanel(controller: controller),
        ],
      ),
    );
  }
}

class _PosPanel extends StatelessWidget {
  const _PosPanel({required this.controller, required this.menu});

  final RestaurantPosController controller;
  final RestaurantMenuData menu;

  @override
  Widget build(BuildContext context) {
    final accent = AppServices.prefs.systemProfile.primary;

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(12, 12, 12, 0),
          child: Obx(
            () => SegmentedButton<int>(
              segments: [
                ButtonSegment(
                  value: 0,
                  label: Text('restaurant_dine_in'.tr()),
                ),
                ButtonSegment(
                  value: 1,
                  label: Text('restaurant_takeaway'.tr()),
                ),
                ButtonSegment(
                  value: 2,
                  label: Text('restaurant_room_service'.tr()),
                ),
              ],
              selected: {controller.orderType.value},
              onSelectionChanged: (selection) =>
                  controller.setOrderType(selection.first),
            ),
          ),
        ),
        if (menu.categories.length > 1)
          SizedBox(
            height: 44,
            child: Obx(
              () => ListView.separated(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                itemCount: menu.categories.length,
                separatorBuilder: (_, __) => const SizedBox(width: 8),
                itemBuilder: (context, index) {
                  final category = menu.categories[index];
                  final selected =
                      controller.selectedCategoryId.value == category.syncId;
                  return FilterChip(
                    label: Text(category.name),
                    selected: selected,
                    onSelected: (_) =>
                        controller.selectCategory(category.syncId),
                  );
                },
              ),
            ),
          ),
        Expanded(
          flex: 2,
          child: Obx(() {
            final selectedCategory = controller.selectedCategoryId.value.isNotEmpty
                ? controller.selectedCategoryId.value
                : (menu.categories.isNotEmpty
                    ? menu.categories.first.syncId
                    : '');
            final items = menu.items
                .where((item) => item.categorySyncId == selectedCategory)
                .toList();

            return GridView.builder(
              padding: const EdgeInsets.all(12),
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 3,
                childAspectRatio: 1.4,
                crossAxisSpacing: 8,
                mainAxisSpacing: 8,
              ),
              itemCount: items.length,
              itemBuilder: (context, index) {
                final item = items[index];
                return Hero(
                  tag: 'menu_${item.syncId}',
                  child: Material(
                    color: accent.withValues(alpha: 0.08),
                    borderRadius: BorderRadius.circular(12),
                    child: InkWell(
                      borderRadius: BorderRadius.circular(12),
                      onTap: () => controller.addToCart(item),
                      child: Padding(
                        padding: const EdgeInsets.all(8),
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Text(
                              item.name,
                              textAlign: TextAlign.center,
                              maxLines: 2,
                            ),
                            const SizedBox(height: 4),
                            Text(
                              NumberFormat('#,###').format(item.salePrice),
                              style: TextStyle(
                                color: accent,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                );
              },
            );
          }),
        ),
        Obx(
          () => AnimatedContainer(
            duration: const Duration(milliseconds: 300),
            curve: Curves.easeOutCubic,
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: Theme.of(context).colorScheme.surface,
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.08),
                  blurRadius: 12,
                  offset: const Offset(0, -4),
                ),
              ],
            ),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        '${controller.cart.length} ${'restaurant_items'.tr()}',
                      ),
                      Text(
                        NumberFormat('#,###').format(controller.cartTotal),
                        style: Theme.of(context).textTheme.titleLarge?.copyWith(
                              fontWeight: FontWeight.bold,
                              color: accent,
                            ),
                      ),
                    ],
                  ),
                ),
                FilledButton(
                  onPressed: controller.cart.isEmpty ? null : controller.pay,
                  style: FilledButton.styleFrom(backgroundColor: accent),
                  child: Text('restaurant_pay'.tr()),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _StockAlertsPanel extends StatelessWidget {
  const _StockAlertsPanel({required this.controller});

  final RestaurantPosController controller;

  @override
  Widget build(BuildContext context) {
    return Obx(() {
      if (controller.isAlertsLoading.value) {
        return const Center(child: CircularProgressIndicator());
      }
      final error = controller.alertsError.value;
      if (error != null) {
        return Center(child: Text('$error'));
      }
      final alerts = controller.alerts.value;
      if (alerts.isEmpty) {
        return Center(child: Text('restaurant_no_alerts'.tr()));
      }
      return ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: alerts.length,
        separatorBuilder: (_, __) => const Divider(),
        itemBuilder: (context, index) {
          final alert = alerts[index];
          return ListTile(
            leading: const Icon(
              Icons.warning_amber_rounded,
              color: Colors.orange,
            ),
            title: Text(alert.name),
            subtitle: Text('${alert.quantity} / ${alert.minQuantity} ${alert.unit}'),
          );
        },
      );
    });
  }
}
