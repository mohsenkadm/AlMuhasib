using AlMuhasib.Admin.Components;
using AlMuhasib.Admin.Services;
using AlMuhasib.Cloud.Infrastructure;
using AlMuhasib.Cloud.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddCloudInfrastructure(builder.Configuration);
builder.Services.AddScoped<DeveloperAuthState>();

var app = builder.Build();

await CloudDbSeeder.SeedAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapDeveloperAuth();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
