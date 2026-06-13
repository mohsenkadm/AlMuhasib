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

    private async Task<ActionResult<MobileWriteResponse>> UpsertAsync<T>(
        T request,
        Func<int, T, string, CancellationToken, Task<MobileWriteResponse>> action,
        CancellationToken ct)
    {
        var tenantId = ResolveTenant();
        var result = await action(tenantId, request, Username, ct);
        return result.Conflicts.Count > 0 ? Conflict(result) : Ok(result);
    }

    private int ResolveTenant()
    {
        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        _tenantContext.SetTenant(tenantId);
        return tenantId;
    }

    private string Username => User.Identity?.Name ?? "mobile";
}
