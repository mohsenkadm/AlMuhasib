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
    private readonly IReportPdfService _pdfReports;
    private readonly IRateLimiter _rateLimiter;
    private readonly TelegramBotOptions _options;
    private readonly ILogger<BotUpdateProcessor> _logger;

    public BotUpdateProcessor(
        ILinkService links,
        IQaidApiClient api,
        ISessionTokenService sessions,
        ITelegramBotGateway bot,
        IReportPdfService pdfReports,
        IRateLimiter rateLimiter,
        IOptions<TelegramBotOptions> options,
        ILogger<BotUpdateProcessor> logger)
    {
        _links = links;
        _api = api;
        _sessions = sessions;
        _bot = bot;
        _pdfReports = pdfReports;
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
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            // /start <code> — consume deep-link code if present
            if (trimmed.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
            {
                var startParts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (startParts.Length >= 2 && LooksLikeLinkCode(startParts[1]))
                {
                    await TryConsumeCodeAsync(telegramUserId, chatId, startParts[1], ct);
                    return;
                }
            }

            await HandleStartAsync(telegramUserId, chatId, ct);
            return;
        }

        if (trimmed.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
        {
            await _bot.SendTextAsync(chatId, BotCommands.HelpText(), null, ct);
            return;
        }

        if (trimmed.StartsWith("/unlink", StringComparison.OrdinalIgnoreCase))
        {
            await PromptUnlinkAsync(chatId, ct);
            return;
        }

        if (trimmed.StartsWith("/branch", StringComparison.OrdinalIgnoreCase))
        {
            await ShowBranchPickerAsync(telegramUserId, chatId, messageId: null, ct);
            return;
        }

        if (trimmed.StartsWith("/system", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("/dash", StringComparison.OrdinalIgnoreCase))
        {
            await ShowSystemDashboardAsync(telegramUserId, chatId, messageId: null, ct);
            return;
        }

        if (trimmed.StartsWith("/customer", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("/supplier", StringComparison.OrdinalIgnoreCase))
        {
            var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var isCustomer = trimmed.StartsWith("/customer", StringComparison.OrdinalIgnoreCase);
            if (parts.Length < 2)
            {
                await _bot.SendTextAsync(
                    chatId,
                    isCustomer
                        ? "ابحث عن زبون هكذا:\n/customer اسم الزبون"
                        : "ابحث عن مورد هكذا:\n/supplier اسم المورد",
                    null,
                    ct);
                return;
            }

            await SearchLookupAsync(telegramUserId, chatId, messageId: null, parts[1], isCustomer, ct);
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

            await SendNotLinkedAsync(chatId, ct);
            return;
        }

        if (ReportCatalog.TryGetByCommand(trimmed, out var kind))
        {
            await HandleReportCommandAsync(telegramUserId, chatId, kind, ct);
            return;
        }

        if (LooksLikeLinkCode(trimmed))
        {
            var link = await _links.GetActiveLinkAsync(telegramUserId, ct);
            if (link is null || !link.IsActive)
            {
                await TryConsumeCodeAsync(telegramUserId, chatId, trimmed, ct);
                return;
            }
        }

        await HandleStartAsync(telegramUserId, chatId, ct);
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
                case CallbackActionType.OpenGroup:
                    await ShowGroupMenuAsync(telegramUserId, chatId, messageId, action.Group!.Value, ct);
                    break;
                case CallbackActionType.SelectReport:
                case CallbackActionType.PeriodMenu:
                    await HandleSelectReportAsync(telegramUserId, chatId, messageId, action.Kind!.Value, ct);
                    break;
                case CallbackActionType.RunReport:
                    await RunReportAsync(telegramUserId, chatId, messageId, action.Kind!.Value, action.Period!.Value, ct);
                    break;
                case CallbackActionType.BranchList:
                    await ShowBranchPickerAsync(telegramUserId, chatId, messageId, ct);
                    break;
                case CallbackActionType.SelectBranch:
                    await SelectBranchAsync(telegramUserId, chatId, messageId, action.BranchId!.Value, ct);
                    break;
                case CallbackActionType.PromptCustomerSearch:
                    await _bot.EditTextAsync(
                        chatId, messageId,
                        "ابحث عن زبون بإرسال:\n/customer اسم الزبون",
                        BotMenus.AfterResult(ReportKind.LastSync, false),
                        ct);
                    break;
                case CallbackActionType.PromptSupplierSearch:
                    await _bot.EditTextAsync(
                        chatId, messageId,
                        "ابحث عن مورد بإرسال:\n/supplier اسم المورد",
                        BotMenus.AfterResult(ReportKind.LastSync, false),
                        ct);
                    break;
                case CallbackActionType.ShowCustomerStatement:
                    await ShowStatementAsync(telegramUserId, chatId, messageId, action.EntitySyncId!.Value, customers: true, ct);
                    break;
                case CallbackActionType.ShowSupplierStatement:
                    await ShowStatementAsync(telegramUserId, chatId, messageId, action.EntitySyncId!.Value, customers: false, ct);
                    break;
                case CallbackActionType.SystemDashboard:
                    await ShowSystemDashboardAsync(telegramUserId, chatId, messageId, ct);
                    break;
                case CallbackActionType.UnlinkPrompt:
                    await _bot.EditTextAsync(chatId, messageId, "هل تريد إلغاء ربط حساب قيد؟", BotMenus.UnlinkConfirm(), ct);
                    break;
                case CallbackActionType.UnlinkConfirm:
                    await _links.UnlinkAsync(telegramUserId, ct);
                    await _bot.EditTextAsync(
                        chatId,
                        messageId,
                        "تم إلغاء الربط. لن تُعرض أي بيانات حتى تعيد الربط.",
                        BotMenus.NotLinked(LinkUrl()),
                        ct);
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
            var url = LinkUrl();
            await _bot.SendTextAsync(
                chatId,
                ArabicReportFormatter.WelcomeLogin(url),
                BotMenus.NotLinked(url),
                ct);
            return;
        }

        if (await NeedsBranchAsync(link, ct))
        {
            await ShowBranchPickerAsync(telegramUserId, chatId, messageId: null, ct);
            return;
        }

        await _bot.SendTextAsync(
            chatId,
            $"مرحباً بك في بوت تقارير قيد.\nالشركة: {link.CompanyName}\nاختر مجموعة تقارير، ثم التقرير المطلوب.\nالتقارير تُرسل كملف PDF بحجم A4.",
            BotMenus.MainMenu(link.CompanyName),
            ct);
    }

    private async Task HandleReportCommandAsync(long telegramUserId, long chatId, ReportKind kind, CancellationToken ct)
    {
        var link = await _links.GetActiveLinkAsync(telegramUserId, ct);
        if (link is null || !link.IsActive)
        {
            await SendNotLinkedAsync(chatId, ct);
            return;
        }

        if (await NeedsBranchAsync(link, ct))
        {
            await ShowBranchPickerAsync(telegramUserId, chatId, messageId: null, ct);
            return;
        }

        if (ReportCatalog.NeedsPeriod(kind))
        {
            await _bot.SendTextAsync(
                chatId,
                $"تقرير: {ReportCatalog.TitleOf(kind)}\nاختر الفترة الزمنية ثم يُجهَّز ملف PDF:",
                BotMenus.PeriodMenu(kind),
                ct);
            return;
        }

        await RunReportAsync(telegramUserId, chatId, messageId: null, kind, ReportPeriod.Today, ct);
    }

    private async Task SendNotLinkedAsync(long chatId, CancellationToken ct)
    {
        var url = LinkUrl();
        await _bot.SendTextAsync(chatId, ArabicReportFormatter.NotLinked(url), BotMenus.NotLinked(url), ct);
    }

    private async Task TryConsumeCodeAsync(long telegramUserId, long chatId, string code, CancellationToken ct)
    {
        var (ok, message, link) = await _links.ConsumeLinkCodeAsync(code, telegramUserId, ct);
        if (!ok || link is null)
        {
            await _bot.SendTextAsync(chatId, message, BotMenus.NotLinked(LinkUrl()), ct);
            return;
        }

        if (await NeedsBranchAsync(link, ct))
        {
            await _bot.SendTextAsync(
                chatId,
                $"تم الربط بنجاح.\nالشركة: {link.CompanyName}\n{ArabicReportFormatter.NeedBranch()}",
                null,
                ct);
            await ShowBranchPickerAsync(telegramUserId, chatId, messageId: null, ct);
            return;
        }

        await _bot.SendTextAsync(
            chatId,
            $"تم الربط بنجاح.\nالشركة: {link.CompanyName}\nصلاحية الربط حتى: {link.SessionExpiresAtUtc:yyyy/MM/dd} UTC\nاختر مجموعة التقارير:",
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
            await _bot.EditTextAsync(chatId, messageId, ArabicReportFormatter.NotLinked(LinkUrl()), BotMenus.NotLinked(LinkUrl()), ct);
            return;
        }

        if (await NeedsBranchAsync(link, ct))
        {
            await ShowBranchPickerAsync(telegramUserId, chatId, messageId, ct);
            return;
        }

        await _bot.EditTextAsync(
            chatId,
            messageId,
            $"الشركة: {link.CompanyName}\nاختر مجموعة التقارير:",
            BotMenus.MainMenu(link.CompanyName),
            ct);
    }

    private async Task ShowGroupMenuAsync(long telegramUserId, long chatId, int messageId, ReportGroup group, CancellationToken ct)
    {
        var link = await _links.GetActiveLinkAsync(telegramUserId, ct);
        if (link is null || !link.IsActive)
        {
            await _bot.EditTextAsync(chatId, messageId, ArabicReportFormatter.NotLinked(LinkUrl()), BotMenus.NotLinked(LinkUrl()), ct);
            return;
        }

        await _bot.EditTextAsync(
            chatId,
            messageId,
            $"{ReportCatalog.GroupTitle(group)}\nاختر تقريراً (يُجهَّز PDF):",
            BotMenus.GroupMenu(group),
            ct);
    }

    private async Task ShowBranchPickerAsync(long telegramUserId, long chatId, int? messageId, CancellationToken ct)
    {
        var link = await _links.GetActiveLinkAsync(telegramUserId, ct);
        if (link is null || !link.IsActive)
        {
            await ReplyAsync(chatId, messageId, ArabicReportFormatter.NotLinked(LinkUrl()), BotMenus.NotLinked(LinkUrl()), ct);
            return;
        }

        var (ok, accessToken, _) = await _sessions.GetValidAccessTokenAsync(link, ct);
        if (!ok || accessToken is null)
        {
            await ReplyAsync(chatId, messageId, ArabicReportFormatter.SessionExpired(), BotMenus.NotLinked(LinkUrl()), ct);
            return;
        }

        try
        {
            var refreshed = await _api.RefreshAsync(link.Tokens.RefreshToken, ct);
            var branches = refreshed.AllowedBranches;
            if (branches.Count == 0)
            {
                await ReplyAsync(chatId, messageId, "لا توجد فروع متاحة لهذا الحساب.", BotMenus.MainMenu(link.CompanyName), ct);
                return;
            }

            if (branches.Count == 1)
            {
                await ApplyBranchAsync(telegramUserId, chatId, messageId, link, branches[0].BranchId, accessToken, ct);
                return;
            }

            await ReplyAsync(
                chatId,
                messageId,
                $"الشركة: {link.CompanyName}\nاختر الفرع:",
                BotMenus.BranchPicker(branches),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load branches");
            await ReplyAsync(chatId, messageId, ArabicReportFormatter.GenericError(), null, ct);
        }
    }

    private async Task SelectBranchAsync(long telegramUserId, long chatId, int messageId, int branchId, CancellationToken ct)
    {
        var link = await _links.GetActiveLinkAsync(telegramUserId, ct);
        if (link is null || !link.IsActive)
        {
            await _bot.EditTextAsync(chatId, messageId, ArabicReportFormatter.NotLinked(LinkUrl()), BotMenus.NotLinked(LinkUrl()), ct);
            return;
        }

        var (ok, accessToken, _) = await _sessions.GetValidAccessTokenAsync(link, ct);
        if (!ok || accessToken is null)
        {
            await _bot.EditTextAsync(chatId, messageId, ArabicReportFormatter.SessionExpired(), BotMenus.NotLinked(LinkUrl()), ct);
            return;
        }

        await ApplyBranchAsync(telegramUserId, chatId, messageId, link, branchId, accessToken, ct);
    }

    private async Task ApplyBranchAsync(
        long telegramUserId, long chatId, int? messageId, TelegramLinkInfo link, int branchId, string accessToken, CancellationToken ct)
    {
        try
        {
            var selected = await _api.SelectBranchAsync(accessToken, branchId, ct);
            await _links.UpdateBranchAsync(telegramUserId, branchId, new StoredSessionTokens
            {
                AccessToken = selected.AccessToken,
                RefreshToken = link.Tokens.RefreshToken,
                AccessTokenExpiresAt = selected.AccessTokenExpiresAt
            }, ct);

            var name = selected.Branch?.Name ?? $"#{branchId}";
            await ReplyAsync(
                chatId,
                messageId,
                $"تم اختيار الفرع: {name}\nالشركة: {link.CompanyName}\nاختر مجموعة التقارير:",
                BotMenus.MainMenu(link.CompanyName),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Branch selection failed");
            await ReplyAsync(chatId, messageId, "تعذّر اختيار الفرع. حاول مجدداً.", null, ct);
        }
    }

    private async Task SearchLookupAsync(
        long telegramUserId, long chatId, int? messageId, string search, bool customers, CancellationToken ct)
    {
        var link = messageId is int mid
            ? await RequireReadyLinkAsync(telegramUserId, chatId, mid, ct)
            : await RequireReadyLinkForSendAsync(telegramUserId, chatId, ct);
        if (link is null)
            return;

        var (ok, accessToken, _) = await _sessions.GetValidAccessTokenAsync(link, ct);
        if (!ok || accessToken is null)
        {
            await ReplyAsync(chatId, messageId, ArabicReportFormatter.SessionExpired(), BotMenus.NotLinked(LinkUrl()), ct);
            return;
        }

        try
        {
            var items = customers
                ? await _api.SearchCustomersAsync(accessToken, search, ct)
                : await _api.SearchSuppliersAsync(accessToken, search, ct);

            if (items.Count == 0)
            {
                await ReplyAsync(chatId, messageId, $"لا نتائج لـ «{search}».", BotMenus.MainMenu(link.CompanyName), ct);
                return;
            }

            if (items.Count == 1)
            {
                await ShowStatementAsync(telegramUserId, chatId, messageId, items[0].SyncId, customers, ct);
                return;
            }

            await ReplyAsync(
                chatId,
                messageId,
                $"نتائج البحث عن «{search}» — اختر:",
                BotMenus.LookupResults(items, customers),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lookup search failed");
            await ReplyAsync(chatId, messageId, ArabicReportFormatter.GenericError(), null, ct);
        }
    }

    private async Task ShowStatementAsync(
        long telegramUserId, long chatId, int? messageId, Guid syncId, bool customers, CancellationToken ct)
    {
        var link = messageId is int mid
            ? await RequireReadyLinkAsync(telegramUserId, chatId, mid, ct)
            : await RequireReadyLinkForSendAsync(telegramUserId, chatId, ct);
        if (link is null)
            return;

        var (ok, accessToken, _) = await _sessions.GetValidAccessTokenAsync(link, ct);
        if (!ok || accessToken is null)
        {
            await ReplyAsync(chatId, messageId, ArabicReportFormatter.SessionExpired(), BotMenus.NotLinked(LinkUrl()), ct);
            return;
        }

        try
        {
            await ReplyAsync(chatId, messageId, "جاري تجهيز كشف الحساب PDF...", null, ct);
            var pdf = await _pdfReports.BuildStatementPdfAsync(customers, syncId, accessToken, link.CompanyName, ct);
            await _bot.SendDocumentAsync(chatId, pdf.Content, pdf.FileName, pdf.Caption, BotMenus.MainMenu(link.CompanyName), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Statement PDF failed");
            await _bot.SendTextAsync(chatId, ArabicReportFormatter.GenericError(), BotMenus.MainMenu(link.CompanyName), ct);
        }
    }

    private async Task ShowSystemDashboardAsync(long telegramUserId, long chatId, int? messageId, CancellationToken ct)
    {
        var link = messageId is int mid
            ? await RequireReadyLinkAsync(telegramUserId, chatId, mid, ct)
            : await RequireReadyLinkForSendAsync(telegramUserId, chatId, ct);
        if (link is null)
            return;

        var (ok, accessToken, _) = await _sessions.GetValidAccessTokenAsync(link, ct);
        if (!ok || accessToken is null)
        {
            await ReplyAsync(chatId, messageId, ArabicReportFormatter.SessionExpired(), BotMenus.NotLinked(LinkUrl()), ct);
            return;
        }

        var systemType = link.ApplicationSystemType;
        try
        {
            await ReplyAsync(chatId, messageId, "جاري تجهيز لوحة النظام PDF...", null, ct);
            var pdf = await _pdfReports.BuildDashboardPdfAsync(systemType, accessToken, link.CompanyName, ct);
            await _bot.SendDocumentAsync(chatId, pdf.Content, pdf.FileName, pdf.Caption, BotMenus.MainMenu(link.CompanyName), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "System dashboard PDF failed for type {Type}", systemType);
            await _bot.SendTextAsync(chatId, ArabicReportFormatter.GenericError(), BotMenus.MainMenu(link.CompanyName), ct);
        }
    }

    private async Task HandleSelectReportAsync(long telegramUserId, long chatId, int messageId, ReportKind kind, CancellationToken ct)
    {
        var link = await RequireReadyLinkAsync(telegramUserId, chatId, messageId, ct);
        if (link is null)
            return;

        if (ReportCatalog.NeedsPeriod(kind))
        {
            await _bot.EditTextAsync(
                chatId,
                messageId,
                $"تقرير: {ReportCatalog.TitleOf(kind)}\nاختر الفترة الزمنية ثم يُجهَّز ملف PDF:",
                BotMenus.PeriodMenu(kind),
                ct);
            return;
        }

        await RunReportAsync(telegramUserId, chatId, messageId, kind, ReportPeriod.Today, ct);
    }

    private async Task RunReportAsync(
        long telegramUserId, long chatId, int? messageId, ReportKind kind, ReportPeriod period, CancellationToken ct)
    {
        var link = messageId is int mid
            ? await RequireReadyLinkAsync(telegramUserId, chatId, mid, ct)
            : await RequireReadyLinkForSendAsync(telegramUserId, chatId, ct);
        if (link is null)
            return;

        var (ok, accessToken, _) = await _sessions.GetValidAccessTokenAsync(link, ct);
        if (!ok || accessToken is null)
        {
            await ReplyAsync(chatId, messageId, ArabicReportFormatter.SessionExpired(), BotMenus.NotLinked(LinkUrl()), ct);
            return;
        }

        var range = ReportPeriodHelper.Resolve(period);
        var keyboard = BotMenus.AfterResult(kind, ReportCatalog.NeedsPeriod(kind));
        try
        {
            await ReplyAsync(chatId, messageId, $"جاري تجهيز PDF: {ReportCatalog.TitleOf(kind)}...", null, ct);
            var pdf = await _pdfReports.BuildReportPdfAsync(kind, accessToken, link.CompanyName, range, ct);
            await _bot.SendDocumentAsync(chatId, pdf.Content, pdf.FileName, pdf.Caption, keyboard, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await _links.MarkSessionInvalidAsync(telegramUserId, ct);
            await _bot.SendTextAsync(chatId, ArabicReportFormatter.SessionExpired(), BotMenus.NotLinked(LinkUrl()), ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Network/timeout fetching report PDF");
            await _bot.SendTextAsync(chatId, ArabicReportFormatter.GenericError(), keyboard, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected report PDF error");
            await _bot.SendTextAsync(chatId, ArabicReportFormatter.GenericError(), keyboard, ct);
        }
    }

    private async Task<bool> NeedsBranchAsync(TelegramLinkInfo link, CancellationToken ct)
    {
        if (link.CurrentBranchId is > 0)
            return false;

        try
        {
            if (string.IsNullOrWhiteSpace(link.Tokens.RefreshToken))
                return true;

            var refreshed = await _api.RefreshAsync(link.Tokens.RefreshToken, ct);
            if (refreshed.AllowedBranches.Count <= 1)
            {
                if (refreshed.AllowedBranches.Count == 1)
                {
                    var b = refreshed.AllowedBranches[0];
                    var selected = await _api.SelectBranchAsync(refreshed.AccessToken, b.BranchId, ct);
                    await _links.UpdateBranchAsync(link.TelegramUserId, b.BranchId, new StoredSessionTokens
                    {
                        AccessToken = selected.AccessToken,
                        RefreshToken = string.IsNullOrWhiteSpace(refreshed.RefreshToken)
                            ? link.Tokens.RefreshToken
                            : refreshed.RefreshToken,
                        AccessTokenExpiresAt = selected.AccessTokenExpiresAt
                    }, ct);
                }
                return false;
            }

            return true;
        }
        catch
        {
            return link.CurrentBranchId is null or <= 0;
        }
    }

    private async Task<TelegramLinkInfo?> RequireReadyLinkAsync(long telegramUserId, long chatId, int messageId, CancellationToken ct)
    {
        var link = await _links.GetActiveLinkAsync(telegramUserId, ct);
        if (link is null || !link.IsActive)
        {
            await _bot.EditTextAsync(chatId, messageId, ArabicReportFormatter.NotLinked(LinkUrl()), BotMenus.NotLinked(LinkUrl()), ct);
            return null;
        }

        if (await NeedsBranchAsync(link, ct))
        {
            await ShowBranchPickerAsync(telegramUserId, chatId, messageId, ct);
            return null;
        }

        return await _links.GetActiveLinkAsync(telegramUserId, ct);
    }

    private async Task<TelegramLinkInfo?> RequireReadyLinkForSendAsync(long telegramUserId, long chatId, CancellationToken ct)
    {
        var link = await _links.GetActiveLinkAsync(telegramUserId, ct);
        if (link is null || !link.IsActive)
        {
            await SendNotLinkedAsync(chatId, ct);
            return null;
        }

        if (await NeedsBranchAsync(link, ct))
        {
            await ShowBranchPickerAsync(telegramUserId, chatId, messageId: null, ct);
            return null;
        }

        return await _links.GetActiveLinkAsync(telegramUserId, ct);
    }

    private async Task ReplyAsync(
        long chatId, int? messageId, string text, IReadOnlyList<IReadOnlyList<BotButton>>? keyboard, CancellationToken ct)
    {
        if (messageId is int mid)
            await _bot.EditTextAsync(chatId, mid, text, keyboard, ct);
        else
            await _bot.SendTextAsync(chatId, text, keyboard, ct);
    }

    private string LinkUrl()
    {
        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        return string.IsNullOrWhiteSpace(baseUrl) ? "/link" : $"{baseUrl}/link";
    }

    private static bool LooksLikeLinkCode(string text)
        => text.Length is >= 6 and <= 16 && text.All(char.IsLetterOrDigit);
}
