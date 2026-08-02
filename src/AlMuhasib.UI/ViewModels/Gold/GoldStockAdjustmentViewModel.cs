namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldStockAdjustmentViewModel : ViewModelBase
{
    public GoldStockAdjustmentViewModel()
    {
        PageTitle = "تسوية مخزون";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
