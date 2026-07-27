using AlMuhasib.Cloud.Application.Abstractions;
using AlMuhasib.Cloud.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMuhasib.Api.Controllers;

[Route("api/master-data")]
[Authorize(Policy = "Tenant")]
public sealed class MasterDataController : TenantApiControllerBase
{
    public MasterDataController(ITenantContext tenantContext, ICloudMasterDataService masterData)
        : base(tenantContext, masterData)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetAllAsync(ct));
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories([FromQuery] string? search, CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetCategoriesAsync(search, ct));
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search,
        [FromQuery] Guid? categorySyncId,
        [FromQuery] string? barcode,
        CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetProductsAsync(search, categorySyncId, barcode, ct));
    }

    [HttpGet("pricing-types")]
    public async Task<IActionResult> GetPricingTypes([FromQuery] string? search, CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetPricingTypesAsync(search, ct));
    }

    [HttpGet("product-prices")]
    public async Task<IActionResult> GetProductPrices(
        [FromQuery] Guid? productSyncId,
        [FromQuery] Guid? pricingTypeSyncId,
        CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetProductPricesAsync(productSyncId, pricingTypeSyncId, ct));
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers([FromQuery] string? search, CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetCustomersAsync(search, ct));
    }

    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers([FromQuery] string? search, CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetSuppliersAsync(search, ct));
    }

    [HttpGet("warehouses")]
    public async Task<IActionResult> GetWarehouses([FromQuery] string? search, CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetWarehousesAsync(search, ct));
    }

    [HttpGet("cash-boxes")]
    public async Task<IActionResult> GetCashBoxes([FromQuery] string? search, CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetCashBoxesAsync(search, ct));
    }

    [HttpGet("bank-accounts")]
    public async Task<IActionResult> GetBankAccounts([FromQuery] string? search, CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetBankAccountsAsync(search, ct));
    }

    [HttpGet("expense-types")]
    public async Task<IActionResult> GetExpenseTypes([FromQuery] string? search, CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetExpenseTypesAsync(search, ct));
    }

    [HttpGet("investors")]
    public async Task<IActionResult> GetInvestors([FromQuery] string? search, CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetInvestorsAsync(search, ct));
    }
}
