namespace AlMuhasib.UI.Services;

public sealed class InvestorRefreshService : IInvestorRefreshService
{
    public event EventHandler? InvestorsChanged;

    public void NotifyChanged() => InvestorsChanged?.Invoke(this, EventArgs.Empty);
}
