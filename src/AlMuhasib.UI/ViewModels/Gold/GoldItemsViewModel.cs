namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldItemsViewModel : ViewModelBase
{
    public GoldItemsViewModel()
    {
        PageTitle = "أصناف الذهب";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
