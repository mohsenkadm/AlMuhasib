namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldStockViewModel : ViewModelBase
{
    public GoldStockViewModel()
    {
        PageTitle = "المخزون";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
