import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/models/mobile_models.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../operations/presentation/forms/finance/finance_entity_forms.dart';

class FinanceListController extends GetxController {
  FinanceListController({required this.listType});

  final String listType;
  final isLoading = true.obs;
  final Rxn<Object> error = Rxn<Object>();
  final items = <dynamic>[].obs;
  final searchController = TextEditingController();

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      final search = searchController.text.trim();
      switch (listType) {
        case 'vouchers':
          final r = await AppServices.finance.getVouchers(
            search: search.isEmpty ? null : search,
          );
          items.assignAll(r.items);
        case 'expenses':
          final r = await AppServices.finance.getExpenses(
            search: search.isEmpty ? null : search,
          );
          items.assignAll(r.items);
        case 'transfers':
          final r = await AppServices.finance.getTransfers();
          items.assignAll(r.items);
        case 'warehouse-stocks':
          final r = await AppServices.finance.getWarehouseStocks(
            search: search.isEmpty ? null : search,
          );
          items.assignAll(r.items);
        case 'warehouse-transfers':
          final r = await AppServices.finance.getWarehouseTransfers();
          items.assignAll(r.items);
        default:
          items.clear();
      }
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }

  String get title => switch (listType) {
        'vouchers' => 'vouchers'.tr(),
        'expenses' => 'expenses'.tr(),
        'transfers' => 'transfers'.tr(),
        'warehouse-stocks' => 'warehouse_stocks'.tr(),
        'warehouse-transfers' => 'warehouse_transfers'.tr(),
        _ => 'data_title'.tr(),
      };

  String? get createRoute => switch (listType) {
        'vouchers' => AppRoutes.voucherNew,
        'expenses' => AppRoutes.expenseNew,
        'transfers' => AppRoutes.transferNew,
        'warehouse-stocks' => AppRoutes.stockAdjustment,
        'warehouse-transfers' => AppRoutes.warehouseTransferNew,
        _ => null,
      };

  @override
  void onClose() {
    searchController.dispose();
    super.onClose();
  }
}

class FinanceListScreen extends GetView<FinanceListController> {
  const FinanceListScreen({super.key, required this.listType});

  final String listType;

  @override
  String? get tag => 'finance_list_$listType';

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: controller.title,
      actions: [
        if (controller.createRoute != null)
          IconButton(
            icon: const Icon(Icons.add_circle_outline_rounded),
            onPressed: () async {
              final ok = await Get.toNamed(controller.createRoute!);
              if (ok == true) controller.load();
            },
          ),
      ],
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 8),
            child: TextField(
              controller: controller.searchController,
              onSubmitted: (_) => controller.load(),
              decoration: InputDecoration(
                hintText: 'search_hint'.tr(),
                prefixIcon: const Icon(Icons.search),
                suffixIcon: IconButton(
                  icon: const Icon(Icons.refresh),
                  onPressed: controller.load,
                ),
              ),
            ),
          ),
          Expanded(
            child: Obx(() {
              if (controller.isLoading.value) {
                return const Center(child: CircularProgressIndicator());
              }
              if (controller.error.value != null) {
                return ErrorStateWidget(
                  message: AppExceptionHandler.messageFor(controller.error.value),
                  onRetry: controller.load,
                );
              }
              if (controller.items.isEmpty) {
                return Center(
                  child: EmptyStateWidget(
                    onRetry: controller.load,
                  ),
                );
              }
              final items = controller.items.toList(growable: false);
              return RefreshIndicator(
                onRefresh: controller.load,
                child: ListView.separated(
                  padding: const EdgeInsets.fromLTRB(20, 8, 20, 120),
                  itemCount: items.length + 1,
                  separatorBuilder: (_, __) => const SizedBox(height: 10),
                  itemBuilder: (context, index) {
                    if (index == 0) {
                      return _FinanceStatsHeader(
                        listType: listType,
                        items: items,
                      );
                    }
                    return _buildTile(items[index - 1]);
                  },
                ),
              );
            }),
          ),
        ],
      ),
    );
  }

  Widget _buildTile(dynamic item) {
    if (item is VoucherListItem) {
      return AppEntityCard(
        title: '${item.voucherNumber} • ${voucherTypeLabel(item.voucherType)}',
        subtitle:
            '${item.customerName ?? item.investorName ?? item.cashBoxName} • ${formatDate(item.date)}',
        trailing: Text(
          formatCurrency(item.amount),
          style: const TextStyle(fontWeight: FontWeight.w800),
        ),
        leading: const Icon(Icons.receipt_long_rounded, color: AppColors.primary),
      );
    }
    if (item is ExpenseListItem) {
      return AppEntityCard(
        title: item.expenseTypeName,
        subtitle: '${item.cashBoxName} • ${formatDate(item.date)}',
        trailing: Text(
          formatCurrency(item.amount),
          style: const TextStyle(
            fontWeight: FontWeight.w800,
            color: AppColors.error,
          ),
        ),
        leading: const Icon(Icons.money_off_outlined, color: AppColors.error),
      );
    }
    if (item is TransferListItem) {
      return AppEntityCard(
        title: '${item.fromName} → ${item.toName}',
        subtitle: formatDate(item.date),
        trailing: Text(
          formatCurrency(item.amount),
          style: const TextStyle(fontWeight: FontWeight.w800),
        ),
        leading: const Icon(Icons.swap_horiz_rounded, color: AppColors.moduleCyan),
      );
    }
    if (item is WarehouseStockListItem) {
      return AppEntityCard(
        title: item.productName,
        subtitle: item.warehouseName,
        trailing: Text(
          '${item.quantity}',
          style: const TextStyle(fontWeight: FontWeight.w800),
        ),
        leading: const Icon(Icons.inventory_2_outlined, color: AppColors.moduleGreen),
      );
    }
    if (item is WarehouseTransferListItem) {
      return AppEntityCard(
        title: item.transferNumber,
        subtitle:
            '${item.fromWarehouseName} → ${item.toWarehouseName} • ${formatDate(item.date)}',
        trailing: Text('${item.items.length}'),
        leading: const Icon(Icons.move_up_rounded, color: AppColors.moduleIndigo),
      );
    }
    return const SizedBox.shrink();
  }
}

class _FinanceStatsHeader extends StatelessWidget {
  const _FinanceStatsHeader({required this.listType, required this.items});

  final String listType;
  final List<dynamic> items;

  @override
  Widget build(BuildContext context) {
    final count = items.length;
    switch (listType) {
      case 'vouchers':
        final total = items
            .whereType<VoucherListItem>()
            .fold<double>(0, (s, e) => s + e.amount);
        return PageStatsHeader(
          heroTitle: 'vouchers'.tr(),
          heroValue: formatCurrency(total),
          heroSubtitle: '$count ${'records_count'.tr()}',
          stats: [
            StatsChipData(
              label: 'records_count'.tr(),
              value: '$count',
              icon: Icons.receipt_long_rounded,
              color: AppColors.primary,
            ),
            StatsChipData(
              label: 'sum_amount'.tr(),
              value: formatCurrency(total),
              icon: Icons.payments_outlined,
              color: AppColors.moduleGreen,
            ),
          ],
        );
      case 'expenses':
        final total = items
            .whereType<ExpenseListItem>()
            .fold<double>(0, (s, e) => s + e.amount);
        return PageStatsHeader(
          heroTitle: 'expenses'.tr(),
          heroValue: formatCurrency(total),
          heroSubtitle: '$count ${'records_count'.tr()}',
          trendPositive: false,
          stats: [
            StatsChipData(
              label: 'records_count'.tr(),
              value: '$count',
              icon: Icons.money_off_outlined,
              color: AppColors.error,
            ),
            StatsChipData(
              label: 'sum_amount'.tr(),
              value: formatCurrency(total),
              icon: Icons.payments_outlined,
              color: AppColors.moduleOrange,
            ),
          ],
        );
      case 'transfers':
        final total = items
            .whereType<TransferListItem>()
            .fold<double>(0, (s, e) => s + e.amount);
        return PageStatsHeader(
          heroTitle: 'transfers'.tr(),
          heroValue: formatCurrency(total),
          heroSubtitle: '$count ${'records_count'.tr()}',
          stats: [
            StatsChipData(
              label: 'records_count'.tr(),
              value: '$count',
              icon: Icons.swap_horiz_rounded,
              color: AppColors.moduleCyan,
            ),
            StatsChipData(
              label: 'sum_amount'.tr(),
              value: formatCurrency(total),
              icon: Icons.payments_outlined,
              color: AppColors.primary,
            ),
          ],
        );
      case 'warehouse-stocks':
        final qty = items
            .whereType<WarehouseStockListItem>()
            .fold<double>(0, (s, e) => s + e.quantity);
        return PageStatsHeader(
          heroTitle: 'warehouse_stocks'.tr(),
          heroValue: qty.toStringAsFixed(0),
          heroSubtitle: '$count ${'products'.tr()}',
          stats: [
            StatsChipData(
              label: 'products'.tr(),
              value: '$count',
              icon: Icons.inventory_2_outlined,
              color: AppColors.moduleGreen,
            ),
            StatsChipData(
              label: 'quantity'.tr(),
              value: qty.toStringAsFixed(0),
              icon: Icons.numbers_rounded,
              color: AppColors.primary,
            ),
          ],
        );
      case 'warehouse-transfers':
        final lines = items
            .whereType<WarehouseTransferListItem>()
            .fold<int>(0, (s, e) => s + e.items.length);
        return PageStatsHeader(
          heroTitle: 'warehouse_transfers'.tr(),
          heroValue: '$count',
          heroSubtitle: 'records_count'.tr(),
          stats: [
            StatsChipData(
              label: 'records_count'.tr(),
              value: '$count',
              icon: Icons.move_up_rounded,
              color: AppColors.moduleIndigo,
            ),
            StatsChipData(
              label: 'lines_count'.tr(),
              value: '$lines',
              icon: Icons.list_alt_rounded,
              color: AppColors.primary,
            ),
          ],
        );
      default:
        return PageStatsHeader(
          heroTitle: 'data_title'.tr(),
          heroValue: '$count',
          heroSubtitle: 'records_count'.tr(),
        );
    }
  }
}
