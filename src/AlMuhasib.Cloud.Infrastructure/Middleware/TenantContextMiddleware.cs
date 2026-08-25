using AlMuhasib.Cloud.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AlMuhasib.Cloud.Infrastructure.Middleware;

public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // Bind tenant from JWT whenever the claim is present (tenant users).
        // Do not rely solely on role mapping — claim is the source of truth for isolation.
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
            if (int.TryParse(tenantClaim, out var tenantId) && tenantId > 0)
            {
                var accountClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                int? accountId = int.TryParse(accountClaim, out var aid) ? aid : null;
                tenantContext.SetTenant(tenantId, accountId);
            }
        }

        await _next(context);
    }
}
