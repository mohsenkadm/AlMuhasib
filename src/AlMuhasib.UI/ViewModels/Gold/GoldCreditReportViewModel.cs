namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCreditReportViewModel : ViewModelBase
{
    public GoldCreditReportViewModel()
    {
        PageTitle = "تقرير الآجل";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
