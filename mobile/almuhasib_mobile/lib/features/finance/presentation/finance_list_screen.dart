import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/models/mobile_models.dart';
import '../../../shared/utils/formatters.dart';
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
                return Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(AppExceptionHandler.messageFor(controller.error.value)),
                      TextButton(
                        onPressed: controller.load,
                        child: Text('retry'.tr()),
                      ),
                    ],
                  ),
                );
              }
              if (controller.items.isEmpty) {
                return Center(child: Text('no_data'.tr()));
              }
              return RefreshIndicator(
                onRefresh: controller.load,
                child: ListView.separated(
                  padding: const EdgeInsets.fromLTRB(20, 8, 20, 120),
                  itemCount: controller.items.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 10),
                  itemBuilder: (context, index) {
                    final item = controller.items[index];
                    return _buildTile(item);
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
