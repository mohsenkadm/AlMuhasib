namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldStockReportViewModel : ViewModelBase
{
    public GoldStockReportViewModel()
    {
        PageTitle = "تقرير المخزون";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
