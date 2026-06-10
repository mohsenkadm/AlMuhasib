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
}
