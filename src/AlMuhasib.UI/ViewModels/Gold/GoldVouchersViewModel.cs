namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldVouchersViewModel : ViewModelBase
{
    public GoldVouchersViewModel()
    {
        PageTitle = "السندات";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
