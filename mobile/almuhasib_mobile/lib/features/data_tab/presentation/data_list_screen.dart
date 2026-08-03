import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../controllers/data_list_controller.dart';
import '../controllers/invoice_detail_controller.dart';
import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../shared/models/master_data_models.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../../../shared/widgets/shimmer_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';

class DataListScreen extends GetView<DataListController> {
  const DataListScreen({super.key, required this.listType});

  final String listType;

  @override
  String? get tag => 'data_list_$listType';

  Color get _accent => switch (listType) {
        'customers' => AppColors.primary,
        'products' => AppColors.moduleGreen,
        'invoices' => AppColors.moduleOrange,
        'suppliers' => AppColors.modulePurple,
        'investors' => AppColors.moduleCyan,
        'warehouses' => AppColors.moduleIndigo,
        _ => AppColors.primary,
      };

  IconData get _icon => switch (listType) {
        'customers' => Icons.people_outline_rounded,
        'products' => Icons.inventory_2_outlined,
        'invoices' => Icons.receipt_long_rounded,
        'suppliers' => Icons.local_shipping_outlined,
        'investors' => Icons.savings_outlined,
        'warehouses' => Icons.warehouse_outlined,
        _ => Icons.folder_outlined,
      };

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

    return AppPageScaffold(
      title: controller.title,
      subtitle: 'data_subtitle'.tr(),
      actions: [
        if (controller.fabRoute != null)
          IconButton(
            tooltip: 'add'.tr(),
            onPressed: () async {
              final refreshed = await Get.toNamed<bool>(controller.fabRoute!);
              if (refreshed == true) controller.reload();
            },
            icon: const Icon(Icons.add_circle_outline_rounded),
          ),
      ],
      body: Column(
        children: [
          if (isInvoices)
            Obx(() {
              return AppFilterBar(
                onSearchChanged: controller.updateSearch,
                filterChips: invoiceTypeFilters,
                onFilterSelected: controller.updateInvoiceTypeFilter,
                showDateRange: true,
                from: controller.from.value,
                to: controller.to.value,
                onPickFrom: () => controller.pickFromDate(context),
                onPickTo: () => controller.pickToDate(context),
                onClear: controller.clearFilters,
              );
            })
          else
            AppFilterBar(onSearchChanged: controller.updateSearch),
          if (isInvoices) _PaymentMethodFilters(controller: controller),
          Expanded(
            child: Obx(() {
              final isLoading = controller.isLoading.value;
              final error = controller.error.value;
              final items = controller.items.toList(growable: false);
              return RefreshIndicator(
                onRefresh: controller.reload,
                child: _DataListBody(
                  isLoading: isLoading,
                  error: error,
                  items: items,
                  listType: listType,
                  accent: _accent,
                  icon: _icon,
                  detailRouteFor: controller.detailRouteFor,
                  onRetry: controller.reload,
                ),
              );
            }),
          ),
        ],
      ),
    );
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
      padding: const EdgeInsets.fromLTRB(20, 0, 20, 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'payment_method'.tr(),
            style: Theme.of(context).textTheme.labelLarge?.copyWith(
                  fontWeight: FontWeight.w700,
                ),
          ),
          const SizedBox(height: 8),
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
    required this.isLoading,
    required this.error,
    required this.items,
    required this.listType,
    required this.accent,
    required this.icon,
    required this.detailRouteFor,
    required this.onRetry,
  });

  final bool isLoading;
  final Object? error;
  final List<dynamic> items;
  final String listType;
  final Color accent;
  final IconData icon;
  final String? Function(LookupItem item) detailRouteFor;
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) {
    if (isLoading && items.isEmpty) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
        children: const [
          SizedBox(height: 8),
          ListShimmer(),
        ],
      );
    }
    if (error != null && items.isEmpty) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          SizedBox(
            height: MediaQuery.sizeOf(context).height * 0.5,
            child: ErrorStateWidget(
              message: AppExceptionHandler.messageFor(error),
              onRetry: onRetry,
            ),
          ),
        ],
      );
    }
    if (items.isEmpty) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          SizedBox(
            height: MediaQuery.sizeOf(context).height * 0.5,
            child: EmptyStateWidget(onRetry: onRetry),
          ),
        ],
      );
    }

    return ListView.builder(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 32),
      itemCount: items.length + 1,
      itemBuilder: (context, index) {
        if (index == 0) {
          return Padding(
            padding: const EdgeInsets.only(bottom: 14),
            child: _buildStatsHeader().fadeSlideIn(),
          );
        }

        final item = items[index - 1];
        final rowIndex = index - 1;
        return Padding(
          padding: const EdgeInsets.only(bottom: 10),
          child: _buildRow(context, item, rowIndex).fadeSlideInList(
            index: rowIndex.clamp(0, 12),
          ),
        );
      },
    );
  }

  Widget _buildStatsHeader() {
    final count = items.length;
    switch (listType) {
      case 'customers':
      case 'suppliers':
      case 'investors':
        final balances = items
            .whereType<LookupItem>()
            .map((e) => e.balance ?? 0)
            .toList();
        final totalBalance = balances.fold<double>(0, (s, b) => s + b);
        final withBalance = balances.where((b) => b != 0).length;
        final positive = balances.where((b) => b > 0).length;
        return PageStatsHeader(
          heroTitle: listType == 'customers'
              ? 'total_outstanding'.tr()
              : 'balance'.tr(),
          heroValue: formatCurrency(totalBalance),
          heroSubtitle: '$count ${'records_count'.tr()}',
          trendPositive: totalBalance <= 0,
          stats: [
            StatsChipData(
              label: 'records_count'.tr(),
              value: '$count',
              icon: icon,
              color: accent,
            ),
            StatsChipData(
              label: 'with_balance'.tr(),
              value: '$withBalance',
              icon: Icons.account_balance_wallet_outlined,
              color: AppColors.moduleCyan,
            ),
            StatsChipData(
              label: 'debtors'.tr(),
              value: '$positive',
              icon: Icons.trending_up_rounded,
              color: AppColors.warning,
            ),
          ],
        );
      case 'invoices':
        final invoices = items.whereType<InvoiceDetailResponse>().toList();
        final totalNet =
            invoices.fold<double>(0, (s, i) => s + i.netAmount);
        final sales = invoices
            .where((i) => i.invoiceType == 1 || i.invoiceType == 2)
            .fold<double>(0, (s, i) => s + i.netAmount);
        final purchases = invoices
            .where((i) => i.invoiceType == 0 || i.invoiceType == 3)
            .fold<double>(0, (s, i) => s + i.netAmount);
        return PageStatsHeader(
          heroTitle: 'invoices'.tr(),
          heroValue: formatCurrency(totalNet),
          heroSubtitle: '$count ${'records_count'.tr()}',
          stats: [
            StatsChipData(
              label: 'records_count'.tr(),
              value: '$count',
              icon: Icons.receipt_long_rounded,
              color: accent,
            ),
            StatsChipData(
              label: 'sale'.tr(),
              value: formatCurrency(sales),
              icon: Icons.trending_up_rounded,
              color: AppColors.success,
            ),
            StatsChipData(
              label: 'purchase'.tr(),
              value: formatCurrency(purchases),
              icon: Icons.trending_down_rounded,
              color: AppColors.error,
            ),
          ],
        );
      case 'products':
        final products = items.whereType<ProductLookupItem>().toList();
        final withPrices =
            products.where((p) => p.prices.isNotEmpty).length;
        return PageStatsHeader(
          heroTitle: 'products'.tr(),
          heroValue: '$count',
          heroSubtitle: 'records_count'.tr(),
          stats: [
            StatsChipData(
              label: 'records_count'.tr(),
              value: '$count',
              icon: icon,
              color: accent,
            ),
            StatsChipData(
              label: 'with_prices'.tr(),
              value: '$withPrices',
              icon: Icons.sell_outlined,
              color: AppColors.moduleOrange,
            ),
          ],
        );
      default:
        return PageStatsHeader(
          heroTitle: switch (listType) {
            'warehouses' => 'warehouses'.tr(),
            _ => 'data_title'.tr(),
          },
          heroValue: '$count',
          heroSubtitle: 'records_count'.tr(),
          stats: [
            StatsChipData(
              label: 'records_count'.tr(),
              value: '$count',
              icon: icon,
              color: accent,
            ),
          ],
        );
    }
  }

  Widget _buildRow(BuildContext context, dynamic item, int index) {
    if (item is ProductLookupItem) {
      final priceSummary = item.prices.isEmpty
          ? null
          : item.prices
              .map((p) => '${p.pricingTypeName}: ${formatCurrency(p.salePrice)}')
              .take(2)
              .join(' • ');
      return AppEntityCard(
        title: item.name,
        subtitle: [
          item.categoryName,
          if (item.barcode != null && item.barcode!.isNotEmpty) item.barcode!,
          if (priceSummary != null) priceSummary,
        ].where((e) => e.isNotEmpty).join(' • '),
        leading: _LeadingBadge(icon: icon, color: accent, letter: item.name),
        trailing: Icon(Icons.chevron_left_rounded, color: accent),
        onTap: () {
          final route = detailRouteFor(item);
          if (route != null) Get.toNamed(route, arguments: item);
        },
      );
    }

    if (item is LookupItem && listType != 'invoices') {
      final showBalance = item.balance != null &&
          (listType == 'customers' ||
              listType == 'suppliers' ||
              listType == 'investors');
      final balanceText = showBalance
          ? '${'balance'.tr()}: ${formatCurrency(item.balance!)}'
          : null;
      return AppEntityCard(
        title: item.name,
        subtitle: [
          if (item.extra != null && item.extra!.isNotEmpty) item.extra!,
          if (balanceText != null) balanceText,
        ].join(' • '),
        leading: _LeadingBadge(icon: icon, color: accent, letter: item.name),
        trailing: showBalance
            ? Text(
                formatCurrency(item.balance!),
                style: Theme.of(context).textTheme.titleSmall?.copyWith(
                      fontWeight: FontWeight.w800,
                      color: item.balance! > 0
                          ? AppColors.error
                          : AppColors.success,
                    ),
              )
            : Icon(Icons.chevron_left_rounded, color: accent),
        onTap: () {
          final route = detailRouteFor(item);
          if (route != null) Get.toNamed(route, arguments: item);
        },
      );
    }

    if (item is InvoiceDetailResponse) {
      final isSale = item.invoiceType == 1 || item.invoiceType == 2;
      final tone = isSale ? AppColors.success : AppColors.error;
      return AppEntityCard(
        title: item.invoiceNumber,
        subtitle:
            '${invoiceTypeLabel(item.invoiceType)} • ${formatDate(item.date)}',
        leading: _LeadingBadge(
          icon: isSale ? Icons.south_west_rounded : Icons.north_east_rounded,
          color: tone,
        ),
        trailing: Text(
          formatCurrency(item.netAmount),
          style: TextStyle(
            fontWeight: FontWeight.w800,
            color: tone,
            fontSize: 15,
          ),
        ),
        status: invoiceTypeLabel(item.invoiceType),
        statusTone: tone,
        onTap: () => Get.toNamed(AppRoutes.invoiceDetailPath(item.syncId)),
      );
    }

    return const SizedBox.shrink();
  }
}

class _LeadingBadge extends StatelessWidget {
  const _LeadingBadge({
    required this.icon,
    required this.color,
    this.letter,
  });

  final IconData icon;
  final Color color;
  final String? letter;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 48,
      height: 48,
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.14),
        borderRadius: BorderRadius.circular(14),
      ),
      alignment: Alignment.center,
      child: letter != null && letter!.isNotEmpty
          ? Text(
              letter![0].toUpperCase(),
              style: TextStyle(
                color: color,
                fontWeight: FontWeight.w900,
                fontSize: 18,
              ),
            )
          : Icon(icon, color: color),
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
  LookupItem? _customer;
  bool _loadingProduct = false;

  String get _editRoute => switch (widget.entityType) {
        'customer' => AppRoutes.customerEditPath(widget.syncId),
        'product' => AppRoutes.productEditPath(widget.syncId),
        'supplier' => AppRoutes.supplierEditPath(widget.syncId),
        'investor' => AppRoutes.investorEditPath(widget.syncId),
        _ => AppRoutes.customerEditPath(widget.syncId),
      };

  Color get _accent => switch (widget.entityType) {
        'product' => AppColors.moduleGreen,
        'supplier' => AppColors.modulePurple,
        'investor' => AppColors.moduleCyan,
        _ => AppColors.primary,
      };

  IconData get _icon => switch (widget.entityType) {
        'product' => Icons.inventory_2_outlined,
        'supplier' => Icons.local_shipping_outlined,
        'investor' => Icons.savings_outlined,
        _ => Icons.person_outline_rounded,
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
    } else if (widget.entityType == 'customer') {
      if (args is LookupItem) {
        _customer = args;
      } else {
        _loadCustomer();
      }
    }
  }

  Future<void> _loadCustomer() async {
    try {
      final customers = await AppServices.data.getCustomers();
      for (final c in customers) {
        if (c.syncId == widget.syncId) {
          if (mounted) setState(() => _customer = c);
          break;
        }
      }
    } catch (_) {}
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
    final customer = _customer;
    final displayName = widget.name.isNotEmpty
        ? widget.name
        : (product?.name ?? customer?.name ?? '');
    final customerBalance = customer?.balance;

    return AppPageScaffold(
      title: displayName,
      subtitle: switch (widget.entityType) {
        'customer' => 'customers'.tr(),
        'product' => 'products'.tr(),
        'supplier' => 'suppliers'.tr(),
        'investor' => 'investors'.tr(),
        _ => null,
      },
      actions: [
        IconButton(
          tooltip: 'edit'.tr(),
          onPressed: () => Get.toNamed(
            _editRoute,
            arguments: product ?? customer,
          ),
          icon: const Icon(Icons.edit_outlined),
        ),
      ],
      body: _loadingProduct
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.fromLTRB(20, 12, 20, 32),
              children: [
                AppBalanceHeroCard(
                  title: widget.entityType == 'customer'
                      ? 'balance'.tr()
                      : displayName,
                  value: widget.entityType == 'customer'
                      ? formatCurrency(customerBalance ?? 0)
                      : product?.categoryName.isNotEmpty == true
                          ? product!.categoryName
                          : switch (widget.entityType) {
                              'product' => 'products'.tr(),
                              'supplier' => 'suppliers'.tr(),
                              'investor' => 'investors'.tr(),
                              _ => displayName,
                            },
                  subtitle: widget.entityType == 'customer'
                      ? (customer?.extra ?? displayName)
                      : product?.barcode,
                ).fadeSlideIn(),
                const SizedBox(height: 14),
                if (widget.entityType == 'customer') ...[
                  AppEntityCard(
                    title: displayName,
                    subtitle: [
                      if (customer?.extra != null &&
                          customer!.extra!.isNotEmpty)
                        '${'phone'.tr()}: ${customer.extra}',
                      'SyncId: ${widget.syncId}',
                    ].join('\n'),
                    leading: _LeadingBadge(icon: _icon, color: _accent),
                  ).fadeSlideIn(delayMs: 40),
                  const SizedBox(height: 12),
                  AppEntityCard(
                    title: 'report_statement'.tr(),
                    subtitle: 'view_customer_statement'.tr(),
                    leading: const _LeadingBadge(
                      icon: Icons.receipt_long_outlined,
                      color: AppColors.moduleCyan,
                    ),
                    trailing: const Icon(Icons.chevron_left_rounded),
                    onTap: () => Get.toNamed(
                      AppRoutes.reportDetailPath('statement'),
                      arguments: {
                        'customerSyncId': widget.syncId,
                        'customerId': customer?.id,
                        'customerName': displayName,
                      },
                    ),
                  ).fadeSlideIn(delayMs: 80),
                ] else ...[
                AppEntityCard(
                  title: 'details'.tr(),
                  subtitle: product != null
                      ? [
                          if (product.categoryName.isNotEmpty)
                            '${'category'.tr()}: ${product.categoryName}',
                          if (product.barcode != null &&
                              product.barcode!.isNotEmpty)
                            '${'barcode'.tr()}: ${product.barcode}',
                        ].join('\n')
                      : 'SyncId: ${widget.syncId}',
                  leading: _LeadingBadge(icon: _icon, color: _accent),
                ).fadeSlideIn(delayMs: 40),
                ],
                if (product != null) ...[
                  const SizedBox(height: 18),
                  Text(
                    'product_prices'.tr(),
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.w800,
                        ),
                  ).fadeSlideIn(delayMs: 60),
                  const SizedBox(height: 10),
                  if (product.prices.isEmpty)
                    AppEntityCard(
                      title: 'no_product_prices'.tr(),
                      leading: const _LeadingBadge(
                        icon: Icons.price_change_outlined,
                        color: AppColors.warning,
                      ),
                    )
                  else
                    ...product.prices.asMap().entries.map(
                          (entry) => Padding(
                            padding: const EdgeInsets.only(bottom: 10),
                            child: AppEntityCard(
                              title: entry.value.pricingTypeName,
                              subtitle:
                                  '${'sale_price'.tr()}: ${formatCurrency(entry.value.salePrice)} • ${'purchase_price'.tr()}: ${formatCurrency(entry.value.purchasePrice)}',
                              leading: const _LeadingBadge(
                                icon: Icons.sell_outlined,
                                color: AppColors.modulePink,
                              ),
                              trailing: const Icon(Icons.edit_outlined),
                              onTap: () async {
                                final refreshed = await Get.toNamed<bool>(
                                  AppRoutes.productPriceEditPath(
                                    entry.value.syncId,
                                  ),
                                  arguments: entry.value,
                                );
                                if (refreshed == true) _loadProduct();
                              },
                            ).fadeSlideInList(index: entry.key),
                          ),
                        ),
                  const SizedBox(height: 8),
                  FilledButton.tonalIcon(
                    onPressed: () async {
                      final refreshed = await Get.toNamed<bool>(
                        AppRoutes.productPriceNew,
                        arguments: product.syncId,
                      );
                      if (refreshed == true) _loadProduct();
                    },
                    icon: const Icon(Icons.add_rounded),
                    label: Text('add_product_price'.tr()),
                  ),
                ],
                const SizedBox(height: 16),
                FilledButton.icon(
                  onPressed: () => Get.toNamed(
                    _editRoute,
                    arguments: product ?? customer,
                  ),
                  icon: const Icon(Icons.edit_rounded),
                  label: Text('edit'.tr()),
                ).fadeSlideIn(delayMs: 120),
              ],
            ),
    );
  }
}

class InvoiceDetailScreen extends GetView<InvoiceDetailController> {
  const InvoiceDetailScreen({super.key, required this.syncId});

  final String syncId;

  @override
  String? get tag => 'invoice_$syncId';

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: 'invoice_details'.tr(),
      subtitle: 'invoices'.tr(),
      body: Obx(() {
        final isLoading = controller.isLoading.value;
        final error = controller.error.value;
        final invoice = controller.invoice.value;

        if (isLoading && invoice == null) {
          return const ListShimmer(itemCount: 4);
        }
        if (error != null && invoice == null) {
          return ErrorStateWidget(
            message: AppExceptionHandler.messageFor(error),
            onRetry: controller.reload,
          );
        }
        if (invoice == null) {
          return EmptyStateWidget(onRetry: controller.reload);
        }

        final isSale = invoice.invoiceType == 1 || invoice.invoiceType == 2;
        final tone = isSale ? AppColors.success : AppColors.error;

        return ListView(
          padding: const EdgeInsets.fromLTRB(20, 12, 20, 32),
          children: [
            AppBalanceHeroCard(
              title: invoice.invoiceNumber,
              value: formatCurrency(invoice.netAmount),
              subtitle:
                  '${invoiceTypeLabel(invoice.invoiceType)} • ${formatDate(invoice.date)}',
              trendLabel: paymentMethodLabel(invoice.paymentMethod),
              trendPositive: isSale,
            ).fadeSlideIn(),
            const SizedBox(height: 12),
            StatsStrip(
              items: [
                StatsChipData(
                  label: 'paid_amount'.tr(),
                  value: formatCurrency(invoice.paidAmount),
                  icon: Icons.check_circle_outline,
                  color: AppColors.success,
                ),
                StatsChipData(
                  label: 'total_remaining'.tr(),
                  value: formatCurrency(invoice.remainingAmount),
                  icon: Icons.pending_outlined,
                  color: invoice.remainingAmount > 0
                      ? AppColors.warning
                      : AppColors.moduleCyan,
                ),
                StatsChipData(
                  label: 'items'.tr(),
                  value: '${invoice.items.length}',
                  icon: Icons.inventory_2_outlined,
                  color: AppColors.moduleOrange,
                ),
              ],
            ).fadeSlideIn(delayMs: 20),
            const SizedBox(height: 14),
            AppKpiGrid(
              childAspectRatio: 1.5,
              items: [
                AppKpiItem(
                  title: 'date'.tr(),
                  value: formatDate(invoice.date),
                  icon: Icons.event_rounded,
                  color: AppColors.primary,
                  compact: true,
                ),
                AppKpiItem(
                  title: 'payment_method'.tr(),
                  value: paymentMethodLabel(invoice.paymentMethod),
                  icon: Icons.payments_outlined,
                  color: tone,
                  compact: true,
                ),
              ],
            ).fadeSlideIn(delayMs: 40),
            if (invoice.items.length >= 2) ...[
              const SizedBox(height: 14),
              AppChartCard(
                title: 'invoice_lines_chart'.tr(),
                height: (invoice.items.take(6).length * 48.0).clamp(100, 260),
                child: AppHorizontalBarChart(
                  points: invoice.items
                      .take(6)
                      .map((e) => (e.itemName, e.totalPrice))
                      .toList(),
                  color: tone,
                  valueAsCurrency: true,
                ),
              ).fadeSlideIn(delayMs: 50),
            ],
            if (invoice.customerName != null || invoice.supplierName != null) ...[
              const SizedBox(height: 10),
              AppEntityCard(
                title: invoice.customerName ?? invoice.supplierName ?? '',
                subtitle: invoice.customerName != null
                    ? 'customers'.tr()
                    : 'suppliers'.tr(),
                leading: _LeadingBadge(
                  icon: invoice.customerName != null
                      ? Icons.person_outline_rounded
                      : Icons.local_shipping_outlined,
                  color: AppColors.moduleCyan,
                ),
              ).fadeSlideIn(delayMs: 60),
            ],
            const SizedBox(height: 18),
            Text(
              'items'.tr(),
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.w800,
                  ),
            ).fadeSlideIn(delayMs: 80),
            const SizedBox(height: 10),
            ...invoice.items.asMap().entries.map(
                  (entry) => Padding(
                    padding: const EdgeInsets.only(bottom: 10),
                    child: AppEntityCard(
                      title: entry.value.itemName,
                      subtitle:
                          '${entry.value.quantity} × ${formatCurrency(entry.value.unitPrice)}',
                      leading: const _LeadingBadge(
                        icon: Icons.inventory_2_outlined,
                        color: AppColors.moduleOrange,
                      ),
                      trailing: Text(
                        formatCurrency(entry.value.totalPrice),
                        style: const TextStyle(
                          fontWeight: FontWeight.w800,
                          color: AppColors.primary,
                        ),
                      ),
                    ).fadeSlideInList(index: entry.key),
                  ),
                ),
          ],
        );
      }),
    );
  }
}
