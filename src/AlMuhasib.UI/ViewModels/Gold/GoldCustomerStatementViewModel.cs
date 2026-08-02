namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldCustomerStatementViewModel : ViewModelBase
{
    public GoldCustomerStatementViewModel()
    {
        PageTitle = "كشف حساب زبون";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
