using AlMuhasib.Admin.Components;
using AlMuhasib.Admin.Services;
using AlMuhasib.Cloud.Infrastructure;
using AlMuhasib.Cloud.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Local signing key for desktop lifetime activation (gitignored — never commit).
builder.Configuration.AddJsonFile("appsettings.DesktopLicense.json", optional: true, reloadOnChange: true);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddCloudInfrastructure(builder.Configuration);
builder.Services.AddSingleton<CloudDatabaseStatus>();
builder.Services.AddScoped<DeveloperAuthState>();

var app = builder.Build();

var databaseStatus = app.Services.GetRequiredService<CloudDatabaseStatus>();
var connectionString = app.Configuration.GetConnectionString("CloudConnection");
if (await CloudDatabaseStatus.CanReachServerAsync(connectionString, app.Logger))
{
    try
    {
        await CloudDbSeeder.SeedAsync(app.Services);
        databaseStatus.MarkAvailable();
    }
    catch (Exception ex)
    {
        databaseStatus.MarkUnavailable();
        app.Logger.LogWarning(ex,
            "Cloud database is unavailable. Admin will keep running so desktop activation can be used without it.");
    }
}
else
{
    databaseStatus.MarkUnavailable();
}

//if (!app.Environment.IsDevelopment())
//{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
//}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapDeveloperAuth();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
