using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ICashBankService
{
    // ── CashBoxes ────────────────────────────────────────
    Task<IEnumerable<CashBox>> GetAllCashBoxesAsync();
    Task<CashBox> AddCashBoxAsync(string name, decimal initialBalance = 0);
    Task UpdateCashBoxAsync(int id, string name);
    Task DeleteCashBoxAsync(int id);

    // ── BankAccounts ─────────────────────────────────────
    Task<IEnumerable<BankAccount>> GetAllBankAccountsAsync();
    Task<BankAccount> AddBankAccountAsync(string name, string? accountNumber, decimal initialBalance = 0);

    // ── Transfers ────────────────────────────────────────
    Task<Transfer> CreateTransferAsync(TransferAccountType fromType, int fromId,
        TransferAccountType toType, int toId, decimal amount, string? notes);
    Task<(IEnumerable<Transfer> Items, int TotalCount)> GetPagedTransfersAsync(
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
}
