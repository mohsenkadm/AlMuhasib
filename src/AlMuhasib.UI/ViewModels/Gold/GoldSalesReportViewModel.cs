namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldSalesReportViewModel : ViewModelBase
{
    public GoldSalesReportViewModel()
    {
        PageTitle = "تقرير المبيعات";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
