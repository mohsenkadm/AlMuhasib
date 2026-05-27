namespace AlMuhasib.UI.Services;

/// <summary>
/// View models that expose investor dropdowns should implement this
/// so lists refresh after investors are added or updated elsewhere.
/// </summary>
public interface IInvestorLookupHost
{
    Task RefreshInvestorsAsync();
}
