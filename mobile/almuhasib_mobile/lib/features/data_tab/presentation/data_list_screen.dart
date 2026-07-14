import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../controllers/data_list_controller.dart';
import '../controllers/invoice_detail_controller.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/models/master_data_models.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/entity_list_tile.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../../../shared/widgets/shimmer_widgets.dart';

import '../../../shared/widgets/design_system/design_system.dart';

class DataListScreen extends GetView<DataListController> {
  DataListScreen({super.key, required this.listType});

  final String listType;

  @override
  String? get tag => 'data_list_$listType';

  @override
  Widget build(BuildContext context) {
    final isInvoices = listType == 'invoices';
    final invoiceTypeFilters = isInvoices
        ? [
            FilterChipOption(id: '0', label: 'purchase'.tr()),
            FilterChipOption(id: '1', label: 'sale'.tr()),
            FilterChipOption(id: '2', label: 'installment'.tr()),
            FilterChipOption(id: '3', label: 'purchase_return'.tr()),
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
            AppFilterBar(
              onSearchChanged: controller.updateSearch,
              filterChips: invoiceTypeFilters,
              onFilterSelected:
                  isInvoices ? controller.updateInvoiceTypeFilter : null,
              showDateRange: isInvoices,
              from: isInvoices ? controller.from.value : null,
              to: isInvoices ? controller.to.value : null,
              onPickFrom: isInvoices
                  ? () => controller.pickFromDate(context)
                  : null,
              onPickTo:
                  isInvoices ? () => controller.pickToDate(context) : null,
              onClear: isInvoices ? controller.clearFilters : null,
            ),
            if (isInvoices) _PaymentMethodFilters(controller: controller),
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

class _PaymentMethodFilters extends StatelessWidget {
  const _PaymentMethodFilters({required this.controller});

  final DataListController controller;

  @override
  Widget build(BuildContext context) {
    final chips = [
      FilterChipOption(id: '0', label: 'cash'.tr()),
      FilterChipOption(id: '1', label: 'credit'.tr()),
      FilterChipOption(id: '2', label: 'installment'.tr()),
    ];

    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'payment_method'.tr(),
            style: Theme.of(context).textTheme.labelLarge,
          ),
          const SizedBox(height: 6),
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Row(
              children: chips.map((chip) {
                return Padding(
                  padding: const EdgeInsetsDirectional.only(end: 8),
                  child: Obx(() {
                    final selected =
                        controller.paymentFilter.value?.toString() == chip.id;
                    return FilterChip(
                      label: Text(chip.label),
                      selected: selected,
                      onSelected: (_) => controller.updatePaymentFilter(
                        selected ? null : chip.id,
                      ),
                    );
                  }),
                );
              }).toList(),
            ),
          ),
        ],
      ),
    );
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
        if (item is ProductLookupItem) {
          final priceSummary = item.prices.isEmpty
              ? null
              : item.prices
                  .map(
                    (p) =>
                        '${p.pricingTypeName}: ${formatCurrency(p.salePrice)}',
                  )
                  .take(2)
                  .join(' • ');
          tile = EntityListTile(
            name: item.name,
            subtitle: [
              item.categoryName,
              if (item.barcode != null && item.barcode!.isNotEmpty) item.barcode!,
              if (priceSummary != null) priceSummary,
            ].join(' • '),
            onTap: () {
              final route = controller.detailRouteFor(item);
              if (route != null) {
                Get.toNamed(route, arguments: item);
              }
            },
          );
        } else if (item is LookupItem && listType != 'invoices') {
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

class EntityDetailScreen extends StatefulWidget {
  const EntityDetailScreen({
    super.key,
    required this.entityType,
    required this.syncId,
    required this.name,
  });

  final String entityType;
  final String syncId;
  final String name;

  @override
  State<EntityDetailScreen> createState() => _EntityDetailScreenState();
}

class _EntityDetailScreenState extends State<EntityDetailScreen> {
  ProductLookupItem? _product;
  bool _loadingProduct = false;

  String get _editRoute => switch (widget.entityType) {
        'customer' => AppRoutes.customerEditPath(widget.syncId),
        'product' => AppRoutes.productEditPath(widget.syncId),
        'supplier' => AppRoutes.supplierEditPath(widget.syncId),
        'investor' => AppRoutes.investorEditPath(widget.syncId),
        _ => AppRoutes.customerEditPath(widget.syncId),
      };

  @override
  void initState() {
    super.initState();
    final args = Get.arguments;
    if (widget.entityType == 'product') {
      if (args is ProductLookupItem) {
        _product = args;
      } else {
        _loadProduct();
      }
    }
  }

  Future<void> _loadProduct() async {
    setState(() => _loadingProduct = true);
    try {
      final products = await AppServices.data.getProducts();
      for (final p in products) {
        if (p.syncId == widget.syncId) {
          if (mounted) setState(() => _product = p);
          break;
        }
      }
    } catch (_) {
    } finally {
      if (mounted) setState(() => _loadingProduct = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final product = _product;
    final displayName =
        widget.name.isNotEmpty ? widget.name : (product?.name ?? '');

    return Scaffold(
      appBar: AppBar(title: Text(displayName)),
      body: _loadingProduct
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                GradientCard(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        displayName,
                        style: Theme.of(context).textTheme.titleLarge,
                      ),
                      const SizedBox(height: 8),
                      if (product != null) ...[
                        Text('${'category'.tr()}: ${product.categoryName}'),
                        if (product.barcode != null &&
                            product.barcode!.isNotEmpty)
                          Text('${'barcode'.tr()}: ${product.barcode}'),
                      ] else
                        Text('SyncId: ${widget.syncId}'),
                    ],
                  ),
                ),
                if (product != null) ...[
                  const SizedBox(height: 16),
                  Text(
                    'product_prices'.tr(),
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: 8),
                  if (product.prices.isEmpty)
                    Text('no_product_prices'.tr())
                  else
                    ...product.prices.map(
                      (price) => Card(
                        child: ListTile(
                          title: Text(price.pricingTypeName),
                          subtitle: Text(
                            '${'sale_price'.tr()}: ${formatCurrency(price.salePrice)} • ${'purchase_price'.tr()}: ${formatCurrency(price.purchasePrice)}',
                          ),
                          trailing: const Icon(Icons.edit_outlined),
                          onTap: () async {
                            final refreshed = await Get.toNamed<bool>(
                              AppRoutes.productPriceEditPath(price.syncId),
                              arguments: price,
                            );
                            if (refreshed == true) _loadProduct();
                          },
                        ),
                      ),
                    ),
                  const SizedBox(height: 8),
                  OutlinedButton.icon(
                    onPressed: () async {
                      final refreshed = await Get.toNamed<bool>(
                        AppRoutes.productPriceNew,
                        arguments: product.syncId,
                      );
                      if (refreshed == true) _loadProduct();
                    },
                    icon: const Icon(Icons.add),
                    label: Text('add_product_price'.tr()),
                  ),
                ],
                const SizedBox(height: 16),
                FilledButton.icon(
                  onPressed: () =>
                      Get.toNamed(_editRoute, arguments: product),
                  icon: const Icon(Icons.edit),
                  label: Text('edit'.tr()),
                ),
              ],
            ),
    );
  }
}

class InvoiceDetailScreen extends GetView<InvoiceDetailController> {
  InvoiceDetailScreen({super.key, required this.syncId});

  final String syncId;

  @override
  String? get tag => 'invoice_$syncId';

  @override
  Widget build(BuildContext context) {
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
