namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldDashboardViewModel : ViewModelBase
{
    public GoldDashboardViewModel()
    {
        PageTitle = "لوحة التحكم";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
