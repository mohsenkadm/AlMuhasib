import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/system_themes.dart';
import '../../../shared/utils/formatters.dart';
import '../models/gold_shop_models.dart';

class GoldReportsHubScreen extends StatelessWidget {
  const GoldReportsHubScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final groups = [
      (
        'gold_reports_ops'.tr(),
        [
          _ReportLink('sales', 'gold_report_sales'.tr(), Icons.point_of_sale),
          _ReportLink(
            'purchases',
            'gold_report_purchases'.tr(),
            Icons.shopping_cart_outlined,
          ),
          _ReportLink(
            'sale-returns',
            'gold_report_returns'.tr(),
            Icons.undo,
          ),
          _ReportLink(
            'exchanges',
            'gold_report_exchanges'.tr(),
            Icons.swap_horiz,
          ),
        ],
      ),
      (
        'gold_reports_stock_credit'.tr(),
        [
          _ReportLink('stock', 'gold_report_stock'.tr(), Icons.inventory_2),
          _ReportLink('credit', 'gold_report_credit'.tr(), Icons.credit_card),
          _ReportLink('aging', 'gold_report_aging'.tr(), Icons.timelapse),
        ],
      ),
      (
        'gold_reports_cash_karat'.tr(),
        [
          _ReportLink(
            'cash-movement',
            'gold_report_cash_movement'.tr(),
            Icons.account_balance_wallet,
          ),
          _ReportLink(
            'karat-movement',
            'gold_report_karat_movement'.tr(),
            Icons.scale,
          ),
          _ReportLink(
            'profitability',
            'gold_report_profitability'.tr(),
            Icons.trending_up,
          ),
          _ReportLink(
            'user-performance',
            'gold_report_user_performance'.tr(),
            Icons.people_outline,
          ),
          _ReportLink(
            'deleted-invoices',
            'gold_report_deleted'.tr(),
            Icons.delete_outline,
          ),
        ],
      ),
    ];

    return Scaffold(
      appBar: AppBar(title: Text('gold_reports_hub'.tr())),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
        children: [
          for (final g in groups) ...[
            Padding(
              padding: const EdgeInsets.only(top: 8, bottom: 8),
              child: Text(
                g.$1,
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w800,
                      color: SystemThemes.goldPrimary,
                    ),
              ),
            ),
            for (final r in g.$2)
              Card(
                margin: const EdgeInsets.only(bottom: 8),
                child: ListTile(
                  leading: Icon(r.icon, color: SystemThemes.goldPrimary),
                  title: Text(r.title),
                  trailing: const Icon(Icons.chevron_left),
                  onTap: () => Get.toNamed(
                    AppRoutes.goldShopReportDetailPath(r.key),
                  ),
                ),
              ),
          ],
        ],
      ),
    );
  }
}

class _ReportLink {
  const _ReportLink(this.key, this.title, this.icon);
  final String key;
  final String title;
  final IconData icon;
}

class GoldReportDetailController extends GetxController {
  GoldReportDetailController(this.reportKey);

  final String reportKey;
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final kpis = <(String, String)>[].obs;
  final rows = <Map<String, String>>[].obs;
  DateTime? from;
  DateTime? to;

  bool get needsDates =>
      reportKey != 'stock' && reportKey != 'credit' && reportKey != 'aging';

  @override
  void onInit() {
    super.onInit();
    final now = DateTime.now();
    from = DateTime(now.year, now.month, 1);
    to = now;
    load();
  }

  Future<void> pickFrom(BuildContext context) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: from ?? DateTime.now(),
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (picked != null) {
      from = picked;
      load();
    }
  }

  Future<void> pickTo(BuildContext context) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: to ?? DateTime.now(),
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (picked != null) {
      to = picked;
      load();
    }
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      final data = await AppServices.goldShop.getReportRaw(
        reportKey,
        from: needsDates ? from : null,
        to: needsDates ? to : null,
      );
      _parse(data);
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }

  void _parse(Map<String, dynamic> data) {
    final nextKpis = <(String, String)>[];
    final nextRows = <Map<String, String>>[];

    void addKpi(String label, dynamic value) {
      if (value == null) return;
      nextKpis.add((label, _fmt(value)));
    }

    switch (reportKey) {
      case 'stock':
        addKpi('gold_weight'.tr(), data['totalGrams']);
        addKpi('gold_total'.tr(), data['totalValue']);
        addKpi('gold_report_low_stock'.tr(), data['lowStockCount']);
        for (final r in (data['rows'] as List? ?? [])) {
          final m = r as Map<String, dynamic>;
          nextRows.add({
            'title': 'عيار ${m['karatValue']}',
            'subtitle':
                '${_fmt(m['gramsOnHand'])} غ · ${_fmt(m['stockValue'])}',
          });
        }
      case 'sales':
        addKpi('gold_report_sales'.tr(), data['totalSalesIqd']);
        addKpi('gold_report_purchases'.tr(), data['totalPurchasesIqd']);
        addKpi('gold_weight'.tr(), data['totalWeightSoldGrams']);
        for (final r in (data['rows'] as List? ?? [])) {
          nextRows.add(_invoiceRow(r as Map<String, dynamic>));
        }
      case 'credit':
        addKpi('gold_kpi_credit'.tr(), data['totalRemainingIqd']);
        addKpi('gold_report_overdue'.tr(), data['overdueCount']);
        for (final r in (data['rows'] as List? ?? [])) {
          final m = r as Map<String, dynamic>;
          final inv = m['invoice'] as Map<String, dynamic>? ?? {};
          nextRows.add({
            'title': inv['invoiceNumber']?.toString() ?? '—',
            'subtitle':
                '${inv['customerName'] ?? '—'} · ${m['daysOpen']} ${'gold_days'.tr()}${m['isOverdue'] == true ? ' · ${'gold_report_overdue'.tr()}' : ''}',
            'trailing': _fmt(inv['remainingAmount']),
          });
        }
      case 'aging':
        addKpi('0-30', data['bucket0To30']);
        addKpi('31-60', data['bucket31To60']);
        addKpi('61-90', data['bucket61To90']);
        addKpi('90+', data['bucket90Plus']);
        for (final r in (data['rows'] as List? ?? [])) {
          final m = r as Map<String, dynamic>;
          final inv = m['invoice'] as Map<String, dynamic>? ?? {};
          nextRows.add({
            'title': inv['invoiceNumber']?.toString() ?? '—',
            'subtitle':
                '${inv['customerName'] ?? '—'} · ${m['bucketLabel']} · ${m['daysOpen']} ${'gold_days'.tr()}',
            'trailing': _fmt(inv['remainingAmount']),
          });
        }
      case 'purchases':
      case 'deleted-invoices':
        addKpi('gold_total'.tr(), data['totalCount']);
        for (final r in (data['items'] as List? ?? [])) {
          nextRows.add(_invoiceRow(r as Map<String, dynamic>));
        }
      case 'sale-returns':
        addKpi('gold_total'.tr(), data['totalCount']);
        for (final r in (data['items'] as List? ?? [])) {
          final m = r as Map<String, dynamic>;
          final inv = m['invoice'] as Map<String, dynamic>? ?? m;
          nextRows.add({
            ..._invoiceRow(inv),
            'subtitle':
                '${_invoiceRow(inv)['subtitle']}${m['relatedInvoiceNumber'] != null ? ' · ${m['relatedInvoiceNumber']}' : ''}',
          });
        }
      case 'exchanges':
        addKpi('gold_total'.tr(), data['totalCount']);
        for (final r in (data['items'] as List? ?? [])) {
          final m = r as Map<String, dynamic>;
          final inv = m['invoice'] as Map<String, dynamic>? ?? m;
          nextRows.add({
            'title': inv['invoiceNumber']?.toString() ?? '—',
            'subtitle':
                'وارد ${_fmt(m['inWeightGrams'])} غ / صادر ${_fmt(m['outWeightGrams'])} غ · فرق ${_fmt(m['exchangeCashDifference'])}',
            'trailing': _fmt(inv['totalAmountIqd']),
            'id': inv['id']?.toString() ?? '',
          });
        }
      case 'cash-movement':
        addKpi('gold_total'.tr(), data['totalCount']);
        for (final r in (data['items'] as List? ?? [])) {
          final m = r as Map<String, dynamic>;
          nextRows.add({
            'title': '${m['movementType']} · ${m['reference']}',
            'subtitle':
                '${m['partyName']} · ${m['cashBoxName']} · ${m['currency']}',
            'trailing':
                '+${_fmt(m['amountIn'])} / -${_fmt(m['amountOut'])}',
          });
        }
      case 'karat-movement':
        addKpi('gold_total'.tr(), data['totalCount']);
        for (final r in (data['items'] as List? ?? [])) {
          final m = r as Map<String, dynamic>;
          nextRows.add({
            'title': m['karatName']?.toString() ?? 'عيار ${m['karatValue']}',
            'subtitle':
                'شراء ${_fmt(m['purchasedGrams'])} · بيع ${_fmt(m['soldGrams'])} · مرتجع ${_fmt(m['returnedGrams'])}',
            'trailing': _fmt(m['closingGrams']),
          });
        }
      case 'profitability':
        addKpi('gold_total'.tr(), data['totalCount']);
        for (final r in (data['items'] as List? ?? [])) {
          final m = r as Map<String, dynamic>;
          nextRows.add({
            'title': m['karatName']?.toString() ?? 'عيار ${m['karatValue']}',
            'subtitle':
                '${_fmt(m['weightSoldGrams'])} غ · مبيعات ${_fmt(m['salesGoldValue'])}',
            'trailing': _fmt(m['grossProfit']),
          });
        }
      case 'user-performance':
        addKpi('gold_total'.tr(), data['totalCount']);
        for (final r in (data['items'] as List? ?? [])) {
          final m = r as Map<String, dynamic>;
          nextRows.add({
            'title': m['userName']?.toString() ?? '—',
            'subtitle':
                'بيع ${m['salesCount']} · شراء ${m['purchasesCount']} · تبديل ${m['exchangeCount']} · مرتجع ${m['returnCount']}',
            'trailing': _fmt(m['salesAmountIqd']),
          });
        }
      default:
        addKpi('gold_total'.tr(), data['totalCount'] ?? data.length);
    }

    kpis.assignAll(nextKpis);
    rows.assignAll(nextRows);
  }

  Map<String, String> _invoiceRow(Map<String, dynamic> inv) {
    return {
      'title': inv['invoiceNumber']?.toString() ?? '—',
      'subtitle':
          '${inv['customerName'] ?? '—'} · ${formatDate(DateTime.tryParse(inv['invoiceDate']?.toString() ?? '') ?? DateTime.now())}',
      'trailing': _fmt(inv['totalAmountIqd'] ?? inv['totalAmount']),
      'id': inv['id']?.toString() ?? '',
    };
  }

  String _fmt(dynamic v) {
    if (v is num) {
      if (v == v.roundToDouble()) return v.toInt().toString();
      return v.toStringAsFixed(2);
    }
    return v?.toString() ?? '—';
  }
}

class GoldReportDetailScreen extends StatelessWidget {
  const GoldReportDetailScreen({super.key, required this.reportKey});

  final String reportKey;

  @override
  Widget build(BuildContext context) {
    final c = Get.put(
      GoldReportDetailController(reportKey),
      tag: 'gold_report_$reportKey',
    );
    final title = switch (reportKey) {
      'sales' => 'gold_report_sales'.tr(),
      'purchases' => 'gold_report_purchases'.tr(),
      'sale-returns' => 'gold_report_returns'.tr(),
      'exchanges' => 'gold_report_exchanges'.tr(),
      'stock' => 'gold_report_stock'.tr(),
      'credit' => 'gold_report_credit'.tr(),
      'aging' => 'gold_report_aging'.tr(),
      'cash-movement' => 'gold_report_cash_movement'.tr(),
      'karat-movement' => 'gold_report_karat_movement'.tr(),
      'profitability' => 'gold_report_profitability'.tr(),
      'user-performance' => 'gold_report_user_performance'.tr(),
      'deleted-invoices' => 'gold_report_deleted'.tr(),
      _ => 'gold_reports_hub'.tr(),
    };

    return Scaffold(
      appBar: AppBar(
        title: Text(title),
        actions: [
          IconButton(onPressed: c.load, icon: const Icon(Icons.refresh)),
        ],
      ),
      body: Obx(() {
        if (c.isLoading.value && c.rows.isEmpty && c.kpis.isEmpty) {
          return const Center(child: CircularProgressIndicator());
        }
        if (c.error.value != null && c.rows.isEmpty) {
          return Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(c.error.value.toString()),
                FilledButton(onPressed: c.load, child: Text('retry'.tr())),
              ],
            ),
          );
        }
        return RefreshIndicator(
          onRefresh: c.load,
          child: ListView(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
            children: [
              if (c.needsDates)
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton(
                        onPressed: () => c.pickFrom(context),
                        child: Text(
                          c.from == null
                              ? 'gold_from'.tr()
                              : formatDate(c.from!),
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: OutlinedButton(
                        onPressed: () => c.pickTo(context),
                        child: Text(
                          c.to == null ? 'gold_to'.tr() : formatDate(c.to!),
                        ),
                      ),
                    ),
                  ],
                ),
              if (c.kpis.isNotEmpty) ...[
                const SizedBox(height: 12),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: c.kpis
                      .map(
                        (k) => Chip(
                          label: Text('${k.$1}: ${k.$2}'),
                          backgroundColor:
                              SystemThemes.goldPrimary.withValues(alpha: 0.12),
                        ),
                      )
                      .toList(),
                ),
              ],
              const SizedBox(height: 12),
              if (c.rows.isEmpty)
                Padding(
                  padding: const EdgeInsets.all(32),
                  child: Center(child: Text('no_data'.tr())),
                )
              else
                ...c.rows.map(
                  (r) => Card(
                    margin: const EdgeInsets.only(bottom: 8),
                    child: ListTile(
                      title: Text(r['title'] ?? ''),
                      subtitle: Text(r['subtitle'] ?? ''),
                      trailing: r['trailing'] == null
                          ? null
                          : Text(
                              r['trailing']!,
                              style: const TextStyle(fontWeight: FontWeight.w700),
                            ),
                      onTap: () {
                        final id = int.tryParse(r['id'] ?? '');
                        if (id != null && id > 0) {
                          Get.toNamed(AppRoutes.goldShopSaleDetailPath(id));
                        }
                      },
                    ),
                  ),
                ),
            ],
          ),
        );
      }),
    );
  }
}

class GoldStatementController extends GetxController {
  GoldStatementController({
    required this.partyId,
    required this.isSupplier,
    required this.partyName,
  });

  final int partyId;
  final bool isSupplier;
  final String partyName;
  final isLoading = true.obs;
  final error = Rxn<Object>();
  final data = Rxn<GoldStatementDto>();
  DateTime? from;
  DateTime? to;

  @override
  void onInit() {
    super.onInit();
    final now = DateTime.now();
    from = DateTime(now.year, now.month, 1);
    to = now;
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      data.value = isSupplier
          ? await AppServices.goldShop
              .getSupplierStatement(partyId, from: from, to: to)
          : await AppServices.goldShop
              .getCustomerStatement(partyId, from: from, to: to);
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }
}

class GoldStatementScreen extends StatelessWidget {
  const GoldStatementScreen({
    super.key,
    required this.partyId,
    required this.isSupplier,
    this.partyName = '',
  });

  final int partyId;
  final bool isSupplier;
  final String partyName;

  @override
  Widget build(BuildContext context) {
    final tag = 'gold_stmt_${isSupplier ? 's' : 'c'}_$partyId';
    final c = Get.put(
      GoldStatementController(
        partyId: partyId,
        isSupplier: isSupplier,
        partyName: partyName,
      ),
      tag: tag,
    );

    return Scaffold(
      appBar: AppBar(
        title: Text(
          isSupplier
              ? 'gold_supplier_statement'.tr()
              : 'gold_customer_statement'.tr(),
        ),
        actions: [
          IconButton(onPressed: c.load, icon: const Icon(Icons.refresh)),
        ],
      ),
      body: Obx(() {
        final stmt = c.data.value;
        if (c.isLoading.value && stmt == null) {
          return const Center(child: CircularProgressIndicator());
        }
        if (c.error.value != null && stmt == null) {
          return Center(child: Text(c.error.value.toString()));
        }
        if (stmt == null) {
          return Center(child: Text('no_data'.tr()));
        }
        return RefreshIndicator(
          onRefresh: c.load,
          child: ListView(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
            children: [
              Text(
                stmt.partyName.isEmpty ? partyName : stmt.partyName,
                style: Theme.of(context).textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
              ),
              const SizedBox(height: 8),
              Text(
                '${'gold_kpi_credit'.tr()}: ${formatCurrency(stmt.creditBalanceIqd)}'
                '${stmt.creditBalanceUsd > 0 ? ' / \$${formatCurrency(stmt.creditBalanceUsd)}' : ''}',
              ),
              Text(
                '${'gold_closing_balance'.tr()}: ${formatCurrency(stmt.closingBalance)}',
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () async {
                        final p = await showDatePicker(
                          context: context,
                          initialDate: c.from ?? DateTime.now(),
                          firstDate: DateTime(2020),
                          lastDate: DateTime.now(),
                        );
                        if (p != null) {
                          c.from = p;
                          c.load();
                        }
                      },
                      child: Text(
                        c.from == null ? 'gold_from'.tr() : formatDate(c.from!),
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () async {
                        final p = await showDatePicker(
                          context: context,
                          initialDate: c.to ?? DateTime.now(),
                          firstDate: DateTime(2020),
                          lastDate: DateTime.now(),
                        );
                        if (p != null) {
                          c.to = p;
                          c.load();
                        }
                      },
                      child: Text(
                        c.to == null ? 'gold_to'.tr() : formatDate(c.to!),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              if (stmt.rows.isEmpty)
                Padding(
                  padding: const EdgeInsets.all(24),
                  child: Center(child: Text('no_data'.tr())),
                )
              else
                ...stmt.rows.map(
                  (r) => Card(
                    margin: const EdgeInsets.only(bottom: 8),
                    child: ListTile(
                      title: Text('${r.documentType} · ${r.documentNumber}'),
                      subtitle: Text(
                        '${formatDate(r.date)}${r.notes.isEmpty ? '' : ' · ${r.notes}'}',
                      ),
                      trailing: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          if (r.debit > 0)
                            Text(
                              '+${formatCurrency(r.debit)}',
                              style: const TextStyle(color: Color(0xFFE65100)),
                            ),
                          if (r.credit > 0)
                            Text(
                              '-${formatCurrency(r.credit)}',
                              style: const TextStyle(color: Color(0xFF2E7D32)),
                            ),
                          Text(
                            formatCurrency(r.balance),
                            style: const TextStyle(fontWeight: FontWeight.w700),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
            ],
          ),
        );
      }),
    );
  }
}
