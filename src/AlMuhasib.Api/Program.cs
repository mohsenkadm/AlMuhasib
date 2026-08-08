using AlMuhasib.Cloud.Application;
using AlMuhasib.Cloud.Infrastructure;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Cloud.Infrastructure.Middleware;
using AlMuhasib.Cloud.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Car/RealEstate (and others) define same simple type names in different namespaces.
    options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
});
builder.Services.AddCloudApplicationServices();
builder.Services.AddCloudInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AdminOrigins").Get<string[]>() ?? ["https://localhost:7100"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

await CloudDbSeeder.SeedAsync(app.Services);

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
app.UseCors("Admin");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantContextMiddleware>();
app.UseMiddleware<TenantLicenseMiddleware>();

app.MapControllers();

app.Run();
