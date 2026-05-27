namespace AlMuhasib.Core.Entities;

/// <summary>مرفقات العميل</summary>
public class CustomerAttachment : BaseEntity
{
    public int CustomerId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation
    public Customer Customer { get; set; } = null!;
}
