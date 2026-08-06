namespace AlMuhasib.Core.Interfaces.Services;

public interface IPartyQuickDetailService
{
    Task<PartyQuickDetailResult?> GetCustomerDetailAsync(int customerId, CancellationToken cancellationToken = default);
    Task<PartyQuickDetailResult?> GetSupplierDetailAsync(int supplierId, CancellationToken cancellationToken = default);
}

public class PartyQuickDetailResult
{
    public PersonPartyType PartyType { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TypeLabel { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? FileNumber { get; set; }
    public string? Notes { get; set; }

    /// <summary>المبلغ المطلوب / الرصيد المستحق.</summary>
    public decimal Balance { get; set; }
    public decimal TotalDealAmount { get; set; }
    public int DealCount { get; set; }

    public DateTime? LastDealDate { get; set; }
    public string? LastDealDescription { get; set; }
    public decimal? LastDealAmount { get; set; }

    public List<PartyQuickProductRow> Products { get; set; } = [];
    public List<PartyQuickTimelineRow> RecentTimeline { get; set; } = [];
}

public class PartyQuickProductRow
{
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public int DealCount { get; set; }
    public DateTime? LastDate { get; set; }
    public decimal? LastUnitPrice { get; set; }
}

public class PartyQuickTimelineRow
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}
