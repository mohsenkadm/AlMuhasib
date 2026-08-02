namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldNotificationsViewModel : ViewModelBase
{
    public GoldNotificationsViewModel()
    {
        PageTitle = "التنبيهات";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
