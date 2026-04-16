using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class InstallmentService : IInstallmentService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public InstallmentService(IDbContextFactory<AppDbContext> contextFactory, ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<InstallmentPlan> CreatePlanAsync(int invoiceId, int customerId, string? fileNumber,
        decimal totalAmount, int numberOfInstallments, DateTime startDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var username = _currentUserService.Username;
            var plan = new InstallmentPlan
            {
                InvoiceId = invoiceId, CustomerId = customerId, FileNumber = fileNumber,
                TotalAmount = totalAmount, NumberOfInstallments = numberOfInstallments,
                InstallmentAmount = Math.Ceiling(totalAmount / numberOfInstallments),
                StartDate = startDate, CreatedBy = username, CreatedAt = DateTime.UtcNow
            };
            await context.InstallmentPlans.AddAsync(plan);
            await context.SaveChangesAsync();

            decimal remaining = totalAmount;
            for (int i = 0; i < numberOfInstallments; i++)
            {
                decimal amount = (i < numberOfInstallments - 1) ? plan.InstallmentAmount : remaining;
                await context.Installments.AddAsync(new Installment
                {
                    InstallmentPlanId = plan.Id, DueDate = startDate.AddMonths(i),
                    Amount = amount, PaidAmount = 0, RemainingAmount = amount,
                    Status = InstallmentStatus.Pending, CreatedBy = username, CreatedAt = DateTime.UtcNow
                });
                remaining -= amount;
            }
            await context.SaveChangesAsync();

            if (_currentUserService.UserId.HasValue)
            {
                await context.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = _currentUserService.UserId.Value, Action = AuditAction.Add,
                    EntityName = "InstallmentPlan", EntityId = plan.Id,
                    NewValues = $"خطة أقساط: {numberOfInstallments} قسط, المبلغ: {totalAmount:N0}, العميل: {customerId}",
                    Timestamp = DateTime.UtcNow, CreatedBy = username, CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return plan;
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task<InstallmentPlan?> GetPlanByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.InstallmentPlans
            .Include(p => p.Customer).Include(p => p.Invoice)
            .Include(p => p.Installments.OrderBy(i => i.DueDate))
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<InstallmentPlan>> GetPlansByCustomerAsync(int customerId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.InstallmentPlans
            .Include(p => p.Customer).Include(p => p.Invoice)
            .Include(p => p.Installments.OrderBy(i => i.DueDate))
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task PayInstallmentAsync(int installmentId, decimal amount, int cashBoxId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var username = _currentUserService.Username;
            var installment = await context.Installments
                .Include(i => i.InstallmentPlan)
                .FirstOrDefaultAsync(i => i.Id == installmentId)
                ?? throw new InvalidOperationException("القسط غير موجود");

            if (amount <= 0) throw new InvalidOperationException("مبلغ الدفع يجب أن يكون أكبر من صفر");
            if (amount > installment.RemainingAmount)
                throw new InvalidOperationException($"مبلغ الدفع ({amount:N0}) أكبر من المتبقي ({installment.RemainingAmount:N0})");

            installment.PaidAmount += amount;
            installment.RemainingAmount = installment.Amount - installment.PaidAmount;
            installment.CashBoxId = cashBoxId;
            installment.PaymentDate = DateTime.Now;
            installment.UpdatedBy = username;
            installment.UpdatedAt = DateTime.UtcNow;
            installment.Status = installment.RemainingAmount <= 0 ? InstallmentStatus.Paid : InstallmentStatus.PartiallyPaid;

            var cashBox = await context.CashBoxes.FindAsync(cashBoxId);
            if (cashBox is not null)
            {
                cashBox.Balance += amount;
                cashBox.UpdatedBy = username;
                cashBox.UpdatedAt = DateTime.UtcNow;
            }
            await context.SaveChangesAsync();

            if (_currentUserService.UserId.HasValue)
            {
                await context.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = _currentUserService.UserId.Value, Action = AuditAction.Edit,
                    EntityName = "Installment", EntityId = installment.Id,
                    NewValues = $"تسديد قسط: {amount:N0} د.ع, المتبقي: {installment.RemainingAmount:N0}",
                    Timestamp = DateTime.UtcNow, CreatedBy = username, CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
            await transaction.CommitAsync();
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task CancelPaymentAsync(int installmentId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var username = _currentUserService.Username;
            var installment = await context.Installments
                .Include(i => i.InstallmentPlan)
                .FirstOrDefaultAsync(i => i.Id == installmentId)
                ?? throw new InvalidOperationException("القسط غير موجود");

            if (installment.PaidAmount <= 0)
                throw new InvalidOperationException("لا يوجد مبلغ مدفوع لإلغائه");

            var refundAmount = installment.PaidAmount;

            // Refund from cash box
            if (installment.CashBoxId.HasValue)
            {
                var cashBox = await context.CashBoxes.FindAsync(installment.CashBoxId.Value);
                if (cashBox is not null)
                {
                    cashBox.Balance -= refundAmount;
                    cashBox.UpdatedBy = username;
                    cashBox.UpdatedAt = DateTime.UtcNow;
                }
            }

            installment.PaidAmount = 0;
            installment.RemainingAmount = installment.Amount;
            installment.CashBoxId = null;
            installment.PaymentDate = null;
            installment.UpdatedBy = username;
            installment.UpdatedAt = DateTime.UtcNow;
            installment.Status = installment.DueDate < DateTime.Today
                ? InstallmentStatus.Overdue
                : InstallmentStatus.Pending;

            await context.SaveChangesAsync();

            if (_currentUserService.UserId.HasValue)
            {
                await context.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = _currentUserService.UserId.Value, Action = AuditAction.Edit,
                    EntityName = "Installment", EntityId = installment.Id,
                    NewValues = $"إلغاء تسديد قسط: {refundAmount:N0} د.ع",
                    Timestamp = DateTime.UtcNow, CreatedBy = username, CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
            await transaction.CommitAsync();
        }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public async Task<IEnumerable<Installment>> GetOverdueInstallmentsAsync()
    {
        await UpdateOverdueStatusesAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Include(i => i.CashBox)
            .Where(i => i.Status == InstallmentStatus.Overdue)
            .OrderBy(i => i.DueDate).ToListAsync();
    }

    public async Task UpdateOverdueStatusesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var today = DateTime.Today;
        var pendingOverdue = await context.Installments
            .Where(i => (i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.PartiallyPaid) && i.DueDate < today)
            .ToListAsync();
        if (pendingOverdue.Count == 0) return;

        var username = _currentUserService.Username;
        foreach (var inst in pendingOverdue)
        {
            inst.Status = InstallmentStatus.Overdue;
            inst.UpdatedBy = username;
            inst.UpdatedAt = DateTime.UtcNow;
        }
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Installment>> GetInstallmentsByStatusAsync(InstallmentStatus status)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Include(i => i.CashBox)
            .Where(i => i.Status == status).OrderBy(i => i.DueDate).ToListAsync();
    }

    public async Task<(IEnumerable<InstallmentPlan> Items, int TotalCount)> GetPagedPlansAsync(
        int page, int pageSize, string? searchTerm = null, InstallmentStatus? statusFilter = null,
        DateTime? fromDate = null, DateTime? toDate = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.InstallmentPlans
            .Include(p => p.Customer).Include(p => p.Invoice).Include(p => p.Installments).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(p => p.Customer.Name.Contains(term) ||
                (p.FileNumber != null && p.FileNumber.Contains(term)) || p.Invoice.InvoiceNumber.Contains(term));
        }
        if (statusFilter.HasValue) query = query.Where(p => p.Installments.Any(i => i.Status == statusFilter.Value));
        if (fromDate.HasValue) query = query.Where(p => p.StartDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(p => p.StartDate <= toDate.Value);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<IEnumerable<Installment>> GetInstallmentsByPlanIdAsync(int planId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Installments.Include(i => i.CashBox)
            .Where(i => i.InstallmentPlanId == planId).OrderBy(i => i.DueDate).ToListAsync();
    }

    public async Task<(IEnumerable<Installment> Items, int TotalCount)> GetPagedInstallmentsAsync(
        int page, int pageSize, InstallmentStatus? status = null, int? customerId = null, string? searchTerm = null)
    {
        await UpdateOverdueStatusesAsync();
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Include(i => i.CashBox).AsQueryable();

        if (status.HasValue) query = query.Where(i => i.Status == status.Value);
        if (customerId.HasValue) query = query.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(i => i.InstallmentPlan.Customer.Name.Contains(term) ||
                (i.InstallmentPlan.FileNumber != null && i.InstallmentPlan.FileNumber.Contains(term)));
        }

        var totalCount = await query.CountAsync();
        var items = await query.OrderBy(i => i.DueDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }
}
