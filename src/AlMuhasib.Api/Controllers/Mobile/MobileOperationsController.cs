using AlMuhasib.Cloud.Application.Abstractions;
using AlMuhasib.Cloud.Application.Models.Mobile;
using AlMuhasib.Cloud.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMuhasib.Api.Controllers.Mobile;

[ApiController]
[Route("api/mobile")]
[Authorize(Policy = "Tenant")]
public sealed class MobileOperationsController : ControllerBase
{
    private readonly ICloudMobileWriteService _mobileWrite;
    private readonly ITenantContext _tenantContext;

    public MobileOperationsController(ICloudMobileWriteService mobileWrite, ITenantContext tenantContext)
    {
        _mobileWrite = mobileWrite;
        _tenantContext = tenantContext;
    }

    [HttpPost("customers")]
    public async Task<ActionResult<MobileWriteResponse>> CreateCustomer(
        [FromBody] CreateCustomerRequest request, CancellationToken ct)
        => await UpsertAsync(request, _mobileWrite.UpsertCustomerAsync, ct);

    [HttpPost("suppliers")]
    public async Task<ActionResult<MobileWriteResponse>> CreateSupplier(
        [FromBody] CreateSupplierRequest request, CancellationToken ct)
        => await UpsertAsync(request, _mobileWrite.UpsertSupplierAsync, ct);

    [HttpPost("products")]
    public async Task<ActionResult<MobileWriteResponse>> CreateProduct(
        [FromBody] CreateProductRequest request, CancellationToken ct)
        => await UpsertAsync(request, _mobileWrite.UpsertProductAsync, ct);

    [HttpPost("investors")]
    public async Task<ActionResult<MobileWriteResponse>> CreateInvestor(
        [FromBody] CreateInvestorRequest request, CancellationToken ct)
        => await UpsertAsync(request, _mobileWrite.UpsertInvestorAsync, ct);

    [HttpPost("pricing-types")]
    public async Task<ActionResult<MobileWriteResponse>> CreatePricingType(
        [FromBody] UpsertPricingTypeRequest request, CancellationToken ct)
        => await UpsertAsync(request, _mobileWrite.UpsertPricingTypeAsync, ct);

    [HttpPut("pricing-types/{syncId:guid}")]
    public async Task<ActionResult<MobileWriteResponse>> UpdatePricingType(
        Guid syncId, [FromBody] UpsertPricingTypeRequest request, CancellationToken ct)
    {
        request.SyncId = syncId;
        return await UpsertAsync(request, _mobileWrite.UpsertPricingTypeAsync, ct);
    }

    [HttpDelete("pricing-types/{syncId:guid}")]
    public async Task<ActionResult<MobileWriteResponse>> DeletePricingType(Guid syncId, CancellationToken ct)
    {
        var tenantId = ResolveTenant();
        try
        {
            var result = await _mobileWrite.DeletePricingTypeAsync(tenantId, syncId, Username, ct);
            return result.Conflicts.Count > 0 ? Conflict(result) : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("product-prices")]
    public async Task<ActionResult<MobileWriteResponse>> CreateProductPrice(
        [FromBody] UpsertProductPriceRequest request, CancellationToken ct)
        => await UpsertAsync(request, _mobileWrite.UpsertProductPriceAsync, ct);

    [HttpPut("product-prices/{syncId:guid}")]
    public async Task<ActionResult<MobileWriteResponse>> UpdateProductPrice(
        Guid syncId, [FromBody] UpsertProductPriceRequest request, CancellationToken ct)
    {
        request.SyncId = syncId;
        return await UpsertAsync(request, _mobileWrite.UpsertProductPriceAsync, ct);
    }

    [HttpDelete("product-prices/{syncId:guid}")]
    public async Task<ActionResult<MobileWriteResponse>> DeleteProductPrice(Guid syncId, CancellationToken ct)
    {
        var tenantId = ResolveTenant();
        var result = await _mobileWrite.DeleteProductPriceAsync(tenantId, syncId, Username, ct);
        return result.Conflicts.Count > 0 ? Conflict(result) : Ok(result);
    }

    [HttpPost("invoices")]
    public async Task<ActionResult<MobileWriteResponse>> CreateInvoice(
        [FromBody] CreateInvoiceRequest request, CancellationToken ct)
    {
        var tenantId = ResolveTenant();
        try
        {
            var result = await _mobileWrite.CreateInvoiceAsync(tenantId, request, Username, ct);
            return result.Conflicts.Count > 0 ? Conflict(result) : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("cash-boxes")]
    public async Task<ActionResult<MobileWriteResponse>> CreateCashBox(
        [FromBody] UpsertCashBoxRequest request, CancellationToken ct)
        => await UpsertAsync(request, _mobileWrite.UpsertCashBoxAsync, ct);

    [HttpPost("bank-accounts")]
    public async Task<ActionResult<MobileWriteResponse>> CreateBankAccount(
        [FromBody] UpsertBankAccountRequest request, CancellationToken ct)
        => await UpsertAsync(request, _mobileWrite.UpsertBankAccountAsync, ct);

    [HttpPost("expense-types")]
    public async Task<ActionResult<MobileWriteResponse>> CreateExpenseType(
        [FromBody] UpsertExpenseTypeRequest request, CancellationToken ct)
        => await UpsertAsync(request, _mobileWrite.UpsertExpenseTypeAsync, ct);

    [HttpPost("vouchers")]
    public async Task<ActionResult<MobileWriteResponse>> CreateVoucher(
        [FromBody] CreateVoucherRequest request, CancellationToken ct)
        => await WriteAsync(() => _mobileWrite.CreateVoucherAsync(ResolveTenant(), request, Username, ct));

    [HttpPost("expenses")]
    public async Task<ActionResult<MobileWriteResponse>> CreateExpense(
        [FromBody] CreateExpenseRequest request, CancellationToken ct)
        => await WriteAsync(() => _mobileWrite.CreateExpenseAsync(ResolveTenant(), request, Username, ct));

    [HttpPost("transfers")]
    public async Task<ActionResult<MobileWriteResponse>> CreateTransfer(
        [FromBody] CreateTransferRequest request, CancellationToken ct)
        => await WriteAsync(() => _mobileWrite.CreateTransferAsync(ResolveTenant(), request, Username, ct));

    [HttpPost("warehouses")]
    public async Task<ActionResult<MobileWriteResponse>> CreateWarehouse(
        [FromBody] UpsertWarehouseRequest request, CancellationToken ct)
        => await UpsertAsync(request, _mobileWrite.UpsertWarehouseAsync, ct);

    [HttpPost("warehouse-transfers")]
    public async Task<ActionResult<MobileWriteResponse>> CreateWarehouseTransfer(
        [FromBody] CreateWarehouseTransferRequest request, CancellationToken ct)
        => await WriteAsync(() => _mobileWrite.CreateWarehouseTransferAsync(ResolveTenant(), request, Username, ct));

    [HttpPost("stock-adjustments")]
    public async Task<ActionResult<MobileWriteResponse>> AdjustStock(
        [FromBody] CreateStockAdjustmentRequest request, CancellationToken ct)
        => await WriteAsync(() => _mobileWrite.AdjustStockAsync(ResolveTenant(), request, Username, ct));

    [HttpPost("installments/{syncId:guid}/pay")]
    public async Task<ActionResult<MobileWriteResponse>> PayInstallment(
        Guid syncId, [FromBody] PayInstallmentRequest request, CancellationToken ct)
        => await WriteAsync(() => _mobileWrite.PayInstallmentAsync(ResolveTenant(), syncId, request, Username, ct));

    private async Task<ActionResult<MobileWriteResponse>> UpsertAsync<T>(
        T request,
        Func<int, T, string, CancellationToken, Task<MobileWriteResponse>> action,
        CancellationToken ct)
    {
        var tenantId = ResolveTenant();
        try
        {
            var result = await action(tenantId, request, Username, ct);
            return result.Conflicts.Count > 0 ? Conflict(result) : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<ActionResult<MobileWriteResponse>> WriteAsync(Func<Task<MobileWriteResponse>> action)
    {
        try
        {
            var result = await action();
            return result.Conflicts.Count > 0 ? Conflict(result) : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private int ResolveTenant()
    {
        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        _tenantContext.SetTenant(tenantId);
        return tenantId;
    }

    private string Username => User.Identity?.Name ?? "mobile";
}
