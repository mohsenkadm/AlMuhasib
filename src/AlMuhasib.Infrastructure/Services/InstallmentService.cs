using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Models;
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
        decimal totalAmount, int numberOfInstallments, DateTime startDate,
        InstallmentType installmentType = InstallmentType.Manual)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var username = _currentUserService.Username;
            var (companyFeePercentage, companyFeeAmount) =
                CompanyFeeHelper.ResolveForInstallment(totalAmount, installmentType);

            var invoice = await context.Invoices.FindAsync(invoiceId);
            if (invoice is not null)
            {
                invoice.CompanyFeePercentage = companyFeePercentage;
                invoice.CompanyFeeAmount = companyFeeAmount;
                invoice.UpdatedBy = username;
                invoice.UpdatedAt = DateTime.UtcNow;
            }
            var plan = new InstallmentPlan
            {
                InvoiceId = invoiceId, CustomerId = customerId, FileNumber = fileNumber,
                TotalAmount = totalAmount, NumberOfInstallments = numberOfInstallments,
                InstallmentAmount = Math.Floor(totalAmount / numberOfInstallments),
                StartDate = startDate, InstallmentType = installmentType,
                CompanyFeePercentage = companyFeePercentage,
                CompanyFeeAmount = companyFeeAmount,
                CreatedBy = username, CreatedAt = DateTime.UtcNow
            };
            await context.InstallmentPlans.AddAsync(plan);
            await context.SaveChangesAsync();

            for (int i = 0; i < numberOfInstallments; i++)
            {
                decimal amount = (i < numberOfInstallments - 1)
                    ? plan.InstallmentAmount
                    : totalAmount - (plan.InstallmentAmount * (numberOfInstallments - 1));
                await context.Installments.AddAsync(new Installment
                {
                    InstallmentPlanId = plan.Id, DueDate = startDate.AddMonths(i),
                    Amount = amount, PaidAmount = 0, RemainingAmount = amount,
                    Status = InstallmentStatus.Pending, CreatedBy = username, CreatedAt = DateTime.UtcNow
                });
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

    public async Task<BulkPayInstallmentsResult> PayInstallmentsBatchAsync(IReadOnlyList<int> installmentIds, int cashBoxId)
    {
        if (installmentIds is null || installmentIds.Count == 0)
            throw new InvalidOperationException("لم يتم اختيار أي قسط للتسديد");

        var distinctIds = installmentIds.Distinct().ToList();
        var errors = new List<string>();
        var paidCount = 0;
        decimal totalPaid = 0;

        foreach (var id in distinctIds)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var installment = await context.Installments
                    .Include(i => i.InstallmentPlan)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (installment is null)
                {
                    errors.Add($"القسط رقم {id} غير موجود");
                    continue;
                }

                if (installment.RemainingAmount <= 0)
                {
                    errors.Add($"القسط رقم {id} مسدد مسبقاً أو لا يوجد مبلغ متبقي");
                    continue;
                }

                var amount = installment.RemainingAmount;
                await PayInstallmentAsync(id, amount, cashBoxId);
                paidCount++;
                totalPaid += amount;
            }
            catch (Exception ex)
            {
                errors.Add($"قسط {id}: {ex.Message}");
            }
        }

        return new BulkPayInstallmentsResult
        {
            PaidCount = paidCount,
            TotalPaid = totalPaid,
            Errors = errors
        };
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
        DateTime? fromDate = null, DateTime? toDate = null, InstallmentType? installmentType = null)
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
        if (toDate.HasValue) query = query.Where(p => p.StartDate < toDate.Value.Date.AddDays(1));
        if (installmentType.HasValue) query = query.Where(p => p.InstallmentType == installmentType.Value);

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

    public async Task<InstallmentPlan> CreateOpeningBalancePlanAsync(OpeningInstallmentBalanceRequest request)
    {
        ValidateOpeningBalanceRequest(request);

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var username = _currentUserService.Username;
            var customerId = await ResolveCustomerIdAsync(context, request, username);
            var warehouse = await context.Warehouses.OrderBy(w => w.Id).FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("يجب إنشاء مخزن واحد على الأقل قبل إدخال الأرصدة الافتتاحية");

            var invoiceNumber = await GenerateInstallmentInvoiceNumberAsync(context);
            var notePrefix = "رصيد افتتاحي — أقساط سابقة";
            var notes = string.IsNullOrWhiteSpace(request.Notes)
                ? notePrefix
                : $"{notePrefix} | {request.Notes.Trim()}";

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNumber,
                InvoiceType = InvoiceType.Installment,
                CustomerId = customerId,
                WarehouseId = warehouse.Id,
                PaymentMethod = PaymentMethod.Installment,
                TotalAmount = request.TotalAmount,
                DiscountAmount = 0,
                NetAmount = request.TotalAmount,
                RoundingAmount = 0,
                RoundingType = RoundingType.RoundDown,
                PaidAmount = request.TotalAmount,
                RemainingAmount = 0,
                IsCreditPaid = true,
                Date = request.StartDate.Date,
                Notes = notes,
                CreatedBy = username,
                CreatedAt = DateTime.UtcNow
            };
            await context.Invoices.AddAsync(invoice);
            await context.SaveChangesAsync();

            var installmentAmount = Math.Floor(request.TotalAmount / request.NumberOfInstallments);
            var plan = new InstallmentPlan
            {
                InvoiceId = invoice.Id,
                CustomerId = customerId,
                FileNumber = string.IsNullOrWhiteSpace(request.FileNumber) ? null : request.FileNumber.Trim(),
                TotalAmount = request.TotalAmount,
                NumberOfInstallments = request.NumberOfInstallments,
                InstallmentAmount = installmentAmount,
                StartDate = request.StartDate.Date,
                InstallmentType = InstallmentType.OpeningBalance,
                CompanyFeePercentage = 0,
                CompanyFeeAmount = 0,
                CreatedBy = username,
                CreatedAt = DateTime.UtcNow
            };
            await context.InstallmentPlans.AddAsync(plan);
            await context.SaveChangesAsync();

            var today = DateTime.Today;
            for (var i = 0; i < request.NumberOfInstallments; i++)
            {
                var amount = i < request.NumberOfInstallments - 1
                    ? installmentAmount
                    : request.TotalAmount - (installmentAmount * (request.NumberOfInstallments - 1));
                var dueDate = request.StartDate.Date.AddMonths(i);
                var isPaid = i < request.PaidInstallmentsCount;

                await context.Installments.AddAsync(new Installment
                {
                    InstallmentPlanId = plan.Id,
                    DueDate = dueDate,
                    Amount = amount,
                    PaidAmount = isPaid ? amount : 0,
                    RemainingAmount = isPaid ? 0 : amount,
                    Status = isPaid
                        ? InstallmentStatus.Paid
                        : dueDate < today ? InstallmentStatus.Overdue : InstallmentStatus.Pending,
                    PaymentDate = isPaid ? dueDate : null,
                    CashBoxId = null,
                    CreatedBy = username,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();

            if (_currentUserService.UserId.HasValue)
            {
                await context.AuditLogs.AddAsync(new AuditLog
                {
                    UserId = _currentUserService.UserId.Value,
                    Action = AuditAction.Add,
                    EntityName = "InstallmentPlan",
                    EntityId = plan.Id,
                    NewValues = $"رصيد افتتاحي: {request.NumberOfInstallments} قسط ({request.PaidInstallmentsCount} مسدد), المبلغ: {request.TotalAmount:N0}, العميل: {customerId}",
                    Timestamp = DateTime.UtcNow,
                    CreatedBy = username,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return plan;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<OpeningInstallmentBatchResult> CreateOpeningBalancePlansBatchAsync(
        IReadOnlyList<OpeningInstallmentBalanceRequest> requests)
    {
        var result = new OpeningInstallmentBatchResult();
        if (requests is null || requests.Count == 0)
        {
            result.Errors.Add("لا توجد بيانات للاستيراد");
            return result;
        }

        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index];
            try
            {
                ValidateOpeningBalanceRequest(request);
                await CreateOpeningBalancePlanAsync(request);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                var label = request.CustomerName ?? request.CustomerId?.ToString() ?? $"سطر {index + 1}";
                result.Errors.Add($"{label}: {ex.Message}");
            }
        }

        return result;
    }

    private static void ValidateOpeningBalanceRequest(OpeningInstallmentBalanceRequest request)
    {
        if (request.TotalAmount <= 0)
            throw new InvalidOperationException("المبلغ الكلي يجب أن يكون أكبر من صفر");
        if (request.NumberOfInstallments <= 0)
            throw new InvalidOperationException("عدد الأقساط يجب أن يكون أكبر من صفر");
        if (request.PaidInstallmentsCount < 0)
            throw new InvalidOperationException("عدد الأقساط المسددة لا يمكن أن يكون سالباً");
        if (request.PaidInstallmentsCount > request.NumberOfInstallments)
            throw new InvalidOperationException("عدد الأقساط المسددة لا يمكن أن يتجاوز إجمالي الأقساط");
        if (request.CustomerId is null && string.IsNullOrWhiteSpace(request.CustomerName))
            throw new InvalidOperationException("يجب اختيار زبون أو إدخال اسمه");
    }

    private static async Task<int> ResolveCustomerIdAsync(
        AppDbContext context, OpeningInstallmentBalanceRequest request, string username)
    {
        if (request.CustomerId is int existingId)
        {
            var exists = await context.Customers.AnyAsync(c => c.Id == existingId);
            if (!exists)
                throw new InvalidOperationException("الزبون المحدد غير موجود");
            return existingId;
        }

        var name = request.CustomerName!.Trim();
        var matched = await context.Customers
            .FirstOrDefaultAsync(c => c.Name == name);
        if (matched is not null)
            return matched.Id;

        var newCustomer = new Customer
        {
            Name = name,
            FileNumber = string.IsNullOrWhiteSpace(request.FileNumber) ? null : request.FileNumber.Trim(),
            CreatedBy = username,
            CreatedAt = DateTime.UtcNow
        };
        await context.Customers.AddAsync(newCustomer);
        await context.SaveChangesAsync();
        return newCustomer.Id;
    }

    private static Task<string> GenerateInstallmentInvoiceNumberAsync(AppDbContext context)
        => InvoiceNumberHelper.GenerateNextAsync(context, InvoiceType.Installment);
}
