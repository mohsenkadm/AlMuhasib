namespace AlMuhasib.Core.Enums.Customs;

/// <summary>
/// Transport invoice subtype: internal (inside Iraq) vs external (cross-border).
/// Only meaningful when <see cref="CustomsServiceType"/> is Transport.
/// </summary>
public enum CustomsTransportKind
{
    Internal = 0,
    External = 1
}
