using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Qaid.TelegramBot.Application.Abstractions;
using Qaid.TelegramBot.Application.Options;
using Qaid.TelegramBot.Services;
using Telegram.Bot.Types;

namespace Qaid.TelegramBot.Endpoints;

public static class TelegramWebhookEndpoint
{
    public const string SecretHeader = "X-Telegram-Bot-Api-Secret-Token";

    public static IEndpointRouteBuilder MapTelegramWebhook(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/telegram/webhook", HandleAsync)
            .RequireRateLimiting("webhook")
            .DisableAntiforgery();
        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        HttpRequest request,
        [FromServices] IBotUpdateProcessor processor,
        [FromServices] IOptions<TelegramBotOptions> options,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("TelegramWebhook");
        var secret = options.Value.WebhookSecret;
        if (!string.IsNullOrWhiteSpace(secret))
        {
            if (!request.Headers.TryGetValue(SecretHeader, out var provided)
                || !FixedTimeEquals(provided.ToString(), secret))
            {
                logger.LogWarning("Webhook rejected: invalid secret token.");
                return Results.Unauthorized();
            }
        }

        Update? update;
        try
        {
            update = await request.ReadFromJsonAsync<Update>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Invalid webhook payload");
            return Results.BadRequest();
        }

        if (update is null)
            return Results.Ok();

        try
        {
            await TelegramUpdateRouter.DispatchAsync(processor, update, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed processing Telegram update");
        }

        return Results.Ok();
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = System.Text.Encoding.UTF8.GetBytes(a);
        var bb = System.Text.Encoding.UTF8.GetBytes(b);
        if (ba.Length != bb.Length)
            return false;
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
