using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Qaid.TelegramBot.Application.Options;
using Qaid.TelegramBot.Infrastructure;
using Qaid.TelegramBot.Infrastructure.Data;
using Qaid.TelegramBot.Endpoints;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddQaidTelegramBot(builder.Configuration);
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
    await db.Database.EnsureCreatedAsync();
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

// Optional webhook registration when URL + token are configured.
var telegramOpts = app.Configuration.GetSection(TelegramBotOptions.SectionName).Get<TelegramBotOptions>();
if (!string.IsNullOrWhiteSpace(telegramOpts?.BotToken)
    && !string.IsNullOrWhiteSpace(telegramOpts.WebhookUrl)
    && !telegramOpts.BotToken.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        var bot = app.Services.GetRequiredService<ITelegramBotClient>();
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

app.Run();

public partial class Program;
