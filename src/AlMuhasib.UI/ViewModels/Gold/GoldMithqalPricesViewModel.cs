namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldMithqalPricesViewModel : ViewModelBase
{
    public GoldMithqalPricesViewModel()
    {
        PageTitle = "أسعار المثقال";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
