using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class CashBankService : ICashBankService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAccountingPeriodLockService _periodLockService;

    public CashBankService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICurrentUserService currentUserService,
        IAccountingPeriodLockService periodLockService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _periodLockService = periodLockService;
    }

    // ══════════════════════════════════════════════════════
    // CashBoxes
    // ══════════════════════════════════════════════════════
    public async Task<IEnumerable<CashBox>> GetAllCashBoxesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CashBoxes.ToListAsync();
    }

    public async Task<CashBox> AddCashBoxAsync(string name, decimal initialBalance = 0)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var username = _currentUserService.Username;
        var cashBox = new CashBox
        {
            Name = name,
            Balance = initialBalance,
            CreatedBy = username,
            CreatedAt = DateTime.UtcNow
        };
        await context.CashBoxes.AddAsync(cashBox);
        await context.SaveChangesAsync();

        if (_currentUserService.UserId.HasValue)
        {
            await context.AuditLogs.AddAsync(new AuditLog
            {
                UserId = _currentUserService.UserId.Value,
                Action = AuditAction.Add,
                EntityName = "CashBox",
                EntityId = cashBox.Id,
                NewValues = $"قاصة: {name}, الرصيد: {initialBalance:N0}",
                Timestamp = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        return cashBox;
    }

    public async Task UpdateCashBoxAsync(int id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("يرجى إدخال اسم القاصة");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var cashBox = await context.CashBoxes.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("الصندوق غير موجود");

        var trimmed = name.Trim();
        var oldName = cashBox.Name;
        cashBox.Name = trimmed;
        cashBox.UpdatedAt = DateTime.UtcNow;
        cashBox.UpdatedBy = _currentUserService.Username;
        await context.SaveChangesAsync();

        if (_currentUserService.UserId.HasValue)
        {
            await context.AuditLogs.AddAsync(new AuditLog
            {
                UserId = _currentUserService.UserId.Value,
                Action = AuditAction.Edit,
                EntityName = "CashBox",
                EntityId = cashBox.Id,
                OldValues = $"قاصة: {oldName}",
                NewValues = $"قاصة: {trimmed}",
                Timestamp = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteCashBoxAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cashBox = await context.CashBoxes.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("الصندوق غير موجود");

        var hasVouchers = await context.Vouchers.AnyAsync(v => v.CashBoxId == id);
        if (hasVouchers)
            throw new InvalidOperationException("لا يمكن حذف الصندوق لوجود سندات مرتبطة به");

        var hasTransfers = await context.Transfers.AnyAsync(t =>
            (t.FromType == TransferAccountType.CashBox && t.FromId == id)
            || (t.ToType == TransferAccountType.CashBox && t.ToId == id));
        if (hasTransfers)
            throw new InvalidOperationException("لا يمكن حذف الصندوق لوجود تحويلات مرتبطة به");

        var hasInvoices = await context.Invoices.AnyAsync(i => i.CashBoxId == id);
        if (hasInvoices)
            throw new InvalidOperationException("لا يمكن حذف الصندوق لوجود فواتير مرتبطة به");

        var hasExpenses = await context.Expenses.AnyAsync(e => e.CashBoxId == id);
        if (hasExpenses)
            throw new InvalidOperationException("لا يمكن حذف الصندوق لوجود مصروفات مرتبطة به");

        var username = _currentUserService.Username ?? "system";
        cashBox.MarkSoftDeleted(username);
        await context.SaveChangesAsync();

        if (_currentUserService.UserId.HasValue)
        {
            await context.AuditLogs.AddAsync(new AuditLog
            {
                UserId = _currentUserService.UserId.Value,
                Action = AuditAction.Delete,
                EntityName = "CashBox",
                EntityId = cashBox.Id,
                OldValues = $"قاصة: {cashBox.Name}, الرصيد: {cashBox.Balance:N0}",
                Timestamp = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }

    // ══════════════════════════════════════════════════════
    // BankAccounts
    // ══════════════════════════════════════════════════════
    public async Task<IEnumerable<BankAccount>> GetAllBankAccountsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.BankAccounts.ToListAsync();
    }

    public async Task<BankAccount> AddBankAccountAsync(string name, string? accountNumber, decimal initialBalance = 0)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var username = _currentUserService.Username;
        var bank = new BankAccount
        {
            Name = name,
            AccountNumber = accountNumber,
            Balance = initialBalance,
            CreatedBy = username,
            CreatedAt = DateTime.UtcNow
        };
        await context.BankAccounts.AddAsync(bank);
        await context.SaveChangesAsync();

        if (_currentUserService.UserId.HasValue)
        {
            await context.AuditLogs.AddAsync(new AuditLog
            {
                UserId = _currentUserService.UserId.Value,
                Action = AuditAction.Add,
                EntityName = "BankAccount",
                EntityId = bank.Id,
                NewValues = $"مصرف: {name}, الرصيد: {initialBalance:N0}",
                Timestamp = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        return bank;
    }

    // ══════════════════════════════════════════════════════
    // Transfers
    // ══════════════════════════════════════════════════════
    public async Task<Transfer> CreateTransferAsync(TransferAccountType fromType, int fromId,
        TransferAccountType toType, int toId, decimal amount, string? notes)
    {
        if (amount <= 0)
            throw new InvalidOperationException("مبلغ التحويل يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var username = _currentUserService.Username;

            if (fromType == TransferAccountType.CashBox)
            {
                var cashBox = await context.CashBoxes.FindAsync(fromId)
                    ?? throw new InvalidOperationException("القاصة المصدر غير موجودة");
                if (cashBox.Balance < amount)
                    throw new InvalidOperationException($"رصيد القاصة ({cashBox.Balance:N0}) غير كافٍ للتحويل ({amount:N0})");
                cashBox.Balance -= amount;
                cashBox.UpdatedBy = username;
                cashBox.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var bank = await context.BankAccounts.FindAsync(fromId)
                    ?? throw new InvalidOperationException("المصرف المصدر غير موجود");
                if (bank.Balance < amount)
                    throw new InvalidOperationException($"رصيد المصرف ({bank.Balance:N0}) غير كافٍ للتحويل ({amount:N0})");
                bank.Balance -= amount;
                bank.UpdatedBy = username;
                bank.UpdatedAt = DateTime.UtcNow;
            }

            if (toType == TransferAccountType.CashBox)
            {
                var cashBox = await context.CashBoxes.FindAsync(toId)
                    ?? throw new InvalidOperationException("القاصة الهدف غير موجودة");
                cashBox.Balance += amount;
                cashBox.UpdatedBy = username;
                cashBox.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var bank = await context.BankAccounts.FindAsync(toId)
                    ?? throw new InvalidOperationException("المصرف الهدف غير موجود");
                bank.Balance += amount;
                bank.UpdatedBy = username;
                bank.UpdatedAt = DateTime.UtcNow;
            }

            var transfer = new Transfer
            {
                FromType = fromType, FromId = fromId,
                ToType = toType, ToId = toId,
                Amount = amount, Date = DateTime.Now,
                Notes = notes, CreatedBy = username, CreatedAt = DateTime.UtcNow
            };
            await context.Transfers.AddAsync(transfer);
            await context.SaveChangesAsync();

            if (_currentUserService.UserId.HasValue)
            {
                await context.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = _currentUserService.UserId.Value,
                    Action = AuditAction.Add,
                    EntityName = "Transfer",
                    EntityId = transfer.Id,
                    NewValues = $"تحويل: {amount:N0} من {fromType}({fromId}) إلى {toType}({toId})",
                    Timestamp = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return transfer;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<(IEnumerable<Transfer> Items, int TotalCount)> GetPagedTransfersAsync(
        int page, int pageSize, DateTime? fromDate = null, DateTime? toDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Transfers.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(t => t.Date >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(t => t.Date <= toDate.Value.AddDays(1));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    // ══════════════════════════════════════════════════════
    // Vouchers
    // ══════════════════════════════════════════════════════
    public async Task<Voucher> CreateVoucherAsync(Voucher voucher)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await _periodLockService.EnsureDateAllowedAsync(voucher.Date);

            var username = _currentUserService.Username;
            voucher.CreatedBy = username;
            voucher.CreatedAt = DateTime.UtcNow;

            // Always assign server-side to avoid stale UI numbers and soft-delete collisions.
            voucher.VoucherNumber = await GetNextVoucherNumberAsync(context, voucher.VoucherType);

            if (voucher.InvoiceId.HasValue && voucher.InstallmentId.HasValue)
                throw new InvalidOperationException("لا يمكن ربط السند بفاتورة وقسط في نفس الوقت");

            switch (voucher.VoucherType)
            {
                case VoucherType.Receipt:
                    await AdjustCashBoxBalance(context, voucher.CashBoxId, voucher.Amount, username);
                    if (voucher.InstallmentId.HasValue)
                        await ApplyAmountToInstallmentAsync(context, voucher, username, adjustCash: false);
                    else if (voucher.InvoiceId.HasValue)
                        await ApplyAmountToCreditInvoiceAsync(context, voucher, username);
                    break;

                case VoucherType.DebtReceipt:
                    await AdjustCashBoxBalance(context, voucher.CashBoxId, voucher.Amount, username);
                    if (voucher.InstallmentId.HasValue)
                        await ApplyAmountToInstallmentAsync(context, voucher, username, adjustCash: false);
                    else if (voucher.InvoiceId.HasValue)
                        await ApplyAmountToCreditInvoiceAsync(context, voucher, username);
                    else if (voucher.CustomerId.HasValue)
                        await ApplyDebtReceiptToCreditInvoicesAsync(context, voucher, username);
                    break;

                case VoucherType.Payment:
                    await ValidateAndDeductCashBox(context, voucher.CashBoxId, voucher.Amount, username);
                    break;

                case VoucherType.BankReceipt:
                    if (!voucher.BankAccountId.HasValue)
                        throw new InvalidOperationException("يجب تحديد المصرف لسند القبض المصرفي");
                    var netAmount = voucher.Amount - voucher.BankFees;
                    await AdjustCashBoxBalance(context, voucher.CashBoxId, netAmount, username);
                    await ValidateAndDeductBank(context, voucher.BankAccountId.Value, voucher.Amount, username);
                    break;

                case VoucherType.InvestorDeposit:
                    if (!voucher.InvestorId.HasValue)
                        throw new InvalidOperationException("يجب تحديد المستثمر لسند إيداع المستثمر");
                    await AdjustCashBoxBalance(context, voucher.CashBoxId, voucher.Amount, username);
                    await AdjustInvestorDeposit(context, voucher.InvestorId.Value, voucher.Amount, username);
                    await CreateInvestorTransaction(context, voucher.InvestorId.Value,
                        InvestorTransactionType.Deposit, voucher.Amount, voucher.Notes, username);
                    break;

                case VoucherType.InvestorWithdrawal:
                    if (!voucher.InvestorId.HasValue)
                        throw new InvalidOperationException("يجب تحديد المستثمر لسند سحب المستثمر");
                    await ValidateAndDeductCashBox(context, voucher.CashBoxId, voucher.Amount, username);
                    await AdjustInvestorDeposit(context, voucher.InvestorId.Value, -voucher.Amount, username);
                    await CreateInvestorTransaction(context, voucher.InvestorId.Value,
                        InvestorTransactionType.Withdrawal, voucher.Amount, voucher.Notes, username);
                    break;
            }

            await context.Vouchers.AddAsync(voucher);
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                voucher.VoucherNumber = await GetNextVoucherNumberAsync(context, voucher.VoucherType);
                await context.SaveChangesAsync();
            }

            if (_currentUserService.UserId.HasValue)
            {
                await context.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = _currentUserService.UserId.Value,
                    Action = AuditAction.Add,
                    EntityName = "Voucher",
                    EntityId = voucher.Id,
                    NewValues = $"سند {GetVoucherTypeName(voucher.VoucherType)}: {voucher.VoucherNumber}, المبلغ: {voucher.Amount:N0}",
                    Timestamp = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return voucher;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
        {
            var message = inner.Message;
            if (message.Contains("IX_Vouchers_VoucherNumber", StringComparison.OrdinalIgnoreCase)
                || message.Contains("unique index", StringComparison.OrdinalIgnoreCase)
                || message.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase)
                || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
                return true;

            // SQL Server unique/duplicate: 2601, 2627
            var numberProp = inner.GetType().GetProperty("Number");
            if (numberProp?.GetValue(inner) is int number && number is 2601 or 2627)
                return true;
        }

        return false;
    }

    public async Task DeleteVoucherAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var voucher = await context.Vouchers
                .FirstOrDefaultAsync(v => v.Id == id)
                ?? throw new InvalidOperationException("السند غير موجود");

            if (voucher.IsDeleted)
                return;

            await _periodLockService.EnsureDateAllowedAsync(voucher.Date);

            var username = _currentUserService.Username;

            if (voucher.VoucherType is VoucherType.Receipt or VoucherType.DebtReceipt)
            {
                if (voucher.InstallmentId.HasValue)
                    await ReverseInstallmentApplicationAsync(context, voucher, username);
                else if (voucher.InvoiceId.HasValue)
                    await ReverseCreditInvoiceApplicationAsync(context, voucher, username);
            }

            switch (voucher.VoucherType)
            {
                case VoucherType.Receipt:
                case VoucherType.DebtReceipt:
                    await ValidateAndDeductCashBox(context, voucher.CashBoxId, voucher.Amount, username);
                    break;

                case VoucherType.Payment:
                    await AdjustCashBoxBalance(context, voucher.CashBoxId, voucher.Amount, username);
                    break;

                case VoucherType.BankReceipt:
                    if (!voucher.BankAccountId.HasValue)
                        throw new InvalidOperationException("السند المصرفي بدون مصرف مرتبط");
                    var netAmount = voucher.Amount - voucher.BankFees;
                    await ValidateAndDeductCashBox(context, voucher.CashBoxId, netAmount, username);
                    await AdjustBankBalance(context, voucher.BankAccountId.Value, voucher.Amount, username);
                    break;

                case VoucherType.InvestorDeposit:
                    if (!voucher.InvestorId.HasValue)
                        throw new InvalidOperationException("سند الإيداع بدون مستثمر مرتبط");
                    await ValidateAndDeductCashBox(context, voucher.CashBoxId, voucher.Amount, username);
                    await AdjustInvestorDeposit(context, voucher.InvestorId.Value, -voucher.Amount, username);
                    await CreateInvestorTransaction(context, voucher.InvestorId.Value,
                        InvestorTransactionType.Withdrawal, voucher.Amount,
                        $"عكس حذف سند {voucher.VoucherNumber}", username);
                    break;

                case VoucherType.InvestorWithdrawal:
                    if (!voucher.InvestorId.HasValue)
                        throw new InvalidOperationException("سند السحب بدون مستثمر مرتبط");
                    await AdjustCashBoxBalance(context, voucher.CashBoxId, voucher.Amount, username);
                    await AdjustInvestorDeposit(context, voucher.InvestorId.Value, voucher.Amount, username);
                    await CreateInvestorTransaction(context, voucher.InvestorId.Value,
                        InvestorTransactionType.Deposit, voucher.Amount,
                        $"عكس حذف سند {voucher.VoucherNumber}", username);
                    break;
            }

            voucher.MarkSoftDeleted(username);
            await context.SaveChangesAsync();

            if (_currentUserService.UserId.HasValue)
            {
                await context.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = _currentUserService.UserId.Value,
                    Action = AuditAction.Delete,
                    EntityName = "Voucher",
                    EntityId = voucher.Id,
                    OldValues = $"سند {GetVoucherTypeName(voucher.VoucherType)}: {voucher.VoucherNumber}, المبلغ: {voucher.Amount:N0}",
                    Timestamp = DateTime.UtcNow,
                    CreatedBy = username,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<string> GetNextVoucherNumberAsync(VoucherType type)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await GetNextVoucherNumberAsync(context, type);
    }

    private static async Task<string> GetNextVoucherNumberAsync(AppDbContext context, VoucherType type)
    {
        var prefix = type switch
        {
            VoucherType.Receipt => "RCV",
            VoucherType.Payment => "PAY",
            VoucherType.BankReceipt => "BRV",
            VoucherType.InvestorDeposit => "IDP",
            VoucherType.InvestorWithdrawal => "IWD",
            VoucherType.DebtReceipt => "DRC",
            _ => "VCH"
        };

        // Include soft-deleted rows so numbers still held by the unique index are not reused.
        var numbers = await context.Vouchers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(v => v.VoucherType == type && v.VoucherNumber.StartsWith(prefix + "-"))
            .Select(v => v.VoucherNumber)
            .ToListAsync();

        var max = 0;
        foreach (var num in numbers)
        {
            var parts = num.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var seq) && seq > max)
                max = seq;
        }

        return $"{prefix}-{(max + 1):D4}";
    }

    public async Task<(IEnumerable<Voucher> Items, int TotalCount)> GetPagedVouchersAsync(
        int page, int pageSize, VoucherType? type = null, DateTime? fromDate = null,
        DateTime? toDate = null, string? searchTerm = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Vouchers
            .Include(v => v.Customer)
            .Include(v => v.Investor)
            .Include(v => v.CashBox)
            .Include(v => v.BankAccount)
            .Include(v => v.Invoice)
            .AsQueryable();

        if (type.HasValue) query = query.Where(v => v.VoucherType == type.Value);
        if (fromDate.HasValue) query = query.Where(v => v.Date >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(v => v.Date <= toDate.Value.AddDays(1));
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(v =>
                v.VoucherNumber.Contains(searchTerm) ||
                (v.Customer != null && v.Customer.Name.Contains(searchTerm)) ||
                (v.Investor != null && v.Investor.Name.Contains(searchTerm)) ||
                (v.Notes != null && v.Notes.Contains(searchTerm)));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(v => v.Date)
            .ThenByDescending(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IEnumerable<Voucher>> GetVouchersByCashBoxAsync(int cashBoxId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Vouchers
            .Include(v => v.Customer).Include(v => v.Investor)
            .Where(v => v.CashBoxId == cashBoxId)
            .OrderByDescending(v => v.Date).ThenByDescending(v => v.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transfer>> GetTransfersByCashBoxAsync(int cashBoxId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Transfers
            .Where(t => (t.FromType == TransferAccountType.CashBox && t.FromId == cashBoxId) ||
                        (t.ToType == TransferAccountType.CashBox && t.ToId == cashBoxId))
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<Voucher>> GetVouchersByBankAsync(int bankAccountId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Vouchers
            .Include(v => v.Customer).Include(v => v.Investor)
            .Where(v => v.BankAccountId == bankAccountId)
            .OrderByDescending(v => v.Date).ThenByDescending(v => v.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transfer>> GetTransfersByBankAsync(int bankAccountId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Transfers
            .Where(t => (t.FromType == TransferAccountType.Bank && t.FromId == bankAccountId) ||
                        (t.ToType == TransferAccountType.Bank && t.ToId == bankAccountId))
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
            .ToListAsync();
    }

    // ══════════════════════════════════════════════════════
    // Private helpers (now take context parameter)
    // ══════════════════════════════════════════════════════
    private static async Task AdjustCashBoxBalance(AppDbContext context, int cashBoxId, decimal amount, string username)
    {
        var cashBox = await context.CashBoxes.FindAsync(cashBoxId)
            ?? throw new InvalidOperationException("القاصة غير موجودة");
        cashBox.Balance += amount;
        cashBox.UpdatedBy = username;
        cashBox.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task ValidateAndDeductCashBox(AppDbContext context, int cashBoxId, decimal amount, string username)
    {
        var cashBox = await context.CashBoxes.FindAsync(cashBoxId)
            ?? throw new InvalidOperationException("القاصة غير موجودة");
        if (cashBox.Balance < amount)
            throw new InvalidOperationException($"رصيد القاصة ({cashBox.Balance:N0}) غير كافٍ ({amount:N0})");
        cashBox.Balance -= amount;
        cashBox.UpdatedBy = username;
        cashBox.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task ValidateAndDeductBank(AppDbContext context, int bankAccountId, decimal amount, string username)
    {
        var bank = await context.BankAccounts.FindAsync(bankAccountId)
            ?? throw new InvalidOperationException("المصرف غير موجود");
        if (bank.Balance < amount)
            throw new InvalidOperationException($"رصيد المصرف ({bank.Balance:N0}) غير كافٍ ({amount:N0})");
        bank.Balance -= amount;
        bank.UpdatedBy = username;
        bank.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task AdjustBankBalance(AppDbContext context, int bankAccountId, decimal amount, string username)
    {
        var bank = await context.BankAccounts.FindAsync(bankAccountId)
            ?? throw new InvalidOperationException("المصرف غير موجود");
        bank.Balance += amount;
        bank.UpdatedBy = username;
        bank.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task AdjustInvestorDeposit(AppDbContext context, int investorId, decimal amount, string username)
    {
        var investor = await context.Investors.FindAsync(investorId)
            ?? throw new InvalidOperationException("المستثمر غير موجود");
        investor.TotalDeposit += amount;
        investor.UpdatedBy = username;
        investor.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task CreateInvestorTransaction(AppDbContext context, int investorId, InvestorTransactionType type,
        decimal amount, string? notes, string username)
    {
        var tx = new InvestorTransaction
        {
            InvestorId = investorId, Type = type, Amount = amount,
            Date = DateTime.Now, Notes = notes,
            CreatedBy = username, CreatedAt = DateTime.UtcNow
        };
        await context.InvestorTransactions.AddAsync(tx);
    }

    private static async Task ApplyDebtReceiptToCreditInvoicesAsync(
        AppDbContext context, Voucher voucher, string username)
    {
        if (CustomerBalanceHelper.IsDebtReceiptApplied(voucher.Notes) || !voucher.CustomerId.HasValue)
            return;

        var creditInvoices = await context.Invoices
            .Where(i => i.CustomerId == voucher.CustomerId.Value &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.RemainingAmount > 0)
            .OrderBy(i => i.Date)
            .ThenBy(i => i.Id)
            .ToListAsync();

        var snapshot = creditInvoices
            .Select(i => (i.Id, i.Date, i.NetAmount, i.PaidAmount, i.RemainingAmount))
            .ToList();
        var updates = CustomerBalanceHelper.AllocateToCreditInvoices(snapshot, voucher.Amount);
        foreach (var u in updates)
        {
            var inv = creditInvoices.First(i => i.Id == u.Id);
            inv.PaidAmount = u.PaidAmount;
            inv.RemainingAmount = u.RemainingAmount;
            inv.IsCreditPaid = u.IsCreditPaid;
            inv.UpdatedAt = DateTime.UtcNow;
            inv.UpdatedBy = username;
        }

        voucher.Notes = CustomerBalanceHelper.MarkDebtReceiptApplied(voucher.Notes);
    }

    private static async Task ApplyAmountToCreditInvoiceAsync(
        AppDbContext context, Voucher voucher, string username)
    {
        var invoice = await context.Invoices.FirstOrDefaultAsync(i => i.Id == voucher.InvoiceId)
            ?? throw new InvalidOperationException("الفاتورة المرتبطة غير موجودة");

        if (invoice.PaymentMethod != PaymentMethod.Credit)
            throw new InvalidOperationException("يمكن ربط سند القبض بفاتورة آجلة فقط");

        if (voucher.CustomerId.HasValue && invoice.CustomerId != voucher.CustomerId)
            throw new InvalidOperationException("الفاتورة لا تخص العميل المحدد");

        if (invoice.RemainingAmount <= 0)
            throw new InvalidOperationException("الفاتورة مسددة بالكامل");

        var apply = Math.Min(voucher.Amount, invoice.RemainingAmount);
        invoice.PaidAmount += apply;
        invoice.RemainingAmount = Math.Max(0, invoice.NetAmount - invoice.PaidAmount);
        invoice.IsCreditPaid = invoice.RemainingAmount <= 0;
        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.UpdatedBy = username;

        voucher.CustomerId ??= invoice.CustomerId;
        voucher.Notes = CustomerBalanceHelper.MarkDebtReceiptApplied(voucher.Notes);
    }

    private static async Task ApplyAmountToInstallmentAsync(
        AppDbContext context, Voucher voucher, string username, bool adjustCash)
    {
        var installment = await context.Installments
            .Include(i => i.InstallmentPlan)
            .FirstOrDefaultAsync(i => i.Id == voucher.InstallmentId)
            ?? throw new InvalidOperationException("القسط المرتبط غير موجود");

        if (voucher.CustomerId.HasValue &&
            installment.InstallmentPlan?.CustomerId != voucher.CustomerId)
            throw new InvalidOperationException("القسط لا يخص العميل المحدد");

        if (installment.RemainingAmount <= 0)
            throw new InvalidOperationException("القسط مسدد بالكامل");

        var apply = Math.Min(voucher.Amount, installment.RemainingAmount);
        installment.PaidAmount += apply;
        installment.RemainingAmount = installment.Amount - installment.PaidAmount;
        installment.CashBoxId = voucher.CashBoxId;
        installment.PaymentDate = voucher.Date;
        installment.UpdatedBy = username;
        installment.UpdatedAt = DateTime.UtcNow;
        installment.Status = installment.RemainingAmount <= 0
            ? InstallmentStatus.Paid
            : InstallmentStatus.PartiallyPaid;

        voucher.CustomerId ??= installment.InstallmentPlan?.CustomerId;
        voucher.InvoiceId ??= installment.InstallmentPlan?.InvoiceId;

        if (adjustCash)
            await AdjustCashBoxBalance(context, voucher.CashBoxId, apply, username);
    }

    private static async Task ReverseCreditInvoiceApplicationAsync(
        AppDbContext context, Voucher voucher, string username)
    {
        var invoice = await context.Invoices.FirstOrDefaultAsync(i => i.Id == voucher.InvoiceId);
        if (invoice is null) return;

        var reverse = Math.Min(voucher.Amount, invoice.PaidAmount);
        invoice.PaidAmount = Math.Max(0, invoice.PaidAmount - reverse);
        invoice.RemainingAmount = Math.Max(0, invoice.NetAmount - invoice.PaidAmount);
        invoice.IsCreditPaid = invoice.RemainingAmount <= 0;
        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.UpdatedBy = username;
    }

    private static async Task ReverseInstallmentApplicationAsync(
        AppDbContext context, Voucher voucher, string username)
    {
        var installment = await context.Installments.FirstOrDefaultAsync(i => i.Id == voucher.InstallmentId);
        if (installment is null) return;

        var reverse = Math.Min(voucher.Amount, installment.PaidAmount);
        installment.PaidAmount = Math.Max(0, installment.PaidAmount - reverse);
        installment.RemainingAmount = installment.Amount - installment.PaidAmount;
        installment.UpdatedBy = username;
        installment.UpdatedAt = DateTime.UtcNow;
        installment.Status = installment.PaidAmount <= 0
            ? (installment.DueDate.Date < DateTime.Today ? InstallmentStatus.Overdue : InstallmentStatus.Pending)
            : InstallmentStatus.PartiallyPaid;
        if (installment.PaidAmount <= 0)
            installment.PaymentDate = null;
    }

    public async Task SetVoucherReconciledAsync(int voucherId, bool isReconciled)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var voucher = await context.Vouchers.FirstOrDefaultAsync(v => v.Id == voucherId)
            ?? throw new InvalidOperationException("السند غير موجود");

        if (!voucher.BankAccountId.HasValue)
            throw new InvalidOperationException("التسوية البنكية متاحة للسندات المصرفية فقط");

        var username = _currentUserService.Username;
        voucher.IsReconciled = isReconciled;
        voucher.ReconciledAt = isReconciled ? DateTime.UtcNow : null;
        voucher.ReconciledBy = isReconciled ? username : null;
        voucher.UpdatedAt = DateTime.UtcNow;
        voucher.UpdatedBy = username;
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Invoice>> GetOpenCreditInvoicesForCustomerAsync(int customerId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Invoices.AsNoTracking()
            .Where(i => i.CustomerId == customerId &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.RemainingAmount > 0)
            .OrderBy(i => i.Date)
            .ThenBy(i => i.Id)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Installment>> GetOpenInstallmentsForCustomerAsync(int customerId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Installments.AsNoTracking()
            .Include(i => i.InstallmentPlan)
            .Where(i => i.InstallmentPlan!.CustomerId == customerId && i.RemainingAmount > 0)
            .OrderBy(i => i.DueDate)
            .ThenBy(i => i.Id)
            .ToListAsync();
    }

    private static string GetVoucherTypeName(VoucherType type) => type switch
    {
        VoucherType.Receipt => "قبض",
        VoucherType.Payment => "صرف",
        VoucherType.BankReceipt => "قبض مصرفي",
        VoucherType.InvestorDeposit => "إيداع مستثمر",
        VoucherType.InvestorWithdrawal => "سحب مستثمر",
        VoucherType.DebtReceipt => "قبض دين",
        _ => "سند"
    };
}
