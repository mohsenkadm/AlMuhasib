using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ICashBankService
{
    // ── CashBoxes ────────────────────────────────────────
    Task<IEnumerable<CashBox>> GetAllCashBoxesAsync();
    Task<CashBox> AddCashBoxAsync(string name, decimal initialBalance = 0, AccountingCurrency currency = AccountingCurrency.IQD);
    Task UpdateCashBoxAsync(int id, string name);
    Task DeleteCashBoxAsync(int id);
    Task AdjustCashBoxBalanceAsync(int cashBoxId, decimal delta, string reason, DateTime date);

    // ── BankAccounts ─────────────────────────────────────
    Task<IEnumerable<BankAccount>> GetAllBankAccountsAsync();
    Task<BankAccount> AddBankAccountAsync(string name, string? accountNumber, decimal initialBalance = 0, AccountingCurrency currency = AccountingCurrency.IQD);
    Task UpdateBankAccountAsync(int id, string name, string? accountNumber);
    Task DeleteBankAccountAsync(int id);
    Task AdjustBankBalanceAsync(int bankAccountId, decimal delta, string reason, DateTime date);

    // ── Transfers ────────────────────────────────────────
    Task<Transfer> CreateTransferAsync(TransferAccountType fromType, int fromId,
        TransferAccountType toType, int toId, decimal amount, string? notes);
    Task ReverseTransferAsync(int transferId);
    Task<(IEnumerable<TransferDisplayItem> Items, int TotalCount)> GetPagedTransfersAsync(
        int page, int pageSize, DateTime? fromDate = null, DateTime? toDate = null);

    // ── Vouchers ─────────────────────────────────────────
    Task<Voucher> CreateVoucherAsync(Voucher voucher);
    Task DeleteVoucherAsync(int id);
    Task SetVoucherReconciledAsync(int voucherId, bool isReconciled);
    Task<string> GetNextVoucherNumberAsync(VoucherType type);
    Task<(IEnumerable<Voucher> Items, int TotalCount)> GetPagedVouchersAsync(
        int page, int pageSize, VoucherType? type = null, DateTime? fromDate = null,
        DateTime? toDate = null, string? searchTerm = null);
    Task<IReadOnlyList<Invoice>> GetOpenCreditInvoicesForCustomerAsync(int customerId);
    Task<IReadOnlyList<Installment>> GetOpenInstallmentsForCustomerAsync(int customerId);

    // ── Transaction history ──────────────────────────────
    Task<IEnumerable<Voucher>> GetVouchersByCashBoxAsync(int cashBoxId);
    Task<IEnumerable<Transfer>> GetTransfersByCashBoxAsync(int cashBoxId);
    Task<IEnumerable<Voucher>> GetVouchersByBankAsync(int bankAccountId);
    Task<IEnumerable<Transfer>> GetTransfersByBankAsync(int bankAccountId);

    /// <summary>كشف حساب كامل للصندوق مع رصيد جاري.</summary>
    Task<IReadOnlyList<AccountStatementEntry>> GetCashBoxStatementAsync(
        int cashBoxId, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>كشف حساب كامل للمصرف مع رصيد جاري.</summary>
    Task<IReadOnlyList<AccountStatementEntry>> GetBankStatementAsync(
        int bankAccountId, DateTime? fromDate = null, DateTime? toDate = null);
}
