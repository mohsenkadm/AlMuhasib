namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCashBoxesViewModel : ViewModelBase
{
    public GoldCashBoxesViewModel()
    {
        PageTitle = "القاصات";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
