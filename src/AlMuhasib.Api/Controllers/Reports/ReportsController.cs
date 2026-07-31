using AlMuhasib.Cloud.Application;
using AlMuhasib.Cloud.Application.Abstractions;
using AlMuhasib.Cloud.Application.Models;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AlMuhasib.Api.Controllers.Reports;

[Route("api/reports")]
[Authorize(Policy = "Tenant")]
public sealed class ReportsController : TenantApiControllerBase
{
    private readonly ICloudReportService _reports;
    private readonly CloudApplicationServiceOptions _options;

    public ReportsController(
        ITenantContext tenantContext,
        ICloudMasterDataService masterData,
        ICloudReportService reports,
        IOptions<CloudApplicationServiceOptions> options)
        : base(tenantContext, masterData)
    {
        _reports = reports;
        _options = options.Value;
    }

    // ── Sales & Purchases ──────────────────────────────────────

    [HttpGet("sales")]
    public async Task<IActionResult> Sales([FromQuery] ReportFilterRequest filter, [FromQuery] PaymentMethod? paymentMethod, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetSalesReportAsync(f.From, f.To, f.CustomerId, paymentMethod, f.WarehouseId));
    }

    [HttpGet("purchases")]
    public async Task<IActionResult> Purchases([FromQuery] ReportFilterRequest filter, [FromQuery] PaymentMethod? paymentMethod, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetPurchasesReportAsync(f.From, f.To, f.SupplierId, f.WarehouseId, paymentMethod));
    }

    // ── Profit ─────────────────────────────────────────────────

    [HttpGet("profit")]
    public async Task<IActionResult> Profit([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetProfitReportAsync(f.From, f.To));
    }

    [HttpGet("profit/monthly")]
    public async Task<IActionResult> MonthlyProfit([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetMonthlyProfitAsync(f.From, f.To));
    }

    [HttpGet("profit/comparison")]
    public async Task<IActionResult> ProfitComparison([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetProfitComparisonAsync(f.From, f.To));
    }

    [HttpGet("profit/invoices")]
    public async Task<IActionResult> ProfitInvoiceDetails([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetProfitInvoiceDetailsAsync(f.From, f.To));
    }

    // ── Installments ───────────────────────────────────────────

    [HttpGet("installments/summary")]
    public async Task<IActionResult> InstallmentsSummary([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetInstallmentsSummaryAsync(f.From, f.To, f.CustomerId, f.Status));
    }

    [HttpGet("installments/detail")]
    public async Task<IActionResult> InstallmentDetail([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        if (!f.CustomerId.HasValue)
            return BadRequest("customerSyncId is required.");
        return Ok(await _reports.GetInstallmentDetailAsync(f.CustomerId.Value));
    }

    [HttpGet("installments/paid")]
    public async Task<IActionResult> PaidInstallments([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetPaidInstallmentsAsync(f.From, f.To, f.CustomerId, f.CashBoxId));
    }

    [HttpGet("installments/unpaid")]
    public async Task<IActionResult> UnpaidInstallments([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetUnpaidInstallmentsAsync(f.From, f.To, f.CustomerId));
    }

    [HttpGet("installments/overdue")]
    public async Task<IActionResult> OverdueInstallments([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        var asOf = f.AsOfDate ?? DateTime.Today;
        return Ok(await _reports.GetOverdueReportAsync(asOf, f.MinDaysOverdue, f.CustomerId));
    }

    [HttpGet("installments/aging")]
    public async Task<IActionResult> InstallmentAging([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        var asOf = f.AsOfDate ?? DateTime.Today;
        return Ok(await _reports.GetInstallmentAgingReportAsync(asOf, f.CustomerId));
    }

    // ── Statements ─────────────────────────────────────────────

    [HttpGet("statements/customer")]
    public async Task<IActionResult> CustomerStatement([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        if (!f.CustomerId.HasValue)
            return BadRequest("customerSyncId is required.");
        return Ok(await _reports.GetCustomerStatementAsync(f.CustomerId.Value, f.From, f.To));
    }

    [HttpGet("statements/supplier")]
    public async Task<IActionResult> SupplierStatement([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        if (!f.SupplierId.HasValue)
            return BadRequest("supplierSyncId is required.");
        return Ok(await _reports.GetSupplierStatementAsync(f.SupplierId.Value, f.From, f.To));
    }

    [HttpGet("statements/investor")]
    public async Task<IActionResult> InvestorStatement([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        if (!f.InvestorId.HasValue)
            return BadRequest("investorSyncId is required.");
        return Ok(await _reports.GetInvestorStatementAsync(f.InvestorId.Value, f.From, f.To));
    }

    // ── Expenses & Income ──────────────────────────────────────

    [HttpGet("expenses")]
    public async Task<IActionResult> Expenses([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetExpensesReportAsync(f.From, f.To, f.ExpenseTypeId, f.CashBoxId));
    }

    [HttpGet("income-expense")]
    public async Task<IActionResult> IncomeExpense([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetIncomeExpenseReportAsync(f.From, f.To));
    }

    // ── Inventory ──────────────────────────────────────────────

    [HttpGet("warehouse")]
    public async Task<IActionResult> Warehouse([FromQuery] ReportFilterRequest filter, [FromQuery] bool includeZero = false, CancellationToken ct = default)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetWarehouseReportAsync(f.WarehouseId, includeZero));
    }

    [HttpGet("products/top")]
    public async Task<IActionResult> TopProducts([FromQuery] ReportFilterRequest filter, [FromQuery] bool sortByRevenueDescending = true, CancellationToken ct = default)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        var top = f.TopCount ?? _options.DefaultTopProductsCount;
        return Ok(await _reports.GetTopProductsReportAsync(f.From, f.To, f.WarehouseId, top, sortByRevenueDescending));
    }

    [HttpGet("products/margin")]
    public async Task<IActionResult> ProductMargin([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetProductProfitMarginReportAsync(f.From, f.To, f.WarehouseId));
    }

    [HttpGet("materials/net-profit")]
    public async Task<IActionResult> MaterialNetProfit(
        [FromQuery] ReportFilterRequest filter,
        [FromQuery] bool ascending = false,
        CancellationToken ct = default)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetMaterialNetProfitReportAsync(
            f.From, f.To, f.WarehouseId, ascending, f.TopCount));
    }

    [HttpGet("customers/net-profit")]
    public async Task<IActionResult> CustomerNetProfit(
        [FromQuery] ReportFilterRequest filter,
        [FromQuery] bool ascending = false,
        CancellationToken ct = default)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetCustomerNetProfitReportAsync(
            f.From, f.To, ascending, f.TopCount));
    }

    [HttpGet("products/movement")]
    public async Task<IActionResult> ProductMovement([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetProductMovementReportAsync(f.From, f.To, f.WarehouseId, f.ProductId));
    }

    [HttpGet("stock-health")]
    public async Task<IActionResult> StockHealth([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        var threshold = f.LowStockThreshold ?? _options.DefaultLowStockThreshold;
        var deadDays = f.DeadStockDays ?? _options.DefaultDeadStockDays;
        return Ok(await _reports.GetStockHealthReportAsync(f.WarehouseId, threshold, deadDays, f.StockHealthFilter));
    }

    [HttpGet("inventory-replenishment")]
    public async Task<IActionResult> InventoryReplenishment([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        var minStock = f.LowStockThreshold ?? _options.DefaultLowStockThreshold;
        return Ok(await _reports.GetInventoryReplenishmentReportAsync(
            f.From, f.To, f.WarehouseId, minStock, f.InventoryReplenishmentFilter));
    }

    [HttpGet("minimum-quantity")]
    public async Task<IActionResult> MinimumQuantity([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetMinimumQuantityReportAsync(
            f.WarehouseId, f.CategoryId, f.MinimumQuantityFilter, f.Search));
    }

    // ── Financial ──────────────────────────────────────────────

    [HttpGet("investors")]
    public async Task<IActionResult> Investors([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetInvestorsReportAsync(f.InvestorId, f.From, f.To));
    }

    [HttpGet("cash-flow")]
    public async Task<IActionResult> CashFlow([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetCashFlowReportAsync(f.CashBoxId, f.From, f.To));
    }

    [HttpGet("balance-sheet")]
    public async Task<IActionResult> BalanceSheet([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        var date = f.AsOfDate ?? f.To ?? DateTime.Today;
        return Ok(await _reports.GetBalanceSheetAsync(date));
    }

    // ── Overview ───────────────────────────────────────────────

    [HttpGet("customers/overview")]
    public async Task<IActionResult> CustomersOverview([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetCustomersOverviewReportAsync(f.From, f.To));
    }

    [HttpGet("suppliers/overview")]
    public async Task<IActionResult> SuppliersOverview([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        EnsureTenant();
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetSuppliersOverviewReportAsync(f.From, f.To));
    }

    // ── New extended reports ──────────────────────────────────────

    [HttpGet("investor-profit-distributions")]
    public async Task<IActionResult> InvestorProfitDistributions([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetInvestorProfitDistributionsReportAsync(f.From, f.To, f.InvestorId));
    }

    [HttpGet("capital-movement")]
    public async Task<IActionResult> CapitalMovement([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetCapitalMovementReportAsync(f.From, f.To));
    }

    [HttpGet("opening-installment-balances")]
    public async Task<IActionResult> OpeningInstallmentBalances([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetOpeningInstallmentBalancesReportAsync(f.From, f.To, f.CustomerId));
    }

    [HttpGet("company-fees")]
    public async Task<IActionResult> CompanyFees([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetCompanyFeeReportAsync(f.From, f.To, f.CustomerId));
    }

    [HttpGet("installment-schedule")]
    public async Task<IActionResult> InstallmentSchedule([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetInstallmentScheduleReportAsync(f.From, f.To, f.CustomerId, f.Status));
    }

    [HttpGet("sales-by-payment-method")]
    public async Task<IActionResult> SalesByPaymentMethod([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetSalesByPaymentMethodReportAsync(f.From, f.To, f.WarehouseId));
    }

    [HttpGet("daily-sales")]
    public async Task<IActionResult> DailySales([FromQuery] ReportFilterRequest filter, [FromQuery] PaymentMethod? paymentMethod, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetDailySalesReportAsync(f.From, f.To, f.WarehouseId, paymentMethod));
    }

    [HttpGet("sales-by-warehouse-user")]
    public async Task<IActionResult> SalesByWarehouseUser([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetSalesByWarehouseUserReportAsync(f.From, f.To, f.WarehouseId));
    }

    [HttpGet("gross-profit-margin")]
    public async Task<IActionResult> GrossProfitMargin([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetGrossProfitMarginReportAsync(f.From, f.To));
    }

    [HttpGet("operating-profit")]
    public async Task<IActionResult> OperatingProfit([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetOperatingProfitReportAsync(f.From, f.To));
    }

    [HttpGet("receivables-aging")]
    public async Task<IActionResult> ReceivablesAging([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        var asOf = f.To?.Date ?? DateTime.Today;
        return Ok(await _reports.GetReceivablesAgingReportAsync(asOf, f.CustomerId));
    }

    [HttpGet("payables-aging")]
    public async Task<IActionResult> PayablesAging([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        var asOf = f.To?.Date ?? DateTime.Today;
        return Ok(await _reports.GetPayablesAgingReportAsync(asOf, f.SupplierId));
    }

    [HttpGet("customer-collections")]
    public async Task<IActionResult> CustomerCollections([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetCustomerCollectionsReportAsync(f.From, f.To, f.CustomerId, f.CashBoxId));
    }

    [HttpGet("overdue-customers")]
    public async Task<IActionResult> OverdueCustomers([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        var asOf = f.To?.Date ?? DateTime.Today;
        return Ok(await _reports.GetOverdueCustomersReportAsync(asOf, f.MinDaysOverdue, f.CustomerId));
    }

    [HttpGet("supplier-payments")]
    public async Task<IActionResult> SupplierPayments([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetSupplierPaymentsReportAsync(f.From, f.To, f.SupplierId));
    }

    [HttpGet("bank-account-statement")]
    public async Task<IActionResult> BankAccountStatement([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetBankAccountStatementReportAsync(f.BankAccountId, f.From, f.To));
    }

    [HttpGet("cash-box-movement")]
    public async Task<IActionResult> CashBoxMovement([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetCashBoxMovementReportAsync(f.CashBoxId, f.From, f.To));
    }

    [HttpGet("cash-balances-summary")]
    public async Task<IActionResult> CashBalancesSummary(CancellationToken ct)
    {
        return Ok(await _reports.GetCashBalancesSummaryReportAsync());
    }

    [HttpGet("transfers")]
    public async Task<IActionResult> Transfers([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetTransfersReportAsync(f.From, f.To));
    }

    [HttpGet("inventory-valuation")]
    public async Task<IActionResult> InventoryValuation([FromQuery] ReportFilterRequest filter, [FromQuery] bool includeZero = false, CancellationToken ct = default)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetInventoryValuationReportAsync(f.WarehouseId, includeZero));
    }

    [HttpGet("stock-taking")]
    public async Task<IActionResult> StockTaking([FromQuery] ReportFilterRequest filter, [FromQuery] bool includeZero = true, CancellationToken ct = default)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetStockTakingReportAsync(f.WarehouseId, includeZero));
    }

    [HttpGet("cogs")]
    public async Task<IActionResult> Cogs([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetCogsReportAsync(f.From, f.To, f.WarehouseId));
    }

    [HttpGet("financial-position-summary")]
    public async Task<IActionResult> FinancialPositionSummary([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetFinancialPositionSummaryReportAsync(f.To ?? DateTime.Today));
    }

    [HttpGet("profit-and-loss")]
    public async Task<IActionResult> ProfitAndLoss([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        return Ok(await _reports.GetProfitAndLossReportAsync(f.From, f.To));
    }

    [HttpGet("statement-of-financial-position")]
    public async Task<IActionResult> StatementOfFinancialPosition([FromQuery] ReportFilterRequest filter, CancellationToken ct)
    {
        var f = await ResolveFilterAsync(filter, ct);
        var date = f.To?.Date ?? DateTime.Today;
        return Ok(await _reports.GetStatementOfFinancialPositionReportAsync(date));
    }

}
