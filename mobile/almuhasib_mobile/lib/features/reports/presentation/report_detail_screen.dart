import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../controllers/report_detail_controller.dart';
import '../../../shared/models/master_data_models.dart';
import '../../../shared/models/report_models.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/lookup_picker_sheet.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/shimmer_widgets.dart';

class ReportDetailScreen extends GetView<ReportDetailController> {
  ReportDetailScreen({super.key, required this.reportType});

  final String reportType;

  @override
  String? get tag => 'report_$reportType';

  @override
  Widget build(BuildContext context) {
    return Obx(() {
      return Scaffold(
        appBar: AppBar(title: Text(controller.title)),
        body: Column(
          children: [
            if (controller.showDateFilter)
              Padding(
                padding: const EdgeInsets.all(16),
                child: Row(
                  children: [
                    if (!controller.singleDate)
                      Expanded(
                        child: OutlinedButton.icon(
                          onPressed: () => controller.pickFromDate(context),
                          icon: const Icon(Icons.calendar_today, size: 16),
                          label: Text(
                            '${'from_date'.tr()}\n${formatDate(controller.from.value)}',
                          ),
                        ),
                      ),
                    if (!controller.singleDate) const SizedBox(width: 8),
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: () => controller.pickToDate(context),
                        icon: const Icon(Icons.calendar_today, size: 16),
                        label: Text(
                          '${controller.singleDate ? 'date'.tr() : 'to_date'.tr()}\n${formatDate(controller.to.value)}',
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            if (reportType == 'statement')
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: OutlinedButton.icon(
                  onPressed: () => controller.pickCustomer(context),
                  icon: const Icon(Icons.person_search),
                  label: Text(
                    controller.selectedCustomer.value?.name ??
                        'select_customer'.tr(),
                  ),
                ),
              ),
            if (reportType == 'investor_statement')
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: OutlinedButton.icon(
                  onPressed: () => controller.pickInvestor(context),
                  icon: const Icon(Icons.savings_outlined),
                  label: Text(
                    controller.selectedInvestor.value?.name ??
                        'select_investor'.tr(),
                  ),
                ),
              ),
            Expanded(
              child: RefreshIndicator(
                onRefresh: controller.reload,
                child: _ReportDetailBody(
                  controller: controller,
                  reportType: reportType,
                ),
              ),
            ),
          ],
        ),
      );
    });
  }
}

class _ReportDetailBody extends StatelessWidget {
  const _ReportDetailBody({
    required this.controller,
    required this.reportType,
  });

  final ReportDetailController controller;
  final String reportType;

  @override
  Widget build(BuildContext context) {
    if (controller.isLoading.value) return const ListShimmer();
    if (controller.error.value != null) {
      return ErrorStateWidget(
        message: controller.error.value.toString(),
        onRetry: controller.reload,
      );
    }
    if (controller.result.value == null) {
      return EmptyStateWidget(onRetry: controller.reload);
    }

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 24),
      children: [
        ...switch (reportType) {
          'sales' => _buildSales(controller.result.value as SalesReportResult),
          'purchases' =>
            _buildPurchases(controller.result.value as PurchasesReportResult),
          'profit' => _buildProfit(
              context,
              controller.result.value as ProfitReportResult,
              controller.profitInvoices,
            ),
          'balance_sheet' =>
            _buildBalanceSheet(controller.result.value as BalanceSheetResult),
          'overdue' => _buildOverdue(controller.result.value as OverdueResult),
          'statement' => _buildStatement(
              controller.result.value as CustomerStatementResult,
            ),
          'investor_statement' => _buildInvestorStatement(
              controller.result.value as InvestorStatementResult,
            ),
          'warehouse' =>
            _buildWarehouse(controller.result.value as List<WarehouseStockRow>),
          'top_products' => _buildTopProducts(
              controller.result.value as TopProductsReportResult,
            ),
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

  List<Widget> _buildProfit(
    BuildContext context,
    ProfitReportResult r,
    List<ProfitInvoiceDetailRow> profitInvoices,
  ) =>
      [
        _SummaryCard('report_sales'.tr(), formatCurrency(r.totalSales)),
        _SummaryCard('report_purchases'.tr(), formatCurrency(r.totalPurchases)),
        _SummaryCard('total_expenses'.tr(), formatCurrency(r.totalExpenses)),
        _SummaryCard('net_profit'.tr(), formatCurrency(r.netProfit)),
        _SummaryCard(
          'profit_margin'.tr(),
          '${r.profitMargin.toStringAsFixed(1)}%',
        ),
        if (profitInvoices.isNotEmpty) ...[
          Padding(
            padding: const EdgeInsets.only(top: 8, bottom: 8),
            child: Text(
              'profit_invoice_details'.tr(),
              style: Theme.of(context).textTheme.titleMedium,
            ),
          ),
          ...profitInvoices.asMap().entries.map(
                (e) => Card(
                  child: ListTile(
                    title: Text(e.value.invoiceNumber),
                    subtitle: Text(
                      '${e.value.customerName} • ${formatDate(e.value.date)}',
                    ),
                    trailing: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(formatCurrency(e.value.grossProfit)),
                        Text('${e.value.marginPercent.toStringAsFixed(0)}%'),
                      ],
                    ),
                  ),
                ).fadeSlideInList(index: e.key),
              ),
        ],
      ];

  List<Widget> _buildBalanceSheet(BalanceSheetResult r) => [
        _SummaryCard('equity_total'.tr(), formatCurrency(r.equityTotal)),
        _SummaryCard('liabilities_total'.tr(), formatCurrency(r.liabilitiesTotal)),
        _SummaryCard('assets_total'.tr(), formatCurrency(r.assetsTotal)),
        _SummaryCard('sales_profit'.tr(), formatCurrency(r.salesProfit)),
        _SummaryCard('cost_of_sales'.tr(), formatCurrency(r.costOfSales)),
        _SummaryCard('supplier_payables'.tr(), formatCurrency(r.supplierPayables)),
        _SummaryCard('investor_deposits'.tr(), formatCurrency(r.investorDeposits)),
        _SummaryCard('customer_debts'.tr(), formatCurrency(r.customerDebts)),
        _SummaryCard('inventory_value'.tr(), formatCurrency(r.inventoryValue)),
        if (r.isBalanced)
          Padding(
            padding: const EdgeInsets.only(top: 8),
            child: Text(
              'balance_sheet_balanced'.tr(),
              style: const TextStyle(color: Colors.green),
            ),
          ),
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
