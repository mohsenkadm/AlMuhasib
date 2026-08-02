namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldSaleInvoiceViewModel : ViewModelBase
{
    public GoldSaleInvoiceViewModel()
    {
        PageTitle = "فاتورة بيع";
    }

    public override Task InitializeAsync() => Task.CompletedTask;
}
