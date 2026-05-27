namespace AlMuhasib.Core.Models;

/// <summary>Immutable snapshot used when building print documents.</summary>
public sealed class PrintBrandingSnapshot
{
    public static PrintBrandingSnapshot Empty { get; } = new();

    public string CompanyName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string PhonePrimary { get; init; } = string.Empty;
    public string PhoneSecondary { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;

    public bool ShowHeaderText { get; init; } = true;
    public bool ShowHeaderImage { get; init; }
    public byte[]? HeaderImageData { get; init; }

    public bool ShowFooterText { get; init; } = true;
    public string FooterText { get; init; } = string.Empty;
    public bool ShowFooterImage { get; init; }
    public byte[]? FooterImageData { get; init; }

    public bool HasHeaderContent =>
        (ShowHeaderText && HasHeaderTextContent) || (ShowHeaderImage && HeaderImageData is { Length: > 0 });

    public bool HasFooterContent =>
        HasFooterContactLines
        || (ShowFooterText && !string.IsNullOrWhiteSpace(FooterText))
        || (ShowFooterImage && FooterImageData is { Length: > 0 });

    private bool HasHeaderTextContent =>
        !string.IsNullOrWhiteSpace(CompanyName)
        || !string.IsNullOrWhiteSpace(Email)
        || !string.IsNullOrWhiteSpace(Details);

    private bool HasFooterContactLines =>
        !string.IsNullOrWhiteSpace(Address)
        || !string.IsNullOrWhiteSpace(PhonePrimary)
        || !string.IsNullOrWhiteSpace(PhoneSecondary);
}
