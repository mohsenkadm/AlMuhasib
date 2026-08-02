namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldFxRatesViewModel : ViewModelBase
{
    public GoldFxRatesViewModel()
    {
        PageTitle = "أسعار الصرف";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
