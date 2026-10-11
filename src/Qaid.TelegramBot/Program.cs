using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Qaid.TelegramBot.Application.Options;
using Qaid.TelegramBot.Infrastructure;
using Qaid.TelegramBot.Infrastructure.Data;
using Qaid.TelegramBot.Application.Bot;
using Qaid.TelegramBot.Endpoints;
using Qaid.TelegramBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddQaidTelegramBot(builder.Configuration);
builder.Services.AddHostedService<TelegramPollingHostedService>();
builder.Services.AddRazorPages();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("webhook", opt =>
    {
        opt.PermitLimit = 120;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("link-page", opt =>
    {
        opt.PermitLimit = 30;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

var app = builder.Build();

var qaidApiBase = app.Configuration["QaidApi:BaseUrl"];
if (string.IsNullOrWhiteSpace(qaidApiBase))
{
    app.Logger.LogCritical("QaidApi:BaseUrl is not configured. Link login will fail.");
}
else
{
    app.Logger.LogInformation("Qaid API BaseUrl: {BaseUrl}", qaidApiBase.TrimEnd('/'));
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
    await db.Database.EnsureCreatedAsync();
    // Soft-upgrade existing SQLite files created before ApplicationSystemType.
    foreach (var sql in new[]
             {
                 "ALTER TABLE LinkCodes ADD COLUMN ApplicationSystemType INTEGER NOT NULL DEFAULT 0",
                 "ALTER TABLE TelegramLinks ADD COLUMN ApplicationSystemType INTEGER NOT NULL DEFAULT 0"
             })
    {
        try { await db.Database.ExecuteSqlRawAsync(sql); }
        catch { /* column already exists */ }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(err =>
    {
        err.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("حدث خطأ غير متوقع.");
        });
    });
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseRouting();
app.MapRazorPages();
app.MapTelegramWebhook();

app.MapGet("/", () => Results.Redirect("/link"));

// Register slash-command suggestions + optional webhook.
var telegramOpts = app.Configuration.GetSection(TelegramBotOptions.SectionName).Get<TelegramBotOptions>();
if (!string.IsNullOrWhiteSpace(telegramOpts?.BotToken)
    && !telegramOpts.BotToken.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
{
    var bot = app.Services.GetRequiredService<ITelegramBotClient>();
    try
    {
        await bot.SetMyCommands(
            BotCommands.All.Select(c => new BotCommand { Command = c.Command, Description = c.Description }));
        app.Logger.LogInformation("Telegram bot commands registered.");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Failed to register Telegram bot commands.");
    }

    if (!string.IsNullOrWhiteSpace(telegramOpts.WebhookUrl))
    {
        try
        {
            await bot.SetWebhook(
                url: telegramOpts.WebhookUrl,
                secretToken: string.IsNullOrWhiteSpace(telegramOpts.WebhookSecret) ? null : telegramOpts.WebhookSecret,
                allowedUpdates: [UpdateType.Message, UpdateType.CallbackQuery]);
            app.Logger.LogInformation("Telegram webhook configured.");
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Failed to set Telegram webhook at startup.");
        }
    }
}

app.Run();

public partial class Program;
