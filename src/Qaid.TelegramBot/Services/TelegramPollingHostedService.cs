using Microsoft.Extensions.Options;
using Qaid.TelegramBot.Application.Abstractions;
using Qaid.TelegramBot.Application.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Qaid.TelegramBot.Services;

/// <summary>
/// Long-polling fallback when <see cref="TelegramBotOptions.WebhookUrl"/> is empty
/// (typical for local development).
/// </summary>
public sealed class TelegramPollingHostedService : BackgroundService
{
    private readonly ITelegramBotClient _bot;
    private readonly IServiceScopeFactory _scopes;
    private readonly IOptions<TelegramBotOptions> _options;
    private readonly ILogger<TelegramPollingHostedService> _logger;

    public TelegramPollingHostedService(
        ITelegramBotClient bot,
        IServiceScopeFactory scopes,
        IOptions<TelegramBotOptions> options,
        ILogger<TelegramPollingHostedService> logger)
    {
        _bot = bot;
        _scopes = scopes;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.Value;
        if (string.IsNullOrWhiteSpace(opts.BotToken)
            || opts.BotToken.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(opts.WebhookUrl))
        {
            return;
        }

        try
        {
            await _bot.DeleteWebhook(dropPendingUpdates: false, cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear Telegram webhook before polling.");
        }

        _logger.LogInformation("Telegram long-polling started (WebhookUrl is empty).");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
            DropPendingUpdates = false
        };

        await _bot.ReceiveAsync(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IBotUpdateProcessor>();
            await TelegramUpdateRouter.DispatchAsync(processor, update, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed processing Telegram update via polling");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
    {
        _logger.LogWarning(exception, "Telegram polling error");
        return Task.CompletedTask;
    }
}
