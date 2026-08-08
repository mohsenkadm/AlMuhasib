using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Models.SalesRep;

public sealed class SalesRepStatement
{
    public int SalesRepresentativeId { get; init; }
    public string SalesRepresentativeName { get; init; } = string.Empty;
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }

    public decimal TotalSales { get; init; }
    public decimal TotalCollections { get; init; }
    public decimal RemainingReceivables { get; init; }
    public decimal TotalCommissions { get; init; }
    public decimal PaidCommissions { get; init; }
    public decimal UnpaidCommissions { get; init; }
    public decimal CollectedByRep { get; init; }
    public decimal HandedOverByRep { get; init; }
    public decimal PendingHandover { get; init; }
    public int InvoiceCount { get; init; }
    public int CustomerCount { get; init; }

    public IReadOnlyList<SalesRepStatementLine> RecentInvoices { get; init; } = [];
    public IReadOnlyList<SalesRepCommissionRow> Commissions { get; init; } = [];
}

public sealed class SalesRepStatementLine
{
    public int InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public decimal NetAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public decimal CommissionAmount { get; init; }
}

public sealed class SalesRepCommissionRow
{
    public int Id { get; init; }
    public int InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateTime InvoiceDate { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public SalesRepCommissionType CommissionType { get; init; }
    public decimal BaseAmount { get; init; }
    public decimal CommissionAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal UnpaidAmount { get; init; }
    public SalesRepCommissionStatus Status { get; init; }
}

public sealed class SalesRepPerformanceRow
{
    public int SalesRepresentativeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Region { get; init; }
    public bool IsActive { get; init; }
    public int InvoiceCount { get; init; }
    public int CustomerCount { get; init; }
    public decimal TotalSales { get; init; }
    public decimal TotalCollections { get; init; }
    public decimal RemainingReceivables { get; init; }
    public decimal TotalCommissions { get; init; }
    public decimal UnpaidCommissions { get; init; }
    public decimal TargetAmount { get; init; }
    public decimal AchievedAmount { get; init; }
    public decimal AchievementPercent { get; init; }
}

public sealed class SalesRepTargetProgress
{
    public int TargetId { get; init; }
    public int SalesRepresentativeId { get; init; }
    public string SalesRepresentativeName { get; init; } = string.Empty;
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public decimal TargetAmount { get; init; }
    public decimal AchievedAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public decimal AchievementPercent { get; init; }
}

public sealed class SalesRepCustomerRow
{
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public decimal TotalSales { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public DateTime? LastInvoiceDate { get; init; }
    public string? LastInvoiceNumber { get; init; }
    public DateTime? LastPaymentDate { get; init; }
    public decimal LastPaymentAmount { get; init; }
}
