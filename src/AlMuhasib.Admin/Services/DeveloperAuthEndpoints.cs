using AlMuhasib.Cloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Admin.Services;

public static class DeveloperAuthEndpoints
{
    public static IEndpointRouteBuilder MapDeveloperAuth(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (HttpContext ctx, CloudDbContext db) =>
        {
            var form = await ctx.Request.ReadFormAsync();
            var username = form["username"].ToString();
            var password = form["password"].ToString();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Results.Redirect("/login?error=invalid");

            var user = await db.DeveloperUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return Results.Redirect("/login?error=invalid");

            ctx.Response.Cookies.Append(DeveloperAuthState.CookieName, user.Username, DeveloperAuthState.CreateCookieOptions());
            return Results.Redirect("/dashboard");
        }).DisableAntiforgery();

        app.MapGet("/auth/logout", (HttpContext ctx) =>
        {
            ctx.Response.Cookies.Delete(DeveloperAuthState.CookieName, DeveloperAuthState.CreateCookieOptions());
            return Results.Redirect("/login");
        }).DisableAntiforgery();

        return app;
    }
}
