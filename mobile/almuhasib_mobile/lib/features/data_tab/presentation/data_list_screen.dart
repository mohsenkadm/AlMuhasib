import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/models/master_data_models.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/entity_list_tile.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../../../shared/widgets/shimmer_widgets.dart';

class DataListController extends GetxController {
  DataListController({required this.listType});

  final String listType;

  final isLoading = true.obs;
  final Rxn<Object> error = Rxn<Object>();
  final items = <dynamic>[].obs;
  final search = ''.obs;
  final RxnInt invoiceTypeFilter = RxnInt();
  final RxnInt paymentFilter = RxnInt();
  final from = DateTime.now().subtract(const Duration(days: 90)).obs;
  final to = DateTime.now().obs;

  @override
  void onInit() {
    super.onInit();
    reload();
  }

  Future<void> reload() async {
    isLoading.value = true;
    error.value = null;
    try {
      final repo = AppServices.data;
      final loaded = switch (listType) {
        'customers' => await repo.getCustomers(search: search.value),
        'products' => await repo.getProducts(search: search.value),
        'suppliers' => await repo.getSuppliers(search: search.value),
        'investors' => await repo.getInvestors(search: search.value),
        'warehouses' => await repo.getWarehouses(search: search.value),
        'invoices' => (await repo.getInvoices(
            from: from.value,
            to: to.value,
            search: search.value,
            invoiceType: invoiceTypeFilter.value,
            paymentMethod: paymentFilter.value,
          ))
            .items,
        _ => <dynamic>[],
      };
      items.value = loaded;
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }

  void updateSearch(String value) {
    search.value = value;
    reload();
  }

  void updateInvoiceTypeFilter(String? id) {
    invoiceTypeFilter.value = id == null ? null : int.tryParse(id);
    reload();
  }

  Future<void> pickFromDate(BuildContext context) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: from.value,
      firstDate: DateTime(2020),
      lastDate: DateTime.now(),
    );
    if (picked != null) {
      from.value = picked;
      await reload();
    }
  }

  Future<void> pickToDate(BuildContext context) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: to.value,
      firstDate: DateTime(2020),
      lastDate: DateTime.now(),
    );
    if (picked != null) {
      to.value = picked;
      await reload();
    }
  }

  String get title => switch (listType) {
        'customers' => 'customers'.tr(),
        'products' => 'products'.tr(),
        'suppliers' => 'suppliers'.tr(),
        'investors' => 'investors'.tr(),
        'warehouses' => 'warehouses'.tr(),
        'invoices' => 'invoices'.tr(),
        _ => 'data_title'.tr(),
      };

  String? get fabRoute => switch (listType) {
        'customers' => AppRoutes.customerNew,
        'products' => AppRoutes.productNew,
        'suppliers' => AppRoutes.supplierNew,
        'investors' => AppRoutes.investorNew,
        'invoices' => AppRoutes.invoiceNew,
        _ => null,
      };

  String? detailRouteFor(LookupItem item) => switch (listType) {
        'customers' =>
          AppRoutes.customerDetailPath(item.syncId, name: item.name),
        'products' => AppRoutes.productDetailPath(item.syncId, name: item.name),
        'suppliers' =>
          AppRoutes.supplierDetailPath(item.syncId, name: item.name),
        'investors' =>
          AppRoutes.investorDetailPath(item.syncId, name: item.name),
        _ => null,
      };
}

class DataListScreen extends StatelessWidget {
  const DataListScreen({super.key, required this.listType});

  final String listType;

  @override
  Widget build(BuildContext context) {
    final controller = Get.put(
      DataListController(listType: listType),
      tag: 'data_list_$listType',
    );

    final invoiceFilters = listType == 'invoices'
        ? [
            const FilterChipOption(id: '0', label: 'شراء'),
            const FilterChipOption(id: '1', label: 'بيع'),
            const FilterChipOption(id: '2', label: 'قسط'),
            const FilterChipOption(id: '3', label: 'مرتجع'),
          ]
        : <FilterChipOption>[];

    return Obx(() {
      return Scaffold(
        appBar: AppBar(title: Text(controller.title)),
        floatingActionButton: controller.fabRoute != null
            ? FloatingActionButton(
                onPressed: () async {
                  final refreshed =
                      await Get.toNamed<bool>(controller.fabRoute!);
                  if (refreshed == true) controller.reload();
                },
                child: const Icon(Icons.add),
              )
            : null,
        body: Column(
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
              child: SearchFilterBar(
                onSearchChanged: controller.updateSearch,
                filterChips: invoiceFilters,
                onFilterSelected: listType == 'invoices'
                    ? controller.updateInvoiceTypeFilter
                    : null,
              ),
            ),
            if (listType == 'invoices')
              Padding(
                padding:
                    const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                child: Row(
                  children: [
                    Expanded(
                      child: OutlinedButton(
                        onPressed: () => controller.pickFromDate(context),
                        child: Text(
                          '${'from_date'.tr()}\n${formatDate(controller.from.value)}',
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: OutlinedButton(
                        onPressed: () => controller.pickToDate(context),
                        child: Text(
                          '${'to_date'.tr()}\n${formatDate(controller.to.value)}',
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            Expanded(
              child: RefreshIndicator(
                onRefresh: controller.reload,
                child: _DataListBody(
                  controller: controller,
                  listType: listType,
                ),
              ),
            ),
          ],
        ),
      );
    });
  }
}

class _DataListBody extends StatelessWidget {
  const _DataListBody({
    required this.controller,
    required this.listType,
  });

  final DataListController controller;
  final String listType;

  @override
  Widget build(BuildContext context) {
    if (controller.isLoading.value) return const ListShimmer();
    if (controller.error.value != null) {
      return ErrorStateWidget(
        message: controller.error.value.toString(),
        onRetry: controller.reload,
      );
    }
    if (controller.items.isEmpty) {
      return EmptyStateWidget(onRetry: controller.reload);
    }

    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: controller.items.length,
      separatorBuilder: (_, __) => const SizedBox(height: 8),
      itemBuilder: (context, index) {
        final item = controller.items[index];
        Widget tile;
        if (item is LookupItem && listType != 'invoices') {
          tile = EntityListTile(
            name: item.name,
            subtitle: item.extra,
            onTap: () {
              final route = controller.detailRouteFor(item);
              if (route != null) {
                Get.toNamed(route, arguments: item);
              }
            },
          );
        } else if (item is ProductLookupItem) {
          tile = EntityListTile(
            name: item.name,
            subtitle:
                '${item.categoryName}${item.barcode != null ? ' • ${item.barcode}' : ''}',
          );
        } else if (item is InvoiceDetailResponse) {
          tile = EntityListTile(
            name: item.invoiceNumber,
            subtitle:
                '${invoiceTypeLabel(item.invoiceType)} • ${formatDate(item.date)}',
            trailing: Text(formatCurrency(item.netAmount)),
            onTap: () => Get.toNamed(AppRoutes.invoiceDetailPath(item.syncId)),
          );
        } else {
          return const SizedBox.shrink();
        }
        return tile.fadeSlideInList(index: index);
      },
    );
  }
}

class EntityDetailScreen extends StatelessWidget {
  const EntityDetailScreen({
    super.key,
    required this.entityType,
    required this.syncId,
    required this.name,
  });

  final String entityType;
  final String syncId;
  final String name;

  String get _editRoute => switch (entityType) {
        'customer' => AppRoutes.customerEditPath(syncId),
        'product' => AppRoutes.productEditPath(syncId),
        'supplier' => AppRoutes.supplierEditPath(syncId),
        'investor' => AppRoutes.investorEditPath(syncId),
        _ => AppRoutes.customerEditPath(syncId),
      };

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(name)),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          GradientCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(name, style: Theme.of(context).textTheme.titleLarge),
                const SizedBox(height: 8),
                Text('SyncId: $syncId'),
              ],
            ),
          ),
          const SizedBox(height: 16),
          FilledButton.icon(
            onPressed: () => Get.toNamed(_editRoute),
            icon: const Icon(Icons.edit),
            label: Text('edit'.tr()),
          ),
        ],
      ),
    );
  }
}

class InvoiceDetailController extends GetxController {
  InvoiceDetailController({required this.syncId});

  final String syncId;

  final isLoading = true.obs;
  final Rxn<Object> error = Rxn<Object>();
  final Rxn<InvoiceDetailResponse> invoice = Rxn<InvoiceDetailResponse>();

  @override
  void onInit() {
    super.onInit();
    reload();
  }

  Future<void> reload() async {
    isLoading.value = true;
    error.value = null;
    try {
      invoice.value = await AppServices.data.getInvoiceDetail(syncId);
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}

class InvoiceDetailScreen extends StatelessWidget {
  const InvoiceDetailScreen({super.key, required this.syncId});

  final String syncId;

  @override
  Widget build(BuildContext context) {
    final controller = Get.put(
      InvoiceDetailController(syncId: syncId),
      tag: 'invoice_$syncId',
    );

    return Obx(() {
      return Scaffold(
        appBar: AppBar(title: Text('invoices'.tr())),
        body: controller.isLoading.value
            ? const ListShimmer(itemCount: 4)
            : controller.error.value != null
                ? ErrorStateWidget(
                    message: controller.error.value.toString(),
                    onRetry: controller.reload,
                  )
                : controller.invoice.value == null
                    ? EmptyStateWidget(onRetry: controller.reload)
                    : ListView(
                        padding: const EdgeInsets.all(16),
                        children: [
                          GradientCard(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  controller.invoice.value!.invoiceNumber,
                                  style:
                                      Theme.of(context).textTheme.titleLarge,
                                ),
                                const SizedBox(height: 8),
                                Text(
                                  '${invoiceTypeLabel(controller.invoice.value!.invoiceType)} • ${paymentMethodLabel(controller.invoice.value!.paymentMethod)}',
                                ),
                                Text(formatDate(controller.invoice.value!.date)),
                                if (controller.invoice.value!.customerName !=
                                    null)
                                  Text(controller.invoice.value!.customerName!),
                                if (controller.invoice.value!.supplierName !=
                                    null)
                                  Text(controller.invoice.value!.supplierName!),
                                const SizedBox(height: 12),
                                Text(
                                  formatCurrency(
                                    controller.invoice.value!.netAmount,
                                  ),
                                  style:
                                      Theme.of(context).textTheme.displaySmall,
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(height: 16),
                          ...controller.invoice.value!.items.map(
                            (item) => Card(
                              child: ListTile(
                                title: Text(item.itemName),
                                subtitle: Text(
                                  '${item.quantity} × ${formatCurrency(item.unitPrice)}',
                                ),
                                trailing:
                                    Text(formatCurrency(item.totalPrice)),
                              ),
                            ),
                          ),
                        ],
                      ),
      );
    });
  }
}
