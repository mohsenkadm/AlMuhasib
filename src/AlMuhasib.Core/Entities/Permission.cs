namespace AlMuhasib.Core.Entities;

/// <summary>الصلاحيات</summary>
public class Permission : BaseEntity
{
    public int UserId { get; set; }
    public string ScreenName { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanPrint { get; set; }
    public bool CanExport { get; set; }
    public bool CanEditPrice { get; set; } = true;
    public bool IsViewOnly { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
