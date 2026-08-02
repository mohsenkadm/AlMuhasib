namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldSettingsViewModel : ViewModelBase
{
    public GoldSettingsViewModel()
    {
        PageTitle = "إعدادات الذهب";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
