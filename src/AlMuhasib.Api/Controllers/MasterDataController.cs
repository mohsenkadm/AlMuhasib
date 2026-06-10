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
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetCategoriesAsync(ct));
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetProductsAsync(ct));
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers(CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetCustomersAsync(ct));
    }

    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers(CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetSuppliersAsync(ct));
    }

    [HttpGet("warehouses")]
    public async Task<IActionResult> GetWarehouses(CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetWarehousesAsync(ct));
    }

    [HttpGet("cash-boxes")]
    public async Task<IActionResult> GetCashBoxes(CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetCashBoxesAsync(ct));
    }

    [HttpGet("bank-accounts")]
    public async Task<IActionResult> GetBankAccounts(CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetBankAccountsAsync(ct));
    }

    [HttpGet("expense-types")]
    public async Task<IActionResult> GetExpenseTypes(CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetExpenseTypesAsync(ct));
    }

    [HttpGet("investors")]
    public async Task<IActionResult> GetInvestors(CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await MasterData.GetInvestorsAsync(ct));
    }
}
