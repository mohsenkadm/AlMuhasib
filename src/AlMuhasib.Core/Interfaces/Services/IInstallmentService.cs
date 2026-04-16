using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IInstallmentService
{
    Task<InstallmentPlan> CreatePlanAsync(int invoiceId, int customerId, string? fileNumber,
        decimal totalAmount, int numberOfInstallments, DateTime startDate);
    Task<InstallmentPlan?> GetPlanByIdAsync(int id);
    Task<IEnumerable<InstallmentPlan>> GetPlansByCustomerAsync(int customerId);
    Task PayInstallmentAsync(int installmentId, decimal amount, int cashBoxId);
    Task CancelPaymentAsync(int installmentId);
    Task<IEnumerable<Installment>> GetOverdueInstallmentsAsync();
    Task UpdateOverdueStatusesAsync();
    Task<IEnumerable<Installment>> GetInstallmentsByStatusAsync(InstallmentStatus status);

    Task<(IEnumerable<InstallmentPlan> Items, int TotalCount)> GetPagedPlansAsync(
        int page, int pageSize, string? searchTerm = null, InstallmentStatus? statusFilter = null,
        DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<Installment>> GetInstallmentsByPlanIdAsync(int planId);
    Task<(IEnumerable<Installment> Items, int TotalCount)> GetPagedInstallmentsAsync(
        int page, int pageSize, InstallmentStatus? status = null, int? customerId = null,
        string? searchTerm = null);
}
