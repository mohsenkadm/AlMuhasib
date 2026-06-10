using System.Text.Json;
using Microsoft.AspNetCore.Http;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Sync;
using AlMuhasib.Sync.Responses;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Middleware;

public sealed class TenantLicenseMiddleware
{
    private readonly RequestDelegate _next;

    public TenantLicenseMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CloudDbContext db, ILicenseValidator validator)
    {
        if (!context.User.IsInRole("Tenant"))
        {
            await _next(context);
            return;
        }

        var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
        var accountClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(tenantClaim, out var tenantId) || !int.TryParse(accountClaim, out var accountId))
        {
            await WriteErrorAsync(context, SyncErrorCodes.InvalidCredentials, "توكن غير صالح", 401);
            return;
        }

        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId);
        var account = await db.TenantAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId);

        if (tenant is null || account is null)
        {
            await WriteErrorAsync(context, SyncErrorCodes.InvalidCredentials, "الحساب غير موجود", 401);
            return;
        }

        var result = validator.Validate(tenant, account);
        if (!result.IsValid)
        {
            await WriteErrorAsync(context, result.ErrorCode!, result.Message!, 403);
            return;
        }

        await _next(context);
    }

    private static async Task WriteErrorAsync(HttpContext context, string code, string message, int status)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        var body = JsonSerializer.Serialize(new ApiErrorResponse { Code = code, Message = message });
        await context.Response.WriteAsync(body);
    }
}
