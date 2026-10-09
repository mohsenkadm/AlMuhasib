using Qaid.TelegramBot.Application.Models;

namespace Qaid.TelegramBot.Application.Abstractions;

public interface ITokenProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedPayload);
}

public interface ILinkService
{
    Task<CreateLinkCodeResult> CreateLinkCodeAsync(CreateLinkCodeRequest request, CancellationToken ct);
    Task<(bool Ok, string Message, TelegramLinkInfo? Link)> ConsumeLinkCodeAsync(string plainCode, long telegramUserId, CancellationToken ct);
    Task<TelegramLinkInfo?> GetActiveLinkAsync(long telegramUserId, CancellationToken ct);
    Task UpdateTokensAsync(long telegramUserId, StoredSessionTokens tokens, CancellationToken ct);
    Task<bool> UnlinkAsync(long telegramUserId, CancellationToken ct);
    Task MarkSessionInvalidAsync(long telegramUserId, CancellationToken ct);
}

public interface IQaidApiClient
{
    Task<TenantLoginResponse> LoginAsync(string username, string password, CancellationToken ct);
    Task<SelectBranchResponse> SelectBranchAsync(string accessToken, int branchId, CancellationToken ct);
    Task<TenantLoginResponse> RefreshAsync(string refreshToken, CancellationToken ct);
    Task<SyncStatusResponse> GetSyncStatusAsync(string accessToken, CancellationToken ct);
    Task<DailySalesReportResult> GetDailySalesAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct);
    Task<ExpensesReportResult> GetExpensesAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct);
    Task<ProfitAndLossReportResult> GetProfitAndLossAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct);
    Task<CashBalancesSummaryReportResult> GetCashBalancesAsync(string accessToken, CancellationToken ct);
    Task<CashFlowResult> GetCashFlowAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct);
    Task<CustomersOverviewReportResult> GetCustomersOverviewAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct);
}

public interface ISessionTokenService
{
    Task<(bool Ok, string? AccessToken, string ErrorMessage)> GetValidAccessTokenAsync(TelegramLinkInfo link, CancellationToken ct);
}

public interface ITelegramBotGateway
{
    Task SendTextAsync(long chatId, string text, IReadOnlyList<IReadOnlyList<BotButton>>? keyboard, CancellationToken ct);
    Task AnswerCallbackAsync(string callbackQueryId, string? text, CancellationToken ct);
    Task EditTextAsync(long chatId, int messageId, string text, IReadOnlyList<IReadOnlyList<BotButton>>? keyboard, CancellationToken ct);
}

public sealed class BotButton
{
    public required string Text { get; init; }
    public required string CallbackData { get; init; }
}

public interface IBotUpdateProcessor
{
    Task ProcessMessageAsync(long telegramUserId, long chatId, string text, CancellationToken ct);
    Task ProcessCallbackAsync(long telegramUserId, long chatId, int messageId, string callbackQueryId, string data, CancellationToken ct);
}

public interface IRateLimiter
{
    bool TryAcquire(long telegramUserId);
}
