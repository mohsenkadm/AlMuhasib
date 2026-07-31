import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../controllers/report_detail_controller.dart';
import '../../../shared/models/report_models.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/shimmer_widgets.dart';

class ReportDetailScreen extends GetView<ReportDetailController> {
  const ReportDetailScreen({super.key, required this.reportType});

  final String reportType;

  @override
  String? get tag => 'report_$reportType';

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: controller.title,
      subtitle: 'reports_subtitle'.tr(),
      body: Column(
        children: [
          if (controller.showDateFilter)
            Obx(
              () => controller.singleDate
                  ? _SingleDateFilter(
                      date: controller.to.value,
                      onPick: () => controller.pickToDate(context),
                    )
                  : AppFilterBar(
                      showDateRange: true,
                      from: controller.from.value,
                      to: controller.to.value,
                      onPickFrom: () => controller.pickFromDate(context),
                      onPickTo: () => controller.pickToDate(context),
                    ),
            ),
          if (controller.showCustomerPicker)
            Obx(
              () => _EntityPickerBar(
                icon: Icons.person_search_rounded,
                color: AppColors.moduleCyan,
                label: controller.selectedCustomer.value?.name ??
                    'select_customer'.tr(),
                onTap: () => controller.pickCustomer(context),
              ),
            ),
          if (controller.showInvestorPicker)
            Obx(
              () => _EntityPickerBar(
                icon: Icons.savings_outlined,
                color: AppColors.modulePink,
                label: controller.selectedInvestor.value?.name ??
                    'select_investor'.tr(),
                onTap: () => controller.pickInvestor(context),
              ),
            ),
          if (controller.showSupplierPicker)
            Obx(
              () => _EntityPickerBar(
                icon: Icons.local_shipping_outlined,
                color: AppColors.modulePurple,
                label: controller.selectedSupplier.value?.name ??
                    'select_supplier'.tr(),
                onTap: () => controller.pickSupplier(context),
              ),
            ),
          if (controller.showWarehousePicker)
            Obx(
              () => _EntityPickerBar(
                icon: Icons.warehouse_outlined,
                color: AppColors.moduleOrange,
                label: controller.selectedWarehouse.value?.name ??
                    'select_warehouse'.tr(),
                onTap: () => controller.pickWarehouse(context),
              ),
            ),
          if (controller.showCashBoxPicker)
            Obx(
              () => _EntityPickerBar(
                icon: Icons.point_of_sale_outlined,
                color: AppColors.moduleGreen,
                label: controller.selectedCashBox.value?.name ??
                    'select_cashbox'.tr(),
                onTap: () => controller.pickCashBox(context),
              ),
            ),
          if (controller.showBankPicker)
            Obx(
              () => _EntityPickerBar(
                icon: Icons.account_balance_rounded,
                color: AppColors.primary,
                label: controller.selectedBankAccount.value?.name ??
                    'select_bank_account'.tr(),
                onTap: () => controller.pickBankAccount(context),
              ),
            ),
          Expanded(
            child: Obx(() {
              final isLoading = controller.isLoading.value;
              final error = controller.error.value;
              final result = controller.result.value;
              final profitInvoices = List<ProfitInvoiceDetailRow>.from(
                controller.profitInvoices,
              );
              return RefreshIndicator(
                onRefresh: controller.reload,
                child: _ReportDetailBody(
                  isLoading: isLoading,
                  error: error,
                  result: result,
                  profitInvoices: profitInvoices,
                  reportType: reportType,
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

class _SingleDateFilter extends StatelessWidget {
  const _SingleDateFilter({required this.date, required this.onPick});

  final DateTime date;
  final VoidCallback onPick;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 4),
      child: Material(
        color: Theme.of(context).colorScheme.surfaceContainerLowest,
        borderRadius: BorderRadius.circular(16),
        child: InkWell(
          onTap: onPick,
          borderRadius: BorderRadius.circular(16),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
            child: Row(
              children: [
                Container(
                  width: 42,
                  height: 42,
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: const Icon(
                    Icons.event_rounded,
                    color: AppColors.primary,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'date'.tr(),
                        style: Theme.of(context).textTheme.labelMedium,
                      ),
                      Text(
                        formatDate(date),
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(
                              fontWeight: FontWeight.w800,
                            ),
                      ),
                    ],
                  ),
                ),
                const Icon(Icons.edit_calendar_outlined),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _EntityPickerBar extends StatelessWidget {
  const _EntityPickerBar({
    required this.icon,
    required this.color,
    required this.label,
    required this.onTap,
  });

  final IconData icon;
  final Color color;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 4, 20, 8),
      child: AppEntityCard(
        title: label,
        subtitle: 'tap_to_change'.tr(),
        leading: _IconBadge(icon: icon, color: color),
        trailing: Icon(Icons.unfold_more_rounded, color: color),
        onTap: onTap,
      ),
    );
  }
}

class _ReportInfoTip extends StatefulWidget {
  const _ReportInfoTip({required this.reportType});

  final String reportType;

  @override
  State<_ReportInfoTip> createState() => _ReportInfoTipState();
}

class _ReportInfoTipState extends State<_ReportInfoTip> {
  bool _expanded = true;

  @override
  Widget build(BuildContext context) {
    final info = 'report_info_${widget.reportType}'.tr();
    if (info == 'report_info_${widget.reportType}') {
      return const SizedBox.shrink();
    }
    final scheme = Theme.of(context).colorScheme;
    return Material(
      color: scheme.primaryContainer.withValues(alpha: 0.45),
      borderRadius: BorderRadius.circular(16),
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: () => setState(() => _expanded = !_expanded),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Icon(Icons.lightbulb_outline_rounded, color: scheme.primary),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      'report_tip_title'.tr(),
                      style: Theme.of(context).textTheme.titleSmall?.copyWith(
                            fontWeight: FontWeight.w800,
                          ),
                    ),
                  ),
                  Icon(
                    _expanded
                        ? Icons.expand_less_rounded
                        : Icons.expand_more_rounded,
                    color: scheme.primary,
                  ),
                ],
              ),
              if (_expanded) ...[
                const SizedBox(height: 8),
                Text(
                  info,
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        height: 1.45,
                      ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _ReportDetailBody extends StatelessWidget {
  const _ReportDetailBody({
    required this.isLoading,
    required this.error,
    required this.result,
    required this.profitInvoices,
    required this.reportType,
    required this.onRetry,
  });

  final bool isLoading;
  final Object? error;
  final dynamic result;
  final List<ProfitInvoiceDetailRow> profitInvoices;
  final String reportType;
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) {
    if (isLoading) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
        children: const [
          SizedBox(height: 12),
          ListShimmer(),
        ],
      );
    }
    if (error != null) {
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
    if (result == null) {
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

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(20, 8, 20, 120),
      children: [
        _ReportInfoTip(reportType: reportType).fadeSlideIn(),
        const SizedBox(height: 10),
        ...switch (reportType) {
          'sales' => _buildSales(context, result as SalesReportResult),
          'purchases' =>
            _buildPurchases(context, result as PurchasesReportResult),
          'profit' => _buildProfit(
              context,
              result as ProfitReportResult,
              profitInvoices,
            ),
          'balance_sheet' =>
            _buildBalanceSheet(context, result as BalanceSheetResult),
          'overdue' => _buildOverdue(context, result as OverdueResult),
          'statement' => _buildStatement(
              context,
              result as CustomerStatementResult,
            ),
          'investor_statement' => _buildInvestorStatement(
              context,
              result as InvestorStatementResult,
            ),
          'warehouse' =>
            _buildWarehouse(context, result as List<WarehouseStockRow>),
          'top_products' => _buildTopProducts(
              context,
              result as TopProductsReportResult,
            ),
          'expenses' ||
          'income_expense' ||
          'cash_flow' ||
          'installments_summary' ||
          'installments_detail' ||
          'installments_paid' ||
          'installments_unpaid' ||
          'installments_aging' ||
          'product_margin' ||
          'product_movement' ||
          'stock_health' ||
          'inventory_replenishment' ||
          'customers_overview' ||
          'suppliers_overview' ||
          'profit_comparison' ||
          'investor_profit_distributions' ||
          'capital_movement' ||
          'opening_installment_balances' ||
          'company_fees' ||
          'installment_schedule' ||
          'sales_by_payment_method' ||
          'daily_sales' ||
          'sales_by_warehouse_user' ||
          'gross_profit_margin' ||
          'operating_profit' ||
          'receivables_aging' ||
          'payables_aging' ||
          'customer_collections' ||
          'overdue_customers' ||
          'supplier_payments' ||
          'bank_account_statement' ||
          'cash_box_movement' ||
          'cash_balances_summary' ||
          'transfers' ||
          'inventory_valuation' ||
          'stock_taking' ||
          'cogs' ||
          'financial_position_summary' ||
          'profit_and_loss' ||
          'statement_of_financial_position' ||
          'supplier_statement' =>
            _buildChartAwareReport(context, reportType, result),
          _ => _buildGeneric(context, result),
        },
      ],
    );
  }

  List<Widget> _buildGeneric(BuildContext context, dynamic data) {
    if (data is List) {
      if (data.isEmpty) return [const EmptyStateWidget()];
      return [
        _SectionTitle('report_rows'.tr()),
        ...data.asMap().entries.map((e) {
          final row = e.value;
          if (row is Map) {
            final map = Map<String, dynamic>.from(row);
            final stringVals =
                map.values.whereType<String>().where((v) => v.isNotEmpty);
            final title =
                stringVals.isNotEmpty ? stringVals.first : '#${e.key + 1}';
            final subtitle = map.entries
                .take(4)
                .map((x) => '${x.key}: ${x.value}')
                .join(' • ');
            return Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: AppEntityCard(
                title: title,
                subtitle: subtitle,
              ).fadeSlideInList(index: e.key),
            );
          }
          return Padding(
            padding: const EdgeInsets.only(bottom: 8),
            child: Text(row.toString()),
          );
        }),
      ];
    }
    if (data is Map) {
      return _buildChartAwareReport(context, reportType, data);
    }
    return [Text(data.toString())];
  }

  List<Widget> _buildChartAwareReport(
    BuildContext context,
    String type,
    dynamic data,
  ) {
    if (data is! Map) return _buildGenericFallbackList(context, data);

    final map = Map<String, dynamic>.from(data);
    final kpis = <AppKpiItem>[];
    final chartWidgets = <Widget>[];
    final rowWidgets = <Widget>[];

    const chartKeys = {
      'byTypeChart',
      'statusChart',
      'sharesChart',
      'topCustomersChart',
      'byCustomerChart',
      'overdueBucketChart',
      'byCashBoxChart',
      'chart',
      'methodChart',
      'compositionChart',
      'warehouseChart',
      'userChart',
      'byInvestorChart',
      'bySupplierChart',
      'assetsChart',
      'equityLiabilitiesChart',
      'topProductsChart',
      'distributedChart',
      'reorderChart',
    };
    const dailyChartKeys = {
      'dailyChart',
      'monthlyCollectionChart',
      'monthlyDueChart',
      'dailyIncomingChart',
      'dailyOutgoingChart',
      'dueChart',
      'dailyInChart',
      'dailyOutChart',
      'dailySalesChart',
      'dailyGrossChart',
      'monthlyChart',
    };

    const colors = [
      AppColors.primary,
      AppColors.accent,
      AppColors.moduleOrange,
      AppColors.moduleGreen,
      AppColors.modulePink,
      AppColors.moduleIndigo,
      AppColors.modulePurple,
      AppColors.warning,
    ];

    map.forEach((key, value) {
      if (value is num) {
        kpis.add(
          AppKpiItem(
            title: _humanizeKey(key),
            value: value is double || '$value'.contains('.')
                ? formatCurrency(value.toDouble())
                : '$value',
            icon: Icons.analytics_outlined,
            color: AppColors.primary,
            compact: true,
          ),
        );
      } else if (value is List &&
          (key == 'monthlyChart') &&
          value.isNotEmpty &&
          value.first is Map &&
          (value.first as Map).containsKey('income')) {
        final labels = <String>[];
        final income = <double>[];
        final expense = <double>[];
        for (final row in value.whereType<Map>()) {
          final m = Map<String, dynamic>.from(row);
          labels.add('${m['month'] ?? ''}');
          income.add(_asDouble(m['income']));
          expense.add(_asDouble(m['expense']));
        }
        if (labels.isNotEmpty) {
          chartWidgets.add(
            Padding(
              padding: const EdgeInsets.only(bottom: 14),
              child: AppChartCard(
                title: 'income_vs_expense'.tr(),
                height: 210,
                legend: AppChartLegend(
                  items: [
                    ('income'.tr(), AppColors.moduleGreen),
                    ('expenses'.tr(), AppColors.moduleOrange),
                  ],
                ),
                child: AppGroupedBarChart(
                  labels: labels,
                  series: [
                    AppChartSeries(
                      label: 'income'.tr(),
                      values: income,
                      color: AppColors.moduleGreen,
                    ),
                    AppChartSeries(
                      label: 'expenses'.tr(),
                      values: expense,
                      color: AppColors.moduleOrange,
                    ),
                  ],
                  valueAsCurrency: true,
                ),
              ).fadeSlideIn(),
            ),
          );
        }
      } else if (value is List && (chartKeys.contains(key) || key == 'buckets')) {
        final points = key == 'buckets'
            ? _parseBucketAmountList(value)
            : _parseNameAmountList(value);
        if (points.isNotEmpty) {
          final sections = points
              .take(8)
              .toList()
              .asMap()
              .entries
              .map((e) => (e.value.$1, e.value.$2, colors[e.key % colors.length]))
              .toList();
          final total = sections.fold<double>(0, (s, e) => s + e.$2);
          chartWidgets.add(
            Padding(
              padding: const EdgeInsets.only(bottom: 14),
              child: AppChartCard(
                title: key == 'buckets'
                    ? 'aging_buckets'.tr()
                    : _humanizeKey(key),
                height: 180,
                child: AppDonutChart(
                  sections: sections,
                  centerLabel: 'total'.tr(),
                  centerValue: formatCurrency(total),
                  valueAsCurrency: true,
                ),
              ).fadeSlideIn(),
            ),
          );
        }
      } else if (value is List && dailyChartKeys.contains(key)) {
        final series = _parseDailyAmountList(value);
        if (series.isNotEmpty) {
          chartWidgets.add(
            Padding(
              padding: const EdgeInsets.only(bottom: 14),
              child: AppChartCard(
                title: _humanizeKey(key),
                height: 200,
                child: AppGroupedBarChart(
                  labels: series.map((e) => e.$1).toList(),
                  series: [
                    AppChartSeries(
                      label: _humanizeKey(key),
                      values: series.map((e) => e.$2).toList(),
                      color: AppColors.primary,
                    ),
                  ],
                  valueAsCurrency: true,
                ),
              ).fadeSlideIn(),
            ),
          );
        }
      } else if (value is List &&
          (key.toLowerCase().contains('rows') ||
              key == 'lines' ||
              key == 'details')) {
        if (type == 'customers_overview') {
          final outstanding = value
              .whereType<Map>()
              .map((row) {
                final m = Map<String, dynamic>.from(row);
                return (
                  '${m['customerName'] ?? m['CustomerName'] ?? ''}',
                  _asDouble(m['outstandingBalance'] ?? m['OutstandingBalance']),
                );
              })
              .where((e) => e.$1.isNotEmpty && e.$2 > 0)
              .toList()
            ..sort((a, b) => b.$2.compareTo(a.$2));
          if (outstanding.isNotEmpty) {
            chartWidgets.add(
              Padding(
                padding: const EdgeInsets.only(bottom: 14),
                child: AppChartCard(
                  title: 'top_customer_balances'.tr(),
                  height: (outstanding.take(8).length * 52.0).clamp(120, 280),
                  child: AppHorizontalBarChart(
                    points: outstanding.take(8).toList(),
                    color: AppColors.moduleCyan,
                    valueAsCurrency: true,
                  ),
                ).fadeSlideIn(),
              ),
            );
          }
        }
        rowWidgets.addAll(_buildGenericFallbackList(context, value));
      } else if (value is List) {
        rowWidgets.addAll(_buildGenericFallbackList(context, value));
      } else if (value is Map) {
        rowWidgets.add(
          Padding(
            padding: const EdgeInsets.only(bottom: 10),
            child: AppEntityCard(
              title: _humanizeKey(key),
              subtitle: value.entries
                  .take(4)
                  .map((e) => '${e.key}: ${e.value}')
                  .join(' • '),
            ),
          ),
        );
      } else if (value != null) {
        rowWidgets.add(
          Padding(
            padding: const EdgeInsets.only(bottom: 8),
            child: AppEntityCard(
              title: _humanizeKey(key),
              subtitle: value.toString(),
            ),
          ),
        );
      }
    });

    return [
      if (kpis.isNotEmpty) ...[
        AppKpiGrid(
          childAspectRatio: 1.55,
          items: kpis,
        ).fadeSlideIn(),
        const SizedBox(height: 14),
      ],
      ...chartWidgets,
      ...rowWidgets,
    ];
  }

  List<Widget> _buildGenericFallbackList(BuildContext context, dynamic data) {
    if (data is! List) return [Text(data.toString())];
    if (data.isEmpty) return [const EmptyStateWidget()];
    return [
      _SectionTitle('report_rows'.tr()),
      ...data.asMap().entries.map((e) {
        final row = e.value;
        if (row is Map) {
          final map = Map<String, dynamic>.from(row);
          final stringVals =
              map.values.whereType<String>().where((v) => v.isNotEmpty);
          final title =
              stringVals.isNotEmpty ? stringVals.first : '#${e.key + 1}';
          final subtitle = map.entries
              .take(4)
              .map((x) => '${x.key}: ${x.value}')
              .join(' • ');
          return Padding(
            padding: const EdgeInsets.only(bottom: 10),
            child: AppEntityCard(
              title: title,
              subtitle: subtitle,
            ).fadeSlideInList(index: e.key),
          );
        }
        return Padding(
          padding: const EdgeInsets.only(bottom: 8),
          child: Text(row.toString()),
        );
      }),
    ];
  }

  static List<(String, double)> _parseNameAmountList(List<dynamic> raw) {
    return raw
        .whereType<Map>()
        .map((row) {
          final m = Map<String, dynamic>.from(row);
          final name =
              '${m['name'] ?? m['category'] ?? m['customerName'] ?? m['supplierName'] ?? m['productName'] ?? m['warehouseName'] ?? m['lineName'] ?? m['bucketName'] ?? ''}';
          final amount = _asDouble(
            m['amount'] ??
                m['outstandingBalance'] ??
                m['value'] ??
                m['totalValue'] ??
                m['cogsAmount'] ??
                m['remainingAmount'],
          );
          return (name, amount);
        })
        .where((e) => e.$1.isNotEmpty)
        .toList();
  }

  static List<(String, double)> _parseBucketAmountList(List<dynamic> raw) {
    return raw
        .whereType<Map>()
        .map((row) {
          final m = Map<String, dynamic>.from(row);
          final name = '${m['bucketName'] ?? m['name'] ?? ''}';
          return (name, _asDouble(m['amount']));
        })
        .where((e) => e.$1.isNotEmpty && e.$2 > 0)
        .toList();
  }

  static List<(String, double)> _parseDailyAmountList(List<dynamic> raw) {
    return raw.whereType<Map>().map((row) {
      final m = Map<String, dynamic>.from(row);
      final dateRaw = m['date'] ?? m['month'] ?? '';
      String label;
      if (dateRaw is String && dateRaw.contains('T')) {
        label = shortDateFormat.format(DateTime.tryParse(dateRaw) ?? DateTime.now());
      } else {
        label = '$dateRaw';
        if (label.length > 7) label = label.substring(5);
      }
      return (label, _asDouble(m['amount']));
    }).toList();
  }

  static double _asDouble(dynamic value) {
    if (value == null) return 0;
    if (value is num) return value.toDouble();
    return double.tryParse(value.toString()) ?? 0;
  }

  static String _humanizeKey(String key) {
    final spaced = key
        .replaceAllMapped(RegExp(r'([a-z])([A-Z])'), (m) => '${m[1]} ${m[2]}')
        .replaceAll('Chart', '')
        .trim();
    return spaced.isEmpty ? key : spaced;
  }

  List<Widget> _buildSales(BuildContext context, SalesReportResult r) => [
        AppBalanceHeroCard(
          title: 'total'.tr(),
          value: formatCurrency(r.totalSales),
          subtitle: '${'invoice_count'.tr()}: ${r.invoiceCount}',
        ).fadeSlideIn(),
        const SizedBox(height: 14),
        AppKpiGrid(
          childAspectRatio: 1.55,
          items: [
            AppKpiItem(
              title: 'invoice_count'.tr(),
              value: '${r.invoiceCount}',
              icon: Icons.receipt_long_rounded,
              color: AppColors.moduleGreen,
              compact: true,
            ),
            AppKpiItem(
              title: 'total'.tr(),
              value: formatCurrency(r.totalSales),
              icon: Icons.payments_outlined,
              color: AppColors.primary,
              compact: true,
            ),
          ],
        ).fadeSlideIn(delayMs: 40),
        _SectionTitle('report_rows'.tr()),
        ...r.rows.asMap().entries.map(
              (e) => Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: AppEntityCard(
                  title: e.value.invoiceNumber,
                  subtitle:
                      '${e.value.customerName} • ${formatDate(e.value.date)}',
                  leading: const _IconBadge(
                    icon: Icons.point_of_sale_rounded,
                    color: AppColors.moduleGreen,
                  ),
                  trailing: _AmountText(formatCurrency(e.value.netAmount)),
                ).fadeSlideInList(index: e.key),
              ),
            ),
      ];

  List<Widget> _buildPurchases(
    BuildContext context,
    PurchasesReportResult r,
  ) =>
      [
        AppBalanceHeroCard(
          title: 'total'.tr(),
          value: formatCurrency(r.totalPurchases),
          subtitle: '${'invoice_count'.tr()}: ${r.invoiceCount}',
        ).fadeSlideIn(),
        const SizedBox(height: 14),
        AppKpiGrid(
          childAspectRatio: 1.55,
          items: [
            AppKpiItem(
              title: 'invoice_count'.tr(),
              value: '${r.invoiceCount}',
              icon: Icons.shopping_bag_outlined,
              color: AppColors.moduleOrange,
              compact: true,
            ),
            AppKpiItem(
              title: 'total'.tr(),
              value: formatCurrency(r.totalPurchases),
              icon: Icons.payments_outlined,
              color: AppColors.primary,
              compact: true,
            ),
          ],
        ).fadeSlideIn(delayMs: 40),
        _SectionTitle('report_rows'.tr()),
        ...r.rows.asMap().entries.map(
              (e) => Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: AppEntityCard(
                  title: e.value.invoiceNumber,
                  subtitle:
                      '${e.value.supplierName} • ${formatDate(e.value.date)}',
                  leading: const _IconBadge(
                    icon: Icons.shopping_bag_outlined,
                    color: AppColors.moduleOrange,
                  ),
                  trailing: _AmountText(formatCurrency(e.value.netAmount)),
                ).fadeSlideInList(index: e.key),
              ),
            ),
      ];

  List<Widget> _buildProfit(
    BuildContext context,
    ProfitReportResult r,
    List<ProfitInvoiceDetailRow> profitInvoices,
  ) =>
      [
        AppBalanceHeroCard(
          title: 'net_profit'.tr(),
          value: formatCurrency(r.netProfit),
          trendLabel: '${r.profitMargin.toStringAsFixed(1)}%',
          trendPositive: r.netProfit >= 0,
          subtitle: 'profit_margin'.tr(),
        ).fadeSlideIn(),
        const SizedBox(height: 14),
        AppKpiGrid(
          childAspectRatio: 1.45,
          items: [
            AppKpiItem(
              title: 'report_sales'.tr(),
              value: formatCurrency(r.totalSales),
              icon: Icons.trending_up_rounded,
              color: AppColors.moduleGreen,
              compact: true,
            ),
            AppKpiItem(
              title: 'report_purchases'.tr(),
              value: formatCurrency(r.totalPurchases),
              icon: Icons.trending_down_rounded,
              color: AppColors.moduleOrange,
              compact: true,
            ),
            AppKpiItem(
              title: 'total_expenses'.tr(),
              value: formatCurrency(r.totalExpenses),
              icon: Icons.account_balance_wallet_outlined,
              color: AppColors.modulePurple,
              compact: true,
            ),
            AppKpiItem(
              title: 'profit_margin'.tr(),
              value: '${r.profitMargin.toStringAsFixed(1)}%',
              icon: Icons.percent_rounded,
              color: AppColors.moduleIndigo,
              compact: true,
            ),
          ],
        ).fadeSlideIn(delayMs: 40),
        if (profitInvoices.isNotEmpty) ...[
          _SectionTitle('profit_invoice_details'.tr()),
          ...profitInvoices.asMap().entries.map(
                (e) => Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: AppEntityCard(
                    title: e.value.invoiceNumber,
                    subtitle:
                        '${e.value.customerName} • ${formatDate(e.value.date)}',
                    leading: const _IconBadge(
                      icon: Icons.insights_rounded,
                      color: AppColors.modulePurple,
                    ),
                    trailing: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        _AmountText(formatCurrency(e.value.grossProfit)),
                        Text(
                          '${e.value.marginPercent.toStringAsFixed(0)}%',
                          style: Theme.of(context).textTheme.labelMedium,
                        ),
                      ],
                    ),
                  ).fadeSlideInList(index: e.key),
                ),
              ),
        ],
      ];

  List<Widget> _buildBalanceSheet(
    BuildContext context,
    BalanceSheetResult r,
  ) =>
      [
        AppBalanceHeroCard(
          title: 'assets_total'.tr(),
          value: formatCurrency(r.assetsTotal),
          subtitle: r.isBalanced
              ? 'balance_sheet_balanced'.tr()
              : 'equity_total'.tr(),
          trendLabel: r.isBalanced ? '✓' : null,
          trendPositive: r.isBalanced,
        ).fadeSlideIn(),
        const SizedBox(height: 14),
        AppKpiGrid(
          childAspectRatio: 1.35,
          items: [
            AppKpiItem(
              title: 'equity_total'.tr(),
              value: formatCurrency(r.equityTotal),
              icon: Icons.account_balance_rounded,
              color: AppColors.moduleIndigo,
              compact: true,
            ),
            AppKpiItem(
              title: 'liabilities_total'.tr(),
              value: formatCurrency(r.liabilitiesTotal),
              icon: Icons.credit_card_outlined,
              color: AppColors.warning,
              compact: true,
            ),
            AppKpiItem(
              title: 'sales_profit'.tr(),
              value: formatCurrency(r.salesProfit),
              icon: Icons.trending_up_rounded,
              color: AppColors.moduleGreen,
              compact: true,
            ),
            AppKpiItem(
              title: 'cost_of_sales'.tr(),
              value: formatCurrency(r.costOfSales),
              icon: Icons.trending_down_rounded,
              color: AppColors.moduleOrange,
              compact: true,
            ),
            AppKpiItem(
              title: 'supplier_payables'.tr(),
              value: formatCurrency(r.supplierPayables),
              icon: Icons.local_shipping_outlined,
              color: AppColors.moduleCyan,
              compact: true,
            ),
            AppKpiItem(
              title: 'investor_deposits'.tr(),
              value: formatCurrency(r.investorDeposits),
              icon: Icons.savings_outlined,
              color: AppColors.modulePink,
              compact: true,
            ),
            AppKpiItem(
              title: 'customer_debts'.tr(),
              value: formatCurrency(r.customerDebts),
              icon: Icons.people_outline_rounded,
              color: AppColors.error,
              compact: true,
            ),
            AppKpiItem(
              title: 'inventory_value'.tr(),
              value: formatCurrency(r.inventoryValue),
              icon: Icons.warehouse_outlined,
              color: AppColors.primary,
              compact: true,
            ),
          ],
        ).fadeSlideIn(delayMs: 40),
      ];

  List<Widget> _buildOverdue(BuildContext context, OverdueResult r) => [
        AppBalanceHeroCard(
          title: 'total'.tr(),
          value: formatCurrency(r.totalOverdueAmount),
          subtitle: '${'customers'.tr()}: ${r.overdueCustomerCount}',
          trendPositive: false,
          trendLabel: 'report_overdue'.tr(),
        ).fadeSlideIn(),
        const SizedBox(height: 14),
        AppKpiGrid(
          childAspectRatio: 1.55,
          items: [
            AppKpiItem(
              title: 'customers'.tr(),
              value: '${r.overdueCustomerCount}',
              icon: Icons.people_outline_rounded,
              color: AppColors.warning,
              compact: true,
            ),
            AppKpiItem(
              title: 'total'.tr(),
              value: formatCurrency(r.totalOverdueAmount),
              icon: Icons.warning_amber_rounded,
              color: AppColors.error,
              compact: true,
            ),
          ],
        ).fadeSlideIn(delayMs: 40),
        _SectionTitle('report_rows'.tr()),
        ...r.rows.asMap().entries.map(
              (e) => Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: AppEntityCard(
                  title: e.value.customerName,
                  subtitle:
                      '${e.value.phone} • ${e.value.overdueDays} ${'days'.tr()}',
                  leading: const _IconBadge(
                    icon: Icons.warning_amber_rounded,
                    color: AppColors.warning,
                  ),
                  trailing: _AmountText(
                    formatCurrency(e.value.overdueAmount),
                    tone: AppColors.error,
                  ),
                  status: '${e.value.overdueDays} ${'days'.tr()}',
                  statusTone: AppColors.warning,
                ).fadeSlideInList(index: e.key),
              ),
            ),
      ];

  List<Widget> _buildStatement(
    BuildContext context,
    CustomerStatementResult r,
  ) =>
      [
        AppBalanceHeroCard(
          title: r.customerName,
          value: formatCurrency(r.balance),
          subtitle: 'report_statement'.tr(),
        ).fadeSlideIn(),
        _SectionTitle('report_rows'.tr()),
        ...r.rows.asMap().entries.map(
              (e) => Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: AppEntityCard(
                  title: e.value.description,
                  subtitle: formatDate(e.value.date),
                  leading: const _IconBadge(
                    icon: Icons.receipt_long_rounded,
                    color: AppColors.moduleCyan,
                  ),
                  trailing: _AmountText(formatCurrency(e.value.runningBalance)),
                ).fadeSlideInList(index: e.key),
              ),
            ),
      ];

  List<Widget> _buildInvestorStatement(
    BuildContext context,
    InvestorStatementResult r,
  ) =>
      [
        AppBalanceHeroCard(
          title: r.investorName,
          value: formatCurrency(r.balance),
          subtitle: 'report_investor_statement'.tr(),
        ).fadeSlideIn(),
        _SectionTitle('report_rows'.tr()),
        ...r.rows.asMap().entries.map(
              (e) => Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: AppEntityCard(
                  title: e.value.description,
                  subtitle: formatDate(e.value.date),
                  leading: const _IconBadge(
                    icon: Icons.savings_outlined,
                    color: AppColors.modulePink,
                  ),
                  trailing: _AmountText(formatCurrency(e.value.runningBalance)),
                ).fadeSlideInList(index: e.key),
              ),
            ),
      ];

  List<Widget> _buildWarehouse(
    BuildContext context,
    List<WarehouseStockRow> rows,
  ) =>
      [
        if (rows.isNotEmpty)
          AppBalanceHeroCard(
            title: 'report_warehouse'.tr(),
            value: formatCurrency(
              rows.fold<double>(0, (sum, r) => sum + r.totalValue),
            ),
            subtitle: '${rows.length} ${'products'.tr()}',
          ).fadeSlideIn(),
        _SectionTitle('report_rows'.tr()),
        ...rows.asMap().entries.map(
              (e) => Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: AppEntityCard(
                  title: e.value.productName,
                  subtitle: e.value.warehouseName,
                  leading: const _IconBadge(
                    icon: Icons.inventory_2_outlined,
                    color: AppColors.moduleOrange,
                  ),
                  trailing: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        '${'quantity'.tr()}: ${e.value.quantity}',
                        style: Theme.of(context).textTheme.labelMedium,
                      ),
                      _AmountText(formatCurrency(e.value.totalValue)),
                    ],
                  ),
                ).fadeSlideInList(index: e.key),
              ),
            ),
      ];

  List<Widget> _buildTopProducts(
    BuildContext context,
    TopProductsReportResult r,
  ) =>
      [
        AppBalanceHeroCard(
          title: 'total'.tr(),
          value: formatCurrency(r.totalRevenue),
          subtitle: 'report_top_products'.tr(),
        ).fadeSlideIn(),
        _SectionTitle('report_rows'.tr()),
        ...r.rows.asMap().entries.map(
              (e) => Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: AppEntityCard(
                  title: e.value.productName,
                  subtitle: '${'quantity'.tr()}: ${e.value.quantitySold}',
                  leading: _RankBadge(rank: e.value.rank),
                  trailing: _AmountText(formatCurrency(e.value.revenue)),
                ).fadeSlideInList(index: e.key),
              ),
            ),
      ];
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle(this.title);

  final String title;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(0, 18, 0, 10),
      child: Text(
        title,
        style: Theme.of(context).textTheme.titleMedium?.copyWith(
              fontWeight: FontWeight.w800,
            ),
      ),
    ).fadeSlideIn(delayMs: 60);
  }
}

class _IconBadge extends StatelessWidget {
  const _IconBadge({required this.icon, required this.color});

  final IconData icon;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 48,
      height: 48,
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.14),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Icon(icon, color: color),
    );
  }
}

class _RankBadge extends StatelessWidget {
  const _RankBadge({required this.rank});

  final int rank;

  @override
  Widget build(BuildContext context) {
    final color = switch (rank) {
      1 => AppColors.warning,
      2 => AppColors.textDarkMuted,
      3 => AppColors.moduleOrange,
      _ => AppColors.primary,
    };
    return Container(
      width: 48,
      height: 48,
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.14),
        borderRadius: BorderRadius.circular(14),
      ),
      alignment: Alignment.center,
      child: Text(
        '$rank',
        style: TextStyle(
          color: color,
          fontWeight: FontWeight.w900,
          fontSize: 18,
        ),
      ),
    );
  }
}

class _AmountText extends StatelessWidget {
  const _AmountText(this.value, {this.tone});

  final String value;
  final Color? tone;

  @override
  Widget build(BuildContext context) {
    return Text(
      value,
      style: Theme.of(context).textTheme.titleMedium?.copyWith(
            fontWeight: FontWeight.w800,
            color: tone ?? AppColors.primary,
          ),
    );
  }
}
