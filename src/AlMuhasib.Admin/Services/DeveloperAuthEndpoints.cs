using System.Net;
using AlMuhasib.Cloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Admin.Services;

public static class DeveloperAuthEndpoints
{
    public static IEndpointRouteBuilder MapDeveloperAuth(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (HttpContext ctx, CloudDbContext db, CloudDatabaseStatus database) =>
        {
            if (!database.IsAvailable)
                return Results.Redirect("/login?error=database");

            var form = await ctx.Request.ReadFormAsync();
            var username = form["username"].ToString();
            var password = form["password"].ToString();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Results.Redirect("/login?error=invalid");

            try
            {
                var user = await db.DeveloperUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

                if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                    return Results.Redirect("/login?error=invalid");

                ctx.Response.Cookies.Append(DeveloperAuthState.CookieName, user.Username, DeveloperAuthState.CreateCookieOptions());
                return Results.Redirect("/dashboard");
            }
            catch (Exception ex)
            {
                database.MarkUnavailable();
                var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("DeveloperAuth");
                logger.LogWarning(ex, "Developer login skipped because the cloud database is unavailable.");
                return Results.Redirect("/login?error=database");
            }
        }).DisableAntiforgery();

        app.MapPost("/auth/offline", (HttpContext ctx, CloudDatabaseStatus database) =>
        {
            if (database.IsAvailable || !IsLoopback(ctx))
                return Results.Redirect("/login");

            ctx.Response.Cookies.Append(
                DeveloperAuthState.CookieName,
                DeveloperAuthState.OfflineUsername,
                DeveloperAuthState.CreateCookieOptions());
            return Results.Redirect("/desktop-license");
        }).DisableAntiforgery();

        app.MapGet("/auth/logout", (HttpContext ctx) =>
        {
            ctx.Response.Cookies.Delete(DeveloperAuthState.CookieName, DeveloperAuthState.CreateCookieOptions());
            return Results.Redirect("/login");
        }).DisableAntiforgery();

        return app;
    }

    private static bool IsLoopback(HttpContext ctx)
    {
        var ip = ctx.Connection.RemoteIpAddress;
        return ip is null || IPAddress.IsLoopback(ip);
    }
}
