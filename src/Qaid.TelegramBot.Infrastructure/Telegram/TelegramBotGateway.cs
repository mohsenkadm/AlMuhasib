using Microsoft.Extensions.Logging;
using Qaid.TelegramBot.Application.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Qaid.TelegramBot.Infrastructure.Telegram;

public sealed class TelegramBotGateway : ITelegramBotGateway
{
    private readonly ITelegramBotClient _client;
    private readonly ILogger<TelegramBotGateway> _logger;

    public TelegramBotGateway(ITelegramBotClient client, ILogger<TelegramBotGateway> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task SendTextAsync(long chatId, string text, IReadOnlyList<IReadOnlyList<BotButton>>? keyboard, CancellationToken ct)
    {
        try
        {
            await _client.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.None,
                replyMarkup: ToMarkup(keyboard),
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send Telegram message");
        }
    }

    public async Task AnswerCallbackAsync(string callbackQueryId, string? text, CancellationToken ct)
    {
        try
        {
            await _client.AnswerCallbackQuery(callbackQueryId, text, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "AnswerCallbackQuery failed");
        }
    }

    public async Task EditTextAsync(long chatId, int messageId, string text, IReadOnlyList<IReadOnlyList<BotButton>>? keyboard, CancellationToken ct)
    {
        try
        {
            await _client.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: text,
                parseMode: ParseMode.None,
                replyMarkup: ToMarkup(keyboard),
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EditMessageText failed; falling back to send");
            await SendTextAsync(chatId, text, keyboard, ct);
        }
    }

    private static InlineKeyboardMarkup? ToMarkup(IReadOnlyList<IReadOnlyList<BotButton>>? keyboard)
    {
        if (keyboard is null || keyboard.Count == 0)
            return null;

        var rows = keyboard
            .Select(row => row.Select(b => InlineKeyboardButton.WithCallbackData(b.Text, b.CallbackData)).ToArray())
            .ToArray();
        return new InlineKeyboardMarkup(rows);
    }
}
