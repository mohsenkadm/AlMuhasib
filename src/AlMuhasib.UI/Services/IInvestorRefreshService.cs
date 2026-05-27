namespace AlMuhasib.UI.Services;

public interface IInvestorRefreshService
{
    event EventHandler? InvestorsChanged;

    void NotifyChanged();
}
