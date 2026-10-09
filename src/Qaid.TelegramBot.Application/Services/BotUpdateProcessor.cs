using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qaid.TelegramBot.Application.Abstractions;
using Qaid.TelegramBot.Application.Bot;
using Qaid.TelegramBot.Application.Models;
using Qaid.TelegramBot.Application.Options;
using Qaid.TelegramBot.Application.Reports;

namespace Qaid.TelegramBot.Application.Services;

public sealed class BotUpdateProcessor : IBotUpdateProcessor
{
    private readonly ILinkService _links;
    private readonly IQaidApiClient _api;
    private readonly ISessionTokenService _sessions;
    private readonly ITelegramBotGateway _bot;
    private readonly IRateLimiter _rateLimiter;
    private readonly TelegramBotOptions _options;
    private readonly ILogger<BotUpdateProcessor> _logger;

    public BotUpdateProcessor(
        ILinkService links,
        IQaidApiClient api,
        ISessionTokenService sessions,
        ITelegramBotGateway bot,
        IRateLimiter rateLimiter,
        IOptions<TelegramBotOptions> options,
        ILogger<BotUpdateProcessor> logger)
    {
        _links = links;
        _api = api;
        _sessions = sessions;
        _bot = bot;
        _rateLimiter = rateLimiter;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessMessageAsync(long telegramUserId, long chatId, string text, CancellationToken ct)
    {
        if (!_rateLimiter.TryAcquire(telegramUserId))
        {
            await _bot.SendTextAsync(chatId, "تجاوزت معدل الطلبات. حاول بعد لحظات.", null, ct);
            return;
        }

        var trimmed = (text ?? string.Empty).Trim();
        if (trimmed.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            await HandleStartAsync(telegramUserId, chatId, ct);
            return;
        }

        if (trimmed.StartsWith("/unlink", StringComparison.OrdinalIgnoreCase))
        {
            await PromptUnlinkAsync(chatId, ct);
            return;
        }

        if (trimmed.StartsWith("/link", StringComparison.OrdinalIgnoreCase))
        {
            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2)
            {
                await TryConsumeCodeAsync(telegramUserId, chatId, parts[1], ct);
                return;
            }

            await SendNotLinkedOrMenuAsync(telegramUserId, chatId, ct);
            return;
        }

        // Treat bare link codes (8+ alphanumeric) as link attempts when not already linked.
        if (LooksLikeLinkCode(trimmed))
        {
            var link = await _links.GetActiveLinkAsync(telegramUserId, ct);
            if (link is null || !link.IsActive)
            {
                await TryConsumeCodeAsync(telegramUserId, chatId, trimmed, ct);
                return;
            }
        }

        await SendNotLinkedOrMenuAsync(telegramUserId, chatId, ct);
    }

    public async Task ProcessCallbackAsync(
        long telegramUserId, long chatId, int messageId, string callbackQueryId, string data, CancellationToken ct)
    {
        try
        {
            if (!_rateLimiter.TryAcquire(telegramUserId))
            {
                await _bot.AnswerCallbackAsync(callbackQueryId, "بطء الطلبات", ct);
                return;
            }

            if (!CallbackData.TryParse(data, out var action))
            {
                await _bot.AnswerCallbackAsync(callbackQueryId, "زر غير صالح", ct);
                return;
            }

            await _bot.AnswerCallbackAsync(callbackQueryId, null, ct);

            switch (action.Type)
            {
                case CallbackActionType.MainMenu:
                    await ShowMainMenuAsync(telegramUserId, chatId, messageId, ct);
                    break;
                case CallbackActionType.SelectReport:
                case CallbackActionType.PeriodMenu:
                    await HandleSelectReportAsync(telegramUserId, chatId, messageId, action.Kind!.Value, ct);
                    break;
                case CallbackActionType.RunReport:
                    await RunReportAsync(telegramUserId, chatId, messageId, action.Kind!.Value, action.Period!.Value, ct);
                    break;
                case CallbackActionType.UnlinkPrompt:
                    await _bot.EditTextAsync(chatId, messageId, "هل تريد إلغاء ربط حساب قيد؟", BotMenus.UnlinkConfirm(), ct);
                    break;
                case CallbackActionType.UnlinkConfirm:
                    await _links.UnlinkAsync(telegramUserId, ct);
                    await _bot.EditTextAsync(chatId, messageId, "تم إلغاء الربط. لن تُعرض أي بيانات حتى تعيد الربط.", null, ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Callback handling failed");
            await _bot.SendTextAsync(chatId, ArabicReportFormatter.GenericError(), null, ct);
        }
    }

    private async Task HandleStartAsync(long telegramUserId, long chatId, CancellationToken ct)
    {
        var link = await _links.GetActiveLinkAsync(telegramUserId, ct);
        if (link is null || !link.IsActive)
        {
            await _bot.SendTextAsync(chatId, ArabicReportFormatter.NotLinked(LinkUrl()), null, ct);
            return;
        }

        await _bot.SendTextAsync(
            chatId,
            $"مرحباً بك في بوت تقارير قيد.\nالشركة: {link.CompanyName}\nاختر تقريراً:",
            BotMenus.MainMenu(link.CompanyName),
            ct);
    }

    private async Task SendNotLinkedOrMenuAsync(long telegramUserId, long chatId, CancellationToken ct)
    {
        var link = await _links.GetActiveLinkAsync(telegramUserId, ct);
        if (link is null || !link.IsActive)
        {
            await _bot.SendTextAsync(chatId, ArabicReportFormatter.NotLinked(LinkUrl()), null, ct);
            return;
        }

        await _bot.SendTextAsync(chatId, $"الشركة: {link.CompanyName}\nاختر تقريراً:", BotMenus.MainMenu(link.CompanyName), ct);
    }

    private async Task TryConsumeCodeAsync(long telegramUserId, long chatId, string code, CancellationToken ct)
    {
        var (ok, message, link) = await _links.ConsumeLinkCodeAsync(code, telegramUserId, ct);
        if (!ok || link is null)
        {
            await _bot.SendTextAsync(chatId, message, null, ct);
            return;
        }

        await _bot.SendTextAsync(
            chatId,
            $"تم الربط بنجاح.\nالشركة: {link.CompanyName}\nاختر تقريراً:",
            BotMenus.MainMenu(link.CompanyName),
            ct);
    }

    private async Task PromptUnlinkAsync(long chatId, CancellationToken ct)
        => await _bot.SendTextAsync(chatId, "هل تريد إلغاء ربط حساب قيد؟", BotMenus.UnlinkConfirm(), ct);

    private async Task ShowMainMenuAsync(long telegramUserId, long chatId, int messageId, CancellationToken ct)
    {
        var link = await _links.GetActiveLinkAsync(telegramUserId, ct);
        if (link is null || !link.IsActive)
        {
            await _bot.EditTextAsync(chatId, messageId, ArabicReportFormatter.NotLinked(LinkUrl()), null, ct);
            return;
        }

        await _bot.EditTextAsync(chatId, messageId, $"الشركة: {link.CompanyName}\nاختر تقريراً:", BotMenus.MainMenu(link.CompanyName), ct);
    }

    private async Task HandleSelectReportAsync(long telegramUserId, long chatId, int messageId, ReportKind kind, CancellationToken ct)
    {
        var link = await RequireLinkAsync(telegramUserId, chatId, messageId, ct);
        if (link is null)
            return;

        if (ReportCatalog.NeedsPeriod(kind))
        {
            await _bot.EditTextAsync(
                chatId,
                messageId,
                $"تقرير: {ReportCatalog.TitleOf(kind)}\nاختر الفترة الزمنية:",
                BotMenus.PeriodMenu(kind),
                ct);
            return;
        }

        await RunReportAsync(telegramUserId, chatId, messageId, kind, ReportPeriod.Today, ct);
    }

    private async Task RunReportAsync(
        long telegramUserId, long chatId, int messageId, ReportKind kind, ReportPeriod period, CancellationToken ct)
    {
        var link = await RequireLinkAsync(telegramUserId, chatId, messageId, ct);
        if (link is null)
            return;

        var (ok, accessToken, error) = await _sessions.GetValidAccessTokenAsync(link, ct);
        if (!ok || accessToken is null)
        {
            await _bot.EditTextAsync(chatId, messageId, ArabicReportFormatter.SessionExpired(), null, ct);
            return;
        }

        var range = ReportPeriodHelper.Resolve(period);
        try
        {
            var text = kind switch
            {
                ReportKind.DailySales => ArabicReportFormatter.FormatDailySales(
                    await _api.GetDailySalesAsync(accessToken, range.From, range.To, ct), range.Label),
                ReportKind.Expenses => ArabicReportFormatter.FormatExpenses(
                    await _api.GetExpensesAsync(accessToken, range.From, range.To, ct), range.Label),
                ReportKind.ProfitAndLoss => ArabicReportFormatter.FormatProfitAndLoss(
                    await _api.GetProfitAndLossAsync(accessToken, range.From, range.To, ct), range.Label),
                ReportKind.CashBalances => ArabicReportFormatter.FormatCashBalances(
                    await _api.GetCashBalancesAsync(accessToken, ct)),
                ReportKind.CashFlow => ArabicReportFormatter.FormatCashFlow(
                    await _api.GetCashFlowAsync(accessToken, range.From, range.To, ct), range.Label),
                ReportKind.CustomersOverview => ArabicReportFormatter.FormatCustomers(
                    await _api.GetCustomersOverviewAsync(accessToken, range.From, range.To, ct), range.Label),
                ReportKind.LastSync => ArabicReportFormatter.FormatSyncStatus(
                    await _api.GetSyncStatusAsync(accessToken, ct)),
                _ => ArabicReportFormatter.GenericError()
            };

            // Append last sync footer when available (best-effort, ignore failures).
            if (kind != ReportKind.LastSync)
            {
                try
                {
                    var sync = await _api.GetSyncStatusAsync(accessToken, ct);
                    text += $"\n\nآخر مزامنة: {ArabicReportFormatter.FormatDateTime(sync.LastSyncAt)}";
                }
                catch
                {
                    // ignored
                }
            }

            await _bot.EditTextAsync(
                chatId,
                messageId,
                text,
                BotMenus.AfterResult(kind, ReportCatalog.NeedsPeriod(kind)),
                ct);
        }
        catch (UnauthorizedAccessException)
        {
            await _links.MarkSessionInvalidAsync(telegramUserId, ct);
            await _bot.EditTextAsync(chatId, messageId, ArabicReportFormatter.SessionExpired(), null, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Network error fetching report");
            await _bot.EditTextAsync(chatId, messageId, ArabicReportFormatter.GenericError(), BotMenus.AfterResult(kind, ReportCatalog.NeedsPeriod(kind)), ct);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Timeout fetching report");
            await _bot.EditTextAsync(chatId, messageId, ArabicReportFormatter.GenericError(), BotMenus.AfterResult(kind, ReportCatalog.NeedsPeriod(kind)), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected report error");
            await _bot.EditTextAsync(chatId, messageId, ArabicReportFormatter.GenericError(), BotMenus.AfterResult(kind, ReportCatalog.NeedsPeriod(kind)), ct);
        }
    }

    private async Task<TelegramLinkInfo?> RequireLinkAsync(long telegramUserId, long chatId, int messageId, CancellationToken ct)
    {
        var link = await _links.GetActiveLinkAsync(telegramUserId, ct);
        if (link is null || !link.IsActive)
        {
            await _bot.EditTextAsync(chatId, messageId, ArabicReportFormatter.NotLinked(LinkUrl()), null, ct);
            return null;
        }

        return link;
    }

    private string LinkUrl()
    {
        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        return string.IsNullOrWhiteSpace(baseUrl) ? "/link" : $"{baseUrl}/link";
    }

    private static bool LooksLikeLinkCode(string text)
        => text.Length is >= 6 and <= 16 && text.All(char.IsLetterOrDigit);
}
