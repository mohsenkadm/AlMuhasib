using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.UI.ViewModels;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Services;

public sealed class VoiceCommandExecutor
{
    public async Task<VoiceCommandResult> ExecuteAsync(VoiceCommandDefinition command, MainWindowViewModel host)
    {
        switch (command.ActionType)
        {
            case VoiceCommandActionType.CloseAssistant:
                return new VoiceCommandResult { Succeeded = true, CloseAssistant = true };

            case VoiceCommandActionType.ShowHelp:
                return new VoiceCommandResult
                {
                    Succeeded = true,
                    Message = "اختر أمراً من الاقتراحات أو قل: فاتورة مبيعات، المنتجات، بحث"
                };

            case VoiceCommandActionType.OpenGlobalSearch:
                host.OpenGlobalSearchCommand.Execute(null);
                return new VoiceCommandResult
                {
                    Succeeded = true,
                    Message = command.SuccessMessage ?? "تم فتح البحث",
                    CloseAssistant = true
                };

            case VoiceCommandActionType.QuickNewSale:
                await host.QuickNewSaleCommand.ExecuteAsync(null);
                return new VoiceCommandResult
                {
                    Succeeded = true,
                    Message = command.SuccessMessage ?? "تم فتح فاتورة المبيعات",
                    CloseAssistant = true
                };

            case VoiceCommandActionType.QuickPosSale:
                if (command.ViewModelType is not null)
                {
                    await host.OpenTabAsync(command.ViewModelType, command.TabTitle ?? "بيع سريع", PackIconKind.PointOfSale);
                }
                else
                {
                    await host.QuickPosSaleCommand.ExecuteAsync(null);
                }

                return new VoiceCommandResult
                {
                    Succeeded = true,
                    Message = command.SuccessMessage ?? "تم فتح بيع سريع",
                    CloseAssistant = true
                };

            case VoiceCommandActionType.OpenScreen:
                if (command.ViewModelType is null)
                    return new VoiceCommandResult { Succeeded = false, Message = "الشاشة غير معرّفة" };

                if (!host.TryAuthorizeScreen(command.ViewModelType, out _))
                {
                    return new VoiceCommandResult
                    {
                        Succeeded = false,
                        Message = "ليس لديك صلاحية لهذه الشاشة"
                    };
                }

                var icon = ResolveIcon(command.ViewModelType);
                await host.OpenTabAsync(command.ViewModelType, command.TabTitle ?? command.DisplayLabel, icon);
                return new VoiceCommandResult
                {
                    Succeeded = true,
                    Message = command.SuccessMessage ?? $"تم فتح {command.DisplayLabel}",
                    CloseAssistant = true
                };

            default:
                return new VoiceCommandResult { Succeeded = false, Message = "أمر غير مدعوم" };
        }
    }

    private static PackIconKind ResolveIcon(Type viewModelType) =>
        viewModelType.Name switch
        {
            nameof(DashboardViewModel) => PackIconKind.ViewDashboard,
            nameof(ProductsViewModel) => PackIconKind.PackageVariantClosed,
            nameof(CustomersViewModel) => PackIconKind.AccountGroup,
            nameof(PersonProfileViewModel) => PackIconKind.AccountDetails,
            nameof(SuppliersViewModel) => PackIconKind.TruckDelivery,
            nameof(SalesInvoiceViewModel) => PackIconKind.CashRegister,
            nameof(PurchaseInvoiceViewModel) => PackIconKind.CartArrowDown,
            nameof(InstallmentInvoiceViewModel) => PackIconKind.CalendarClock,
            nameof(InstallmentsViewModel) => PackIconKind.CalendarClock,
            nameof(VouchersViewModel) => PackIconKind.FileDocument,
            nameof(ExpenseViewModel) => PackIconKind.CashMinus,
            nameof(CashBankViewModel) => PackIconKind.Bank,
            nameof(WarehousesViewModel) => PackIconKind.Warehouse,
            nameof(SalesReportViewModel) => PackIconKind.ChartLine,
            nameof(BackupRestoreViewModel) => PackIconKind.BackupRestore,
            nameof(UsersViewModel) => PackIconKind.AccountCog,
            nameof(PosQuickSaleViewModel) => PackIconKind.PointOfSale,
            _ => PackIconKind.Application
        };
}
