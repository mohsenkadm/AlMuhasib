using AlMuhasib.Cloud.Application.Abstractions;
using AlMuhasib.Cloud.Application.Models;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlMuhasib.Api.Controllers;

[ApiController]
public abstract class TenantApiControllerBase : ControllerBase
{
    protected readonly ITenantContext TenantContext;
    protected readonly ICloudMasterDataService MasterData;

    protected TenantApiControllerBase(ITenantContext tenantContext, ICloudMasterDataService masterData)
    {
        TenantContext = tenantContext;
        MasterData = masterData;
    }

    protected void EnsureTenant()
    {
        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        TenantContext.SetTenant(tenantId);
    }

    protected async Task<ResolvedReportFilter> ResolveFilterAsync(ReportFilterRequest? filter, CancellationToken ct)
    {
        filter ??= new ReportFilterRequest();
        return new ResolvedReportFilter
        {
            From = filter.From,
            To = filter.To,
            Status = filter.Status,
            TopCount = filter.TopCount,
            LowStockThreshold = filter.LowStockThreshold,
            DeadStockDays = filter.DeadStockDays,
            AsOfDate = filter.AsOfDate,
            MinDaysOverdue = filter.MinDaysOverdue,
            CustomerId = await ResolveIdAsync("customer", filter.CustomerSyncId, ct),
            SupplierId = await ResolveIdAsync("supplier", filter.SupplierSyncId, ct),
            WarehouseId = await ResolveIdAsync("warehouse", filter.WarehouseSyncId, ct),
            ProductId = await ResolveIdAsync("product", filter.ProductSyncId, ct),
            CashBoxId = await ResolveIdAsync("cashbox", filter.CashBoxSyncId, ct),
            ExpenseTypeId = await ResolveIdAsync("expensetype", filter.ExpenseTypeSyncId, ct),
            InvestorId = await ResolveIdAsync("investor", filter.InvestorSyncId, ct),
            StockHealthFilter = ParseStockHealthFilter(filter.StockHealthFilter)
        };
    }

    private async Task<int?> ResolveIdAsync(string entityType, Guid? syncId, CancellationToken ct) =>
        syncId.HasValue
            ? await MasterData.ResolveIdBySyncIdAsync(entityType, syncId.Value, ct)
            : null;

    private static StockHealthFilter ParseStockHealthFilter(string? value) => value?.ToLowerInvariant() switch
    {
        "lowstock" or "lowstockonly" => StockHealthFilter.LowStockOnly,
        "deadstock" or "deadstockonly" => StockHealthFilter.DeadStockOnly,
        _ => StockHealthFilter.All
    };
}

public sealed class ResolvedReportFilter
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int? CustomerId { get; init; }
    public int? SupplierId { get; init; }
    public int? WarehouseId { get; init; }
    public int? ProductId { get; init; }
    public int? CashBoxId { get; init; }
    public int? ExpenseTypeId { get; init; }
    public int? InvestorId { get; init; }
    public string? Status { get; init; }
    public int? TopCount { get; init; }
    public decimal? LowStockThreshold { get; init; }
    public int? DeadStockDays { get; init; }
    public DateTime? AsOfDate { get; init; }
    public int? MinDaysOverdue { get; init; }
    public StockHealthFilter StockHealthFilter { get; init; } = StockHealthFilter.All;
}
