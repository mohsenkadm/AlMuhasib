import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/providers/core_providers.dart';
import '../../../shared/models/master_data_models.dart';
import '../../../shared/models/report_models.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/lookup_picker_sheet.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/shimmer_widgets.dart';

class ReportDetailScreen extends ConsumerStatefulWidget {
  const ReportDetailScreen({super.key, required this.reportType});

  final String reportType;

  @override
  ConsumerState<ReportDetailScreen> createState() => _ReportDetailScreenState();
}

class _ReportDetailScreenState extends ConsumerState<ReportDetailScreen> {
  DateTime _from = DateTime.now().subtract(const Duration(days: 30));
  DateTime _to = DateTime.now();
  LookupItem? _selectedCustomer;
  LookupItem? _selectedInvestor;
  bool _loading = true;
  Object? _error;
  dynamic _result;

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
      final repo = ref.read(reportsRepositoryProvider);
      dynamic result;
      switch (widget.reportType) {
        case 'sales':
          result = await repo.getSalesReport(_from, _to);
        case 'purchases':
          result = await repo.getPurchasesReport(_from, _to);
        case 'profit':
          result = await repo.getProfitReport(_from, _to);
        case 'overdue':
          result = await repo.getOverdueReport();
        case 'warehouse':
          result = await repo.getWarehouseReport();
        case 'top_products':
          result = await repo.getTopProductsReport(_from, _to);
        case 'statement':
          if (_selectedCustomer == null) {
            final customers = await ref.read(dataRepositoryProvider).getCustomers();
            if (customers.isNotEmpty) _selectedCustomer = customers.first;
          }
          if (_selectedCustomer != null) {
            result = await repo.getCustomerStatement(
              _selectedCustomer!.syncId,
              from: _from,
              to: _to,
            );
          }
        case 'investor_statement':
          if (_selectedInvestor == null) {
            final investors = await ref.read(dataRepositoryProvider).getInvestors();
            if (investors.isNotEmpty) _selectedInvestor = investors.first;
          }
          if (_selectedInvestor != null) {
            result = await repo.getInvestorStatement(
              _selectedInvestor!.syncId,
              from: _from,
              to: _to,
            );
          }
        default:
          result = null;
      }
      setState(() => _result = result);
    } catch (e) {
      setState(() => _error = e);
    } finally {
      setState(() => _loading = false);
    }
  }

  Future<void> _pickFromDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _from,
      firstDate: DateTime(2020),
      lastDate: DateTime.now(),
    );
    if (picked != null) {
      setState(() => _from = picked);
      await _load();
    }
  }

  Future<void> _pickToDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _to,
      firstDate: DateTime(2020),
      lastDate: DateTime.now(),
    );
    if (picked != null) {
      setState(() => _to = picked);
      await _load();
    }
  }

  Future<void> _pickCustomer() async {
    final repo = ref.read(dataRepositoryProvider);
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_customer'.tr(),
      loadItems: (search) => repo.getCustomers(search: search),
    );
    if (selected != null) {
      setState(() => _selectedCustomer = selected);
      await _load();
    }
  }

  Future<void> _pickInvestor() async {
    final repo = ref.read(dataRepositoryProvider);
    final selected = await showLookupPickerSheet<LookupItem>(
      context: context,
      title: 'select_investor'.tr(),
      loadItems: (search) => repo.getInvestors(search: search),
    );
    if (selected != null) {
      setState(() => _selectedInvestor = selected);
      await _load();
    }
  }

  String get _title {
    switch (widget.reportType) {
      case 'sales':
        return 'report_sales'.tr();
      case 'purchases':
        return 'report_purchases'.tr();
      case 'profit':
        return 'report_profit'.tr();
      case 'overdue':
        return 'report_overdue'.tr();
      case 'statement':
        return 'report_statement'.tr();
      case 'investor_statement':
        return 'report_investor_statement'.tr();
      case 'warehouse':
        return 'report_warehouse'.tr();
      case 'top_products':
        return 'report_top_products'.tr();
      default:
        return 'reports_title'.tr();
    }
  }

  @override
  Widget build(BuildContext context) {
    final showDateFilter = !{'overdue', 'warehouse'}.contains(widget.reportType);

    return Scaffold(
      appBar: AppBar(title: Text(_title)),
      body: Column(
        children: [
          if (showDateFilter)
            Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: _pickFromDate,
                      icon: const Icon(Icons.calendar_today, size: 16),
                      label: Text('${'from_date'.tr()}\n${formatDate(_from)}'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: _pickToDate,
                      icon: const Icon(Icons.calendar_today, size: 16),
                      label: Text('${'to_date'.tr()}\n${formatDate(_to)}'),
                    ),
                  ),
                ],
              ),
            ),
          if (widget.reportType == 'statement')
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: OutlinedButton.icon(
                onPressed: _pickCustomer,
                icon: const Icon(Icons.person_search),
                label: Text(_selectedCustomer?.name ?? 'select_customer'.tr()),
              ),
            ),
          if (widget.reportType == 'investor_statement')
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: OutlinedButton.icon(
                onPressed: _pickInvestor,
                icon: const Icon(Icons.savings_outlined),
                label: Text(_selectedInvestor?.name ?? 'select_investor'.tr()),
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
    if (_result == null) return EmptyStateWidget(onRetry: _load);

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
      children: [
        ...switch (widget.reportType) {
          'sales' => _buildSales(_result as SalesReportResult),
          'purchases' => _buildPurchases(_result as PurchasesReportResult),
          'profit' => _buildProfit(_result as ProfitReportResult),
          'overdue' => _buildOverdue(_result as OverdueResult),
          'statement' => _buildStatement(_result as CustomerStatementResult),
          'investor_statement' =>
              _buildInvestorStatement(_result as InvestorStatementResult),
          'warehouse' => _buildWarehouse(_result as List<WarehouseStockRow>),
          'top_products' => _buildTopProducts(_result as TopProductsReportResult),
          _ => [EmptyStateWidget()],
        },
      ],
    );
  }

  List<Widget> _buildSales(SalesReportResult r) => [
        _SummaryCard('total'.tr(), formatCurrency(r.totalSales)),
        _SummaryCard('invoice_count'.tr(), '${r.invoiceCount}'),
        ...r.rows.map(
          (row) => Card(
            child: ListTile(
              title: Text(row.invoiceNumber),
              subtitle: Text('${row.customerName} • ${formatDate(row.date)}'),
              trailing: Text(formatCurrency(row.netAmount)),
            ),
          ),
        ),
      ];

  List<Widget> _buildPurchases(PurchasesReportResult r) => [
        _SummaryCard('total'.tr(), formatCurrency(r.totalPurchases)),
        _SummaryCard('invoice_count'.tr(), '${r.invoiceCount}'),
        ...r.rows.map(
          (row) => Card(
            child: ListTile(
              title: Text(row.invoiceNumber),
              subtitle: Text('${row.supplierName} • ${formatDate(row.date)}'),
              trailing: Text(formatCurrency(row.netAmount)),
            ),
          ),
        ),
      ];

  List<Widget> _buildProfit(ProfitReportResult r) => [
        _SummaryCard('total'.tr(), formatCurrency(r.netProfit)),
        _SummaryCard('report_sales'.tr(), formatCurrency(r.totalSales)),
        _SummaryCard('report_purchases'.tr(), formatCurrency(r.totalPurchases)),
        _SummaryCard('net_profit'.tr(), '${r.profitMargin.toStringAsFixed(1)}%'),
      ];

  List<Widget> _buildOverdue(OverdueResult r) => [
        _SummaryCard('total'.tr(), formatCurrency(r.totalOverdueAmount)),
        _SummaryCard('customers'.tr(), '${r.overdueCustomerCount}'),
        ...r.rows.map(
          (row) => Card(
            child: ListTile(
              title: Text(row.customerName),
              subtitle: Text('${row.phone} • ${row.overdueDays} يوم'),
              trailing: Text(formatCurrency(row.overdueAmount)),
            ),
          ),
        ),
      ];

  List<Widget> _buildStatement(CustomerStatementResult r) => [
        _SummaryCard(r.customerName, formatCurrency(r.balance)),
        ...r.rows.map(
          (row) => Card(
            child: ListTile(
              title: Text(row.description),
              subtitle: Text(formatDate(row.date)),
              trailing: Text(formatCurrency(row.runningBalance)),
            ),
          ),
        ),
      ];

  List<Widget> _buildInvestorStatement(InvestorStatementResult r) => [
        _SummaryCard(r.investorName, formatCurrency(r.balance)),
        ...r.rows.map(
          (row) => Card(
            child: ListTile(
              title: Text(row.description),
              subtitle: Text(formatDate(row.date)),
              trailing: Text(formatCurrency(row.runningBalance)),
            ),
          ),
        ),
      ];

  List<Widget> _buildWarehouse(List<WarehouseStockRow> rows) => rows
      .map(
        (row) => Card(
          child: ListTile(
            title: Text(row.productName),
            subtitle: Text(row.warehouseName),
            trailing: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text('${'quantity'.tr()}: ${row.quantity}'),
                Text(formatCurrency(row.totalValue)),
              ],
            ),
          ),
        ),
      )
      .toList();

  List<Widget> _buildTopProducts(TopProductsReportResult r) => [
        _SummaryCard('total'.tr(), formatCurrency(r.totalRevenue)),
        ...r.rows.map(
          (row) => Card(
            child: ListTile(
              leading: CircleAvatar(child: Text('${row.rank}')),
              title: Text(row.productName),
              subtitle: Text('${'quantity'.tr()}: ${row.quantitySold}'),
              trailing: Text(formatCurrency(row.revenue)),
            ),
          ),
        ),
      ];
}

class _SummaryCard extends StatelessWidget {
  const _SummaryCard(this.title, this.value);

  final String title;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: GradientCard(
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(title, style: Theme.of(context).textTheme.titleMedium),
            Text(value, style: Theme.of(context).textTheme.titleLarge),
          ],
        ),
      ).fadeSlideIn(),
    );
  }
}
