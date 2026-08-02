namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldPurchaseInvoiceViewModel : ViewModelBase
{
    public GoldPurchaseInvoiceViewModel()
    {
        PageTitle = "فاتورة شراء";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
