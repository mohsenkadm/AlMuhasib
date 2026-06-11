using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class InstallmentInvoiceViewModel
{
    [RelayCommand(CanExecute = nameof(CanPrintSavedInvoice))]
    private void PrintInstallmentContract()
    {
        if (_savedInvoice is null) return;
        var path = _exportService.ExportInstallmentContractToPdf(BuildSavedInvoicePrintModel());
        BeautifulMessageDialog.ShowSuccess($"تم حفظ عقد التقسيط:\n{path}");
    }

    [RelayCommand(CanExecute = nameof(CanPrintSavedInvoice))]
    private void PrintInstallmentScheduleDoc()
    {
        if (_savedInvoice is null) return;
        _exportService.PrintInstallmentSchedule(BuildSavedInvoicePrintModel());
    }
}
