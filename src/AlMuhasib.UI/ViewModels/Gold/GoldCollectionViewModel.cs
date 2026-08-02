namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCollectionViewModel : ViewModelBase
{
    public GoldCollectionViewModel()
    {
        PageTitle = "التحصيل";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
