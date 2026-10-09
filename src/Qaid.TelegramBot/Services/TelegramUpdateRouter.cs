using Qaid.TelegramBot.Application.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Qaid.TelegramBot.Services;

internal static class TelegramUpdateRouter
{
    public static async Task DispatchAsync(IBotUpdateProcessor processor, Update update, CancellationToken ct)
    {
        if (update.Type == UpdateType.Message && update.Message?.From is not null)
        {
            var msg = update.Message;
            await processor.ProcessMessageAsync(msg.From.Id, msg.Chat.Id, msg.Text ?? string.Empty, ct);
            return;
        }

        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery?.From is not null)
        {
            var cq = update.CallbackQuery;
            var chatId = cq.Message?.Chat.Id ?? cq.From.Id;
            var messageId = cq.Message?.MessageId ?? 0;
            await processor.ProcessCallbackAsync(
                cq.From.Id,
                chatId,
                messageId,
                cq.Id,
                cq.Data ?? string.Empty,
                ct);
        }
    }
}
