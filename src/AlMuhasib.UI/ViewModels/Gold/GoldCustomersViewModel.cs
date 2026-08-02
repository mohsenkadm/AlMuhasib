namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCustomersViewModel : ViewModelBase
{
    public GoldCustomersViewModel()
    {
        PageTitle = "الزبائن";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
