using System.Collections.ObjectModel;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class SalesInvoiceViewModel
{
    private readonly IInvoiceTemplateService _templateService;

    [ObservableProperty] private bool _isTemplatePickerOpen;
    [ObservableProperty] private bool _isSaveTemplateDialogOpen;
    [ObservableProperty] private string _newTemplateName = string.Empty;

    public ObservableCollection<InvoiceTemplate> SavedTemplates { get; } = [];

    [RelayCommand]
    private void OpenTemplatePicker()
    {
        if (IsSaved)
        {
            BeautifulMessageDialog.ShowWarning("افتح فاتورة جديدة لاستخدام القوالب");
            return;
        }

        RefreshSavedTemplates();
        IsTemplatePickerOpen = true;
    }

    [RelayCommand]
    private void CloseTemplatePicker() => IsTemplatePickerOpen = false;

    [RelayCommand]
    private void OpenSaveTemplateDialog()
    {
        if (IsSaved)
        {
            BeautifulMessageDialog.ShowWarning("لا يمكن حفظ فاتورة محفوظة كقالب");
            return;
        }

        if (!Items.Any(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0))
        {
            BeautifulMessageDialog.ShowWarning("أضف بنوداً للفاتورة قبل حفظ القالب");
            return;
        }

        NewTemplateName = $"قالب {DateTime.Now:yyyy/MM/dd HH:mm}";
        IsSaveTemplateDialogOpen = true;
    }

    [RelayCommand]
    private void CloseSaveTemplateDialog()
    {
        IsSaveTemplateDialogOpen = false;
        NewTemplateName = string.Empty;
    }

    [RelayCommand]
    private void ConfirmSaveTemplate()
    {
        var name = NewTemplateName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            BeautifulMessageDialog.ShowWarning("أدخل اسماً للقالب");
            return;
        }

        var template = BuildTemplateFromCurrent(name);
        _templateService.SaveTemplate(template);
        RefreshSavedTemplates();
        IsSaveTemplateDialogOpen = false;
        NewTemplateName = string.Empty;
        BeautifulMessageDialog.ShowSuccess($"تم حفظ القالب «{name}»");
    }

    [RelayCommand]
    private void LoadTemplate(InvoiceTemplate? template)
    {
        if (template is null || IsSaved) return;
        ApplyTemplate(template);
        IsTemplatePickerOpen = false;
        var fieldsHint = ActiveCustomFieldLabels.Count > 0
            ? $" — الحقول: {string.Join("، ", ActiveCustomFieldLabels)}"
            : string.Empty;
        BeautifulMessageDialog.ShowSuccess($"تم تحميل القالب «{template.Name}»{fieldsHint}");
    }

    [RelayCommand]
    private void DeleteTemplate(InvoiceTemplate? template)
    {
        if (template is null) return;
        if (!BeautifulMessageDialog.ShowConfirm($"حذف القالب «{template.Name}»؟")) return;

        _templateService.DeleteTemplate(template.Id);
        RefreshSavedTemplates();
        BeautifulMessageDialog.ShowSuccess("تم حذف القالب");
    }

    [RelayCommand]
    private void SetDefaultCustomerFromInvoice()
    {
        if (SelectedCustomer is null)
        {
            BeautifulMessageDialog.ShowWarning("اختر عميلاً أولاً");
            return;
        }

        _templateService.SetDefaultSalesCustomer(SelectedCustomer.Id);
        BeautifulMessageDialog.ShowSuccess($"تم تعيين «{SelectedCustomer.Name}» كعميل افتراضي");
    }

    private void RefreshSavedTemplates()
    {
        SavedTemplates.Clear();
        foreach (var t in _templateService.GetTemplates(InvoiceTemplateKind.Sale))
            SavedTemplates.Add(t);
    }

    private InvoiceTemplate BuildTemplateFromCurrent(string name) => new()
    {
        Name = name,
        Kind = InvoiceTemplateKind.Sale,
        CustomerId = SelectedCustomer?.Id,
        WarehouseId = SelectedWarehouse?.Id,
        PaymentMethod = SelectedPaymentMethod,
        CreditDueDate = CreditDueDate,
        CashBoxId = SelectedCashBox?.Id,
        Notes = Notes,
        Lines = Items
            .Where(i => !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0)
            .Select(i => new InvoiceTemplateLine
            {
                ProductId = i.ProductId ?? 0,
                ProductName = i.ItemName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            })
            .ToList()
    };

    private void ApplyTemplate(InvoiceTemplate template)
    {
        if (template.CustomerId.HasValue)
        {
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == template.CustomerId);
            if (SelectedCustomer is not null)
                CustomerSearchText = SelectedCustomer.Name;
        }

        if (template.WarehouseId.HasValue)
            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == template.WarehouseId);

        SelectedPaymentMethod = template.PaymentMethod;
        CreditDueDate = template.CreditDueDate ?? DateTime.Today.AddMonths(1);

        if (template.CashBoxId.HasValue)
            SelectedCashBox = CashBoxes.FirstOrDefault(c => c.Id == template.CashBoxId);

        Notes = template.Notes ?? string.Empty;

<<<<<<< Current (Your changes)
        ApplyCustomFieldLabels(template.CustomFieldLabels);
=======
        ApplyCustomFieldLabels(template.CustomFieldLabels, template.IndustryTag);
>>>>>>> Incoming (Background Agent changes)

        foreach (var row in Items.ToList())
            UnwireItemRow(row);
        Items.Clear();

        foreach (var line in template.Lines)
        {
            var row = new InvoiceItemRow
            {
                ProductId = line.ProductId > 0 ? line.ProductId : null,
                ItemName = line.ProductName,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice
            };
            ApplyActiveLabelsToRow(row);
            WireItemRow(row);
            Items.Add(row);
            OnProductChanged(row);
        }

        if (!Items.Any())
            AddRow();

        RecalculateTotals();
        ScheduleDraftSave();
    }

    private void ApplyDefaultCustomerIfAny()
    {
        var id = _templateService.GetDefaultSalesCustomerId();
        if (!id.HasValue) return;

        var customer = Customers.FirstOrDefault(c => c.Id == id.Value);
        if (customer is null) return;

        SelectedCustomer = customer;
        CustomerSearchText = customer.Name;
    }
}
