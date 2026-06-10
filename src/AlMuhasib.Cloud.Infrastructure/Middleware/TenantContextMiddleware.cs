using AlMuhasib.Cloud.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AlMuhasib.Cloud.Infrastructure.Middleware;

public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (context.User.IsInRole("Tenant"))
        {
            var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
            var accountClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(tenantClaim, out var tenantId))
            {
                int? accountId = int.TryParse(accountClaim, out var aid) ? aid : null;
                tenantContext.SetTenant(tenantId, accountId);
            }
        }

        await _next(context);
    }
}
