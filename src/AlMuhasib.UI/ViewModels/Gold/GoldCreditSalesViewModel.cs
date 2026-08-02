namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCreditSalesViewModel : ViewModelBase
{
    public GoldCreditSalesViewModel()
    {
        PageTitle = "مبيعات الآجل";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
