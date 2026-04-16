using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Entities;

/// <summary>رأس المال</summary>
public class CapitalEntry : BaseEntity
{
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public CapitalEntryType Type { get; set; }
    public string? Notes { get; set; }
}
