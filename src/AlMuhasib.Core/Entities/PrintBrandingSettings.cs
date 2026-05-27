namespace AlMuhasib.Core.Entities;

/// <summary>Singleton print header/footer branding (Id = 1).</summary>
public class PrintBrandingSettings : BaseEntity
{
    public const int SingletonId = 1;

    public string CompanyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhonePrimary { get; set; } = string.Empty;
    public string PhoneSecondary { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;

    public bool ShowHeaderText { get; set; } = true;
    public bool ShowHeaderImage { get; set; }
    public byte[]? HeaderImageData { get; set; }
    public string? HeaderImageContentType { get; set; }

    public bool ShowFooterText { get; set; } = true;
    public string FooterText { get; set; } = string.Empty;
    public bool ShowFooterImage { get; set; }
    public byte[]? FooterImageData { get; set; }
    public string? FooterImageContentType { get; set; }
}
