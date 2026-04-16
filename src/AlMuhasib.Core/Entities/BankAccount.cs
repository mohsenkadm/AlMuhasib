namespace AlMuhasib.Core.Entities;

/// <summary>المصرف</summary>
public class BankAccount : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public decimal Balance { get; set; }
}
