using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class InstallmentsViewModel
{
    [RelayCommand]
    private async Task ViewPlanDetailsAsync(InstallmentPlan? plan)
    {
        if (plan is null || plan.InvoiceId <= 0) return;

        try
        {
            IsBusy = true;
            var invoice = await _invoiceService.GetByIdWithDetailsAsync(plan.InvoiceId);
            if (invoice is null)
            {
                BeautifulMessageDialog.ShowWarning("تعذر تحميل تفاصيل الفاتورة");
                return;
            }

            var companyFee = plan.CompanyFeeAmount > 0 ? plan.CompanyFeeAmount : (decimal?)null;
            InvoiceDetailDialog.Show(invoice, "أقساط", companyFee);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر عرض التفاصيل:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PrintPlanInvoiceAsync(InstallmentPlan? plan)
    {
        if (plan is null || plan.InvoiceId <= 0) return;

        try
        {
            IsBusy = true;
            var invoice = await _invoiceService.GetByIdWithDetailsAsync(plan.InvoiceId);
            if (invoice is null)
            {
                BeautifulMessageDialog.ShowWarning("تعذر تحميل الفاتورة للطباعة");
                return;
            }

            _exportService.PrintInvoice(BuildInstallmentPrintModel(invoice, plan));
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر الطباعة:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EditPlanInvoiceAsync(InstallmentPlan? plan)
    {
        if (plan is null || plan.InvoiceId <= 0) return;

        if (InvoiceNavigationBridge.EditInstallmentInvoiceAsync is null)
        {
            BeautifulMessageDialog.ShowWarning("تعذر فتح شاشة فاتورة الأقساط");
            return;
        }

        try
        {
            await InvoiceNavigationBridge.EditInstallmentInvoiceAsync(plan.InvoiceId);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر فتح الفاتورة للتعديل:\n{ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeletePlanInvoiceAsync(InstallmentPlan? plan)
    {
        if (plan is null || plan.InvoiceId <= 0) return;

        var invoiceNumber = plan.Invoice?.InvoiceNumber ?? plan.InvoiceId.ToString();
        var hasPaidInstallments = plan.Installments?.Any(i => i.PaidAmount > 0) == true;

        var message = hasPaidInstallments
            ? $"هل تريد حذف فاتورة الأقساط {invoiceNumber}؟\nتحذير: توجد أقساط مسدّدة مرتبطة بهذه الخطة."
            : $"هل تريد حذف فاتورة الأقساط {invoiceNumber}؟\nسيتم حذف الفاتورة وخطة الأقساط وجميع بنودها.";

        if (!BeautifulMessageDialog.ShowConfirm(message))
            return;

        try
        {
            IsBusy = true;
            await _invoiceService.DeleteInvoiceAsync(plan.InvoiceId);
            BeautifulMessageDialog.ShowSuccess("تم حذف فاتورة الأقساط بنجاح");
            await LoadAllPlansAsync();
            await RefreshSummaryAsync();
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر حذف الفاتورة:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static InvoicePrintModel BuildInstallmentPrintModel(Invoice invoice, InstallmentPlan plan)
    {
        var model = new InvoicePrintModel
        {
            Title = plan.InstallmentType == InstallmentType.OpeningBalance ? "رصيد افتتاحي — أقساط" : "فاتورة أقساط",
            InvoiceNumber = invoice.InvoiceNumber,
            Date = invoice.Date,
            PartyLabel = "العميل",
            PartyName = invoice.Customer?.Name ?? plan.Customer?.Name ?? "—",
            WarehouseName = invoice.Warehouse?.Name ?? "—",
            PaymentMethod = "أقساط",
            Notes = invoice.Notes,
            FileNumber = plan.FileNumber,
            Subtotal = invoice.TotalAmount,
            RoundingAmount = invoice.RoundingAmount,
            GrandTotal = invoice.NetAmount,
            CompanyFeeAmount = plan.CompanyFeeAmount > 0 ? plan.CompanyFeeAmount : null,
            NumberOfInstallments = plan.NumberOfInstallments,
            InstallmentAmount = plan.InstallmentAmount,
            Items = invoice.Items.Select((item, i) => new InvoicePrintItem
            {
                Number = i + 1,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList()
        };

        var installments = plan.Installments?.OrderBy(i => i.DueDate).ToList()
            ?? invoice.InstallmentPlans.FirstOrDefault()?.Installments.OrderBy(i => i.DueDate).ToList()
            ?? [];

        model.Schedule = installments.Select((inst, idx) => new InstallmentPrintRow
        {
            Number = idx + 1,
            DueDate = inst.DueDate,
            Amount = inst.Amount
        }).ToList();

        return model;
    }
}
