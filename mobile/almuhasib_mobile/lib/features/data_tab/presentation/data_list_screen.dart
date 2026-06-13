import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/providers/core_providers.dart';
import '../../../shared/models/master_data_models.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/entity_list_tile.dart';
import '../../../shared/widgets/search_filter_bar.dart';
import '../../../shared/widgets/shimmer_widgets.dart';

class DataListScreen extends ConsumerStatefulWidget {
  const DataListScreen({super.key, required this.listType});

  final String listType;

  @override
  ConsumerState<DataListScreen> createState() => _DataListScreenState();
}

class _DataListScreenState extends ConsumerState<DataListScreen> {
  bool _loading = true;
  Object? _error;
  List<dynamic> _items = [];
  String _search = '';
  int? _invoiceTypeFilter;
  int? _paymentFilter;
  DateTime _from = DateTime.now().subtract(const Duration(days: 90));
  DateTime _to = DateTime.now();

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final repo = ref.read(dataRepositoryProvider);
      final items = switch (widget.listType) {
        'customers' => await repo.getCustomers(search: _search),
        'products' => await repo.getProducts(search: _search),
        'suppliers' => await repo.getSuppliers(search: _search),
        'investors' => await repo.getInvestors(search: _search),
        'warehouses' => await repo.getWarehouses(search: _search),
        'invoices' => (await repo.getInvoices(
            from: _from,
            to: _to,
            search: _search,
            invoiceType: _invoiceTypeFilter,
            paymentMethod: _paymentFilter,
          ))
            .items,
        _ => <dynamic>[],
      };
      setState(() => _items = items);
    } catch (e) {
      setState(() => _error = e);
    } finally {
      setState(() => _loading = false);
    }
  }

  String get _title => switch (widget.listType) {
        'customers' => 'customers'.tr(),
        'products' => 'products'.tr(),
        'suppliers' => 'suppliers'.tr(),
        'investors' => 'investors'.tr(),
        'warehouses' => 'warehouses'.tr(),
        'invoices' => 'invoices'.tr(),
        _ => 'data_title'.tr(),
      };

  String? get _fabRoute => switch (widget.listType) {
        'customers' => '/data/customer/new',
        'products' => '/data/product/new',
        'suppliers' => '/data/supplier/new',
        'investors' => '/data/investor/new',
        'invoices' => '/data/invoice/new',
        _ => null,
      };

  @override
  Widget build(BuildContext context) {
    final invoiceFilters = widget.listType == 'invoices'
        ? [
            const FilterChipOption(id: '0', label: 'شراء'),
            const FilterChipOption(id: '1', label: 'بيع'),
            const FilterChipOption(id: '2', label: 'قسط'),
            const FilterChipOption(id: '3', label: 'مرتجع'),
          ]
        : <FilterChipOption>[];

    return Scaffold(
      appBar: AppBar(title: Text(_title)),
      floatingActionButton: _fabRoute != null
          ? FloatingActionButton(
              onPressed: () async {
                final refreshed = await context.push<bool>(_fabRoute!);
                if (refreshed == true) _load();
              },
              child: const Icon(Icons.add),
            )
          : null,
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
            child: SearchFilterBar(
              onSearchChanged: (v) {
                _search = v;
                _load();
              },
              filterChips: invoiceFilters,
              onFilterSelected: widget.listType == 'invoices'
                  ? (id) {
                      _invoiceTypeFilter =
                          id == null ? null : int.tryParse(id);
                      _load();
                    }
                  : null,
            ),
          ),
          if (widget.listType == 'invoices')
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              child: Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () async {
                        final picked = await showDatePicker(
                          context: context,
                          initialDate: _from,
                          firstDate: DateTime(2020),
                          lastDate: DateTime.now(),
                        );
                        if (picked != null) {
                          setState(() => _from = picked);
                          _load();
                        }
                      },
                      child: Text('${'from_date'.tr()}\n${formatDate(_from)}'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () async {
                        final picked = await showDatePicker(
                          context: context,
                          initialDate: _to,
                          firstDate: DateTime(2020),
                          lastDate: DateTime.now(),
                        );
                        if (picked != null) {
                          setState(() => _to = picked);
                          _load();
                        }
                      },
                      child: Text('${'to_date'.tr()}\n${formatDate(_to)}'),
                    ),
                  ),
                ],
              ),
            ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _load,
              child: _buildBody(),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) return const ListShimmer();
    if (_error != null) {
      return ErrorStateWidget(message: _error.toString(), onRetry: _load);
    }
    if (_items.isEmpty) return EmptyStateWidget(onRetry: _load);

    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: _items.length,
      separatorBuilder: (_, __) => const SizedBox(height: 8),
      itemBuilder: (context, index) {
        final item = _items[index];
        Widget tile;
        if (item is LookupItem && widget.listType != 'invoices') {
          tile = EntityListTile(
            name: item.name,
            subtitle: item.extra,
            onTap: () => _openEntityDetail(item),
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
            onTap: () => context.push('/data/invoice/${item.syncId}'),
          );
        } else {
          return const SizedBox.shrink();
        }
        return tile.fadeSlideInList(index: index);
      },
    );
  }

  void _openEntityDetail(LookupItem item) {
    final route = switch (widget.listType) {
      'customers' => '/data/customer/${item.syncId}',
      'products' => '/data/product/${item.syncId}',
      'suppliers' => '/data/supplier/${item.syncId}',
      'investors' => '/data/investor/${item.syncId}',
      _ => null,
    };
    if (route != null) context.push(route, extra: item);
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

  @override
  Widget build(BuildContext context) {
    final editRoute = '/data/$entityType/$syncId/edit';
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
            onPressed: () => context.push(editRoute),
            icon: const Icon(Icons.edit),
            label: Text('edit'.tr()),
          ),
        ],
      ),
    );
  }
}

class InvoiceDetailScreen extends ConsumerStatefulWidget {
  const InvoiceDetailScreen({super.key, required this.syncId});

  final String syncId;

  @override
  ConsumerState<InvoiceDetailScreen> createState() =>
      _InvoiceDetailScreenState();
}

class _InvoiceDetailScreenState extends ConsumerState<InvoiceDetailScreen> {
  bool _loading = true;
  Object? _error;
  InvoiceDetailResponse? _invoice;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final invoice =
          await ref.read(dataRepositoryProvider).getInvoiceDetail(widget.syncId);
      setState(() => _invoice = invoice);
    } catch (e) {
      setState(() => _error = e);
    } finally {
      setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('invoices'.tr())),
      body: _loading
          ? const ListShimmer(itemCount: 4)
          : _error != null
              ? ErrorStateWidget(message: _error.toString(), onRetry: _load)
              : _invoice == null
                  ? EmptyStateWidget(onRetry: _load)
                  : ListView(
                      padding: const EdgeInsets.all(16),
                      children: [
                        GradientCard(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                _invoice!.invoiceNumber,
                                style: Theme.of(context).textTheme.titleLarge,
                              ),
                              const SizedBox(height: 8),
                              Text(
                                '${invoiceTypeLabel(_invoice!.invoiceType)} • ${paymentMethodLabel(_invoice!.paymentMethod)}',
                              ),
                              Text(formatDate(_invoice!.date)),
                              if (_invoice!.customerName != null)
                                Text(_invoice!.customerName!),
                              if (_invoice!.supplierName != null)
                                Text(_invoice!.supplierName!),
                              const SizedBox(height: 12),
                              Text(
                                formatCurrency(_invoice!.netAmount),
                                style: Theme.of(context).textTheme.displaySmall,
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 16),
                        ..._invoice!.items.map(
                          (item) => Card(
                            child: ListTile(
                              title: Text(item.itemName),
                              subtitle: Text(
                                '${item.quantity} × ${formatCurrency(item.unitPrice)}',
                              ),
                              trailing: Text(formatCurrency(item.totalPrice)),
                            ),
                          ),
                        ),
                      ],
                    ),
    );
  }
}
