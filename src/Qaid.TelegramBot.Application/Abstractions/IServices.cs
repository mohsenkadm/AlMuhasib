using System.Text.Json;
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
    Task UpdateBranchAsync(long telegramUserId, int branchId, StoredSessionTokens tokens, CancellationToken ct);
    Task UpdateApplicationSystemTypeAsync(long telegramUserId, int applicationSystemType, CancellationToken ct);
    Task<bool> UnlinkAsync(long telegramUserId, CancellationToken ct);
    Task MarkSessionInvalidAsync(long telegramUserId, CancellationToken ct);
}

public interface IQaidApiClient
{
    Task<JsonElement> GetJsonAsync(string relativePath, string accessToken, string? query, CancellationToken ct);
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
    Task<DailyOperationsReportResult> GetDailyOperationsAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct);
    Task<OverdueResult> GetInstallmentsOverdueAsync(string accessToken, CancellationToken ct);
    Task<InstallmentsSummaryResult> GetInstallmentsSummaryAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct);
    Task<OverdueCustomersReportResult> GetOverdueCustomersAsync(string accessToken, CancellationToken ct);
    Task<ReceivablesAgingReportResult> GetReceivablesAgingAsync(string accessToken, CancellationToken ct);
    Task<MinimumQuantityReportResult> GetMinimumQuantityAsync(string accessToken, CancellationToken ct);
    Task<StockHealthReportResult> GetStockHealthAsync(string accessToken, CancellationToken ct);
    Task<TopProductsReportResult> GetTopProductsAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct);
    Task<PurchasesReportResult> GetPurchasesAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct);
    Task<SuppliersOverviewReportResult> GetSuppliersOverviewAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct);
    Task<SalesByPaymentMethodReportResult> GetSalesByPaymentMethodAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct);
    Task<PayablesAgingReportResult> GetPayablesAgingAsync(string accessToken, CancellationToken ct);
    Task<CustomerCollectionsReportResult> GetCustomerCollectionsAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct);
    Task<SupplierPaymentsReportResult> GetSupplierPaymentsAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct);
    Task<FinancialPositionSummaryReportResult> GetFinancialPositionAsync(string accessToken, CancellationToken ct);
    Task<CogsReportResult> GetCogsAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct);
    Task<InventoryValuationReportResult> GetInventoryValuationAsync(string accessToken, CancellationToken ct);
    Task<TransfersReportResult> GetTransfersAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct);
    Task<GrossProfitMarginReportResult> GetGrossProfitMarginAsync(string accessToken, DateTime? from, DateTime? to, CancellationToken ct);
    Task<List<LookupItemDto>> SearchCustomersAsync(string accessToken, string search, CancellationToken ct);
    Task<List<LookupItemDto>> SearchSuppliersAsync(string accessToken, string search, CancellationToken ct);
    Task<CustomerStatementResult> GetCustomerStatementAsync(string accessToken, Guid customerSyncId, CancellationToken ct);
    Task<SupplierStatementResult> GetSupplierStatementAsync(string accessToken, Guid supplierSyncId, CancellationToken ct);
    Task<CarDashboardDto> GetCarDashboardAsync(string accessToken, CancellationToken ct);
    Task<CarTradeDashboardDto> GetCarTradeDashboardAsync(string accessToken, CancellationToken ct);
    Task<RealEstateDashboardDto> GetRealEstateDashboardAsync(string accessToken, CancellationToken ct);
    Task<HotelRestaurantSummaryDto> GetHotelRestaurantSummaryAsync(string accessToken, CancellationToken ct);
    Task<GoldStockReportDto> GetGoldStockAsync(string accessToken, CancellationToken ct);
}

public interface ISessionTokenService
{
    Task<(bool Ok, string? AccessToken, string ErrorMessage)> GetValidAccessTokenAsync(TelegramLinkInfo link, CancellationToken ct);
}

public interface ITelegramBotGateway
{
    Task SendTextAsync(long chatId, string text, IReadOnlyList<IReadOnlyList<BotButton>>? keyboard, CancellationToken ct);
    Task SendDocumentAsync(long chatId, byte[] pdfContent, string fileName, string caption, IReadOnlyList<IReadOnlyList<BotButton>>? keyboard, CancellationToken ct);
    Task AnswerCallbackAsync(string callbackQueryId, string? text, CancellationToken ct);
    Task EditTextAsync(long chatId, int messageId, string text, IReadOnlyList<IReadOnlyList<BotButton>>? keyboard, CancellationToken ct);
}

public sealed class BotButton
{
    public required string Text { get; init; }
    public string? CallbackData { get; init; }
    public string? Url { get; init; }
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
