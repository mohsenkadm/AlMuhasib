using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Middleware;

public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, CloudDbContext db)
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

                var canViewAll = context.User.HasClaim("can_view_all_branches", "1")
                                 || context.User.HasClaim("can_manage_all_branches", "1");
                var allBranches = context.User.HasClaim("all_branches", "1");

                List<int> allowed = [];
                if (accountId is > 0)
                {
                    // Load assignments with bypass so branch master isn't filtered by BranchId.
                    db.BypassBranchFilter = true;
                    try
                    {
                        allowed = await db.TenantAccountBranches.AsNoTracking()
                            .Where(x => x.TenantId == tenantId && x.TenantAccountId == accountId.Value)
                            .Select(x => x.BranchId)
                            .ToListAsync(context.RequestAborted);

                        // Backward compatible: no assignments yet → all active branches for tenant (Main after migration).
                        if (allowed.Count == 0)
                        {
                            allowed = await db.Branches.AsNoTracking()
                                .Where(b => b.TenantId == tenantId && b.IsActive && !b.IsDeleted)
                                .Select(b => b.Id)
                                .ToListAsync(context.RequestAborted);
                        }
                    }
                    finally
                    {
                        db.BypassBranchFilter = false;
                    }
                }

                // Header may request a branch switch for the request; still must be allowed.
                var headerBranch = context.Request.Headers["X-Branch-Id"].FirstOrDefault();
                int? requestedBranch = null;
                if (int.TryParse(headerBranch, out var hb) && hb > 0)
                    requestedBranch = hb;

                var claimBranch = context.User.FindFirst("branch_id")?.Value;
                if (requestedBranch is null && int.TryParse(claimBranch, out var cb) && cb > 0)
                    requestedBranch = cb;

                if (allBranches && canViewAll)
                {
                    tenantContext.SetAllBranchesMode(allowed);
                }
                else if (requestedBranch is > 0)
                {
                    if (allowed.Count > 0 && !allowed.Contains(requestedBranch.Value) && !canViewAll)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            code = "BRANCH_FORBIDDEN",
                            message = "لا تملك صلاحية الوصول إلى هذا الفرع."
                        });
                        return;
                    }

                    // Verify branch belongs to this tenant (anti cross-tenant BranchId).
                    db.BypassBranchFilter = true;
                    bool belongs;
                    try
                    {
                        belongs = await db.Branches.AsNoTracking()
                            .AnyAsync(b => b.Id == requestedBranch.Value
                                           && b.TenantId == tenantId
                                           && b.IsActive
                                           && !b.IsDeleted, context.RequestAborted);
                    }
                    finally
                    {
                        db.BypassBranchFilter = false;
                    }

                    if (!belongs)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            code = "BRANCH_FORBIDDEN",
                            message = "الفرع غير تابع لهذه الشركة أو غير نشط."
                        });
                        return;
                    }

                    tenantContext.SetBranch(requestedBranch.Value, allowed, canViewAll);
                }
                else if (allowed.Count == 1)
                {
                    // Auto-bind single branch so legacy clients without branch selection keep working.
                    tenantContext.SetBranch(allowed[0], allowed, canViewAll);
                }
                // else: no branch bound — write guards fail closed; reads see UnsetBranchId (empty).
            }
        }

        await _next(context);
    }
}
