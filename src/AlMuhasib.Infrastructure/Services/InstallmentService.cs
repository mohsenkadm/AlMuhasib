using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Helpers;
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

    public async Task<CustomerAmountPayResult> PayCustomerAmountOldestFirstAsync(
        int customerId, decimal amount, int cashBoxId, string? notes = null)
    {
        if (amount <= 0)
            throw new InvalidOperationException("مبلغ التسديد يجب أن يكون أكبر من صفر");

        await using var context = await _contextFactory.CreateDbContextAsync();
        var unpaid = await context.Installments
            .Include(i => i.InstallmentPlan)
            .Where(i => i.InstallmentPlan.CustomerId == customerId && i.RemainingAmount > 0)
            .OrderBy(i => i.DueDate)
            .ThenBy(i => i.Id)
            .ToListAsync();

        if (unpaid.Count == 0)
            throw new InvalidOperationException("لا توجد أقساط مستحقة لهذا العميل");

        var remainingToApply = amount;
        var applied = 0m;
        var touched = 0;

        foreach (var installment in unpaid)
        {
            if (remainingToApply <= 0)
                break;

            var pay = Math.Min(remainingToApply, installment.RemainingAmount);
            if (pay <= 0)
                continue;

            await PayInstallmentAsync(installment.Id, pay, cashBoxId);
            remainingToApply -= pay;
            applied += pay;
            touched++;
        }

        var message = notes;
        if (remainingToApply > 0)
            message = string.IsNullOrWhiteSpace(message)
                ? $"تبقّى {remainingToApply:N2} بدون أقساط كافية"
                : $"{message} | تبقّى {remainingToApply:N2} بدون أقساط كافية";

        return new CustomerAmountPayResult
        {
            AmountApplied = applied,
            AmountRemaining = remainingToApply,
            InstallmentsTouched = touched,
            Message = message
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
        var username = _currentUserService.Username;
        var now = DateTime.UtcNow;

        await context.Installments
            .Where(i => (i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.PartiallyPaid)
                        && i.DueDate < today)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.Status, InstallmentStatus.Overdue)
                .SetProperty(i => i.UpdatedBy, username)
                .SetProperty(i => i.UpdatedAt, now));
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
                (p.Customer.Phone != null && p.Customer.Phone.Contains(term)) ||
                (p.Customer.FileNumber != null && p.Customer.FileNumber.Contains(term)) ||
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
        int page, int pageSize, InstallmentStatus? status = null, int? customerId = null, string? searchTerm = null,
        IReadOnlyCollection<InstallmentStatus>? statuses = null, bool updateOverdueStatuses = true,
        bool includeCashBox = true)
    {
        if (updateOverdueStatuses)
            await UpdateOverdueStatusesAsync();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = BuildInstallmentsQuery(context, status, customerId, searchTerm, statuses);

        var totalCount = await query.CountAsync();
        var safePageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 500);
        var safePage = page <= 0 ? 1 : page;

        // جلب المعرفات أولاً ثم التحميل مع العلاقات — أسرع مع Include على جداول كبيرة
        var pageIds = await query
            .OrderBy(i => i.DueDate)
            .ThenBy(i => i.Id)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(i => i.Id)
            .ToListAsync();

        if (pageIds.Count == 0)
            return (Array.Empty<Installment>(), totalCount);

        IQueryable<Installment> itemsQuery = context.Installments
            .AsNoTracking()
            .Where(i => pageIds.Contains(i.Id))
            .Include(i => i.InstallmentPlan).ThenInclude(p => p!.Customer);

        if (includeCashBox)
            itemsQuery = itemsQuery.Include(i => i.CashBox);

        var items = await itemsQuery
            .AsSplitQuery()
            .ToListAsync();

        var order = pageIds.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        items.Sort((a, b) => order[a.Id].CompareTo(order[b.Id]));
        return (items, totalCount);
    }

    public async Task<(int Count, decimal TotalAmount, decimal PaidAmount, decimal RemainingAmount)> GetInstallmentTotalsAsync(
        InstallmentStatus? status = null, int? customerId = null, string? searchTerm = null,
        IReadOnlyCollection<InstallmentStatus>? statuses = null, bool updateOverdueStatuses = false)
    {
        if (updateOverdueStatuses)
            await UpdateOverdueStatusesAsync();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = BuildInstallmentsQuery(context, status, customerId, searchTerm, statuses);

        var agg = await query
            .Select(i => new { i.Amount, i.PaidAmount, i.RemainingAmount })
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                PaidAmount = g.Sum(x => x.PaidAmount),
                RemainingAmount = g.Sum(x => x.RemainingAmount)
            })
            .FirstOrDefaultAsync();

        return agg is null
            ? (0, 0m, 0m, 0m)
            : (agg.Count, agg.TotalAmount, agg.PaidAmount, agg.RemainingAmount);
    }

    private static IQueryable<Installment> BuildInstallmentsQuery(
        AppDbContext context,
        InstallmentStatus? status,
        int? customerId,
        string? searchTerm,
        IReadOnlyCollection<InstallmentStatus>? statuses)
    {
        var query = context.Installments.AsNoTracking().AsQueryable();

        if (statuses is { Count: > 0 })
        {
            var statusList = statuses as InstallmentStatus[] ?? statuses.ToArray();
            query = query.Where(i => statusList.Contains(i.Status));
        }
        else if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        if (customerId.HasValue)
            query = query.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(i => i.InstallmentPlan.Customer.Name.Contains(term) ||
                (i.InstallmentPlan.Customer.Phone != null && i.InstallmentPlan.Customer.Phone.Contains(term)) ||
                (i.InstallmentPlan.Customer.FileNumber != null && i.InstallmentPlan.Customer.FileNumber.Contains(term)) ||
                (i.InstallmentPlan.FileNumber != null && i.InstallmentPlan.FileNumber.Contains(term)));
        }

        return query;
    }

    public Task<InstallmentPlan> CreateOpeningBalancePlanAsync(OpeningInstallmentBalanceRequest request)
        => CreateOpeningBalancePlanCoreAsync(request, nameCache: null);

    public async Task<OpeningInstallmentBatchResult> CreateOpeningBalancePlansBatchAsync(
        IReadOnlyList<OpeningInstallmentBalanceRequest> requests)
    {
        var result = new OpeningInstallmentBatchResult();
        if (requests is null || requests.Count == 0)
        {
            result.Errors.Add("لا توجد بيانات للاستيراد");
            return result;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var customers = await context.Customers
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();
        var nameCache = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var c in customers)
        {
            var key = ArabicNameNormalizer.Compact(c.Name);
            if (key.Length > 0 && !nameCache.ContainsKey(key))
                nameCache[key] = c.Id;
        }

        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index];
            try
            {
                ValidateOpeningBalanceRequest(request);
                await CreateOpeningBalancePlanCoreAsync(request, nameCache);
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

    private async Task<InstallmentPlan> CreateOpeningBalancePlanCoreAsync(
        OpeningInstallmentBalanceRequest request,
        Dictionary<string, int>? nameCache)
    {
        ValidateOpeningBalanceRequest(request);

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var username = _currentUserService.Username;
            var customerId = await ResolveCustomerIdAsync(context, request, username, nameCache);
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
        AppDbContext context,
        OpeningInstallmentBalanceRequest request,
        string username,
        Dictionary<string, int>? nameCache)
    {
        if (request.CustomerId is int existingId)
        {
            var exists = await context.Customers.AnyAsync(c => c.Id == existingId);
            if (!exists)
                throw new InvalidOperationException("الزبون المحدد غير موجود");
            return existingId;
        }

        var name = request.CustomerName!.Trim();
        var compact = ArabicNameNormalizer.Compact(name);

        if (nameCache is not null && compact.Length > 0 && nameCache.TryGetValue(compact, out var cachedId))
            return cachedId;

        var exact = await context.Customers.FirstOrDefaultAsync(c => c.Name == name);
        if (exact is not null)
        {
            if (nameCache is not null)
            {
                var exactKey = ArabicNameNormalizer.Compact(exact.Name);
                if (exactKey.Length > 0)
                    nameCache[exactKey] = exact.Id;
            }
            return exact.Id;
        }

        if (compact.Length > 0 && nameCache is null)
        {
            var candidates = await context.Customers.Select(c => new { c.Id, c.Name }).ToListAsync();
            var match = candidates.FirstOrDefault(c => ArabicNameNormalizer.Compact(c.Name) == compact);
            if (match is not null)
                return match.Id;
        }

        var newCustomer = new Customer
        {
            Name = name,
            FileNumber = string.IsNullOrWhiteSpace(request.FileNumber) ? null : request.FileNumber.Trim(),
            CreatedBy = username,
            CreatedAt = DateTime.UtcNow
        };
        await context.Customers.AddAsync(newCustomer);
        await context.SaveChangesAsync();

        if (nameCache is not null && compact.Length > 0)
            nameCache[compact] = newCustomer.Id;

        return newCustomer.Id;
    }

    private static Task<string> GenerateInstallmentInvoiceNumberAsync(AppDbContext context)
        => InvoiceNumberHelper.GenerateNextAsync(context, InvoiceType.Installment);
}
