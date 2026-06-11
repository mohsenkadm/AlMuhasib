using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.UI.Services;

public sealed class InvoiceTemplateService : IInvoiceTemplateService
{
    private const int MaxTemplatesPerKind = 12;
    private readonly IUserPreferencesService _preferences;

    private static readonly InvoiceTemplate[] BuiltInInstallmentTemplates =
    [
        new()
        {
            Id = "builtin-install-3-manual",
            Name = "3 أقساط — يدوي",
            Kind = InvoiceTemplateKind.Installment,
            InstallmentType = InstallmentType.Manual,
            NumberOfInstallments = 3,
            InstallmentStartMonthsOffset = 1,
            IsBuiltIn = true,
            SavedAt = DateTime.MinValue
        },
        new()
        {
            Id = "builtin-install-6-manual",
            Name = "6 أقساط — يدوي",
            Kind = InvoiceTemplateKind.Installment,
            InstallmentType = InstallmentType.Manual,
            NumberOfInstallments = 6,
            InstallmentStartMonthsOffset = 1,
            IsBuiltIn = true,
            SavedAt = DateTime.MinValue
        },
        new()
        {
            Id = "builtin-install-12-manual",
            Name = "12 قسط — يدوي",
            Kind = InvoiceTemplateKind.Installment,
            InstallmentType = InstallmentType.Manual,
            NumberOfInstallments = 12,
            InstallmentStartMonthsOffset = 1,
            IsBuiltIn = true,
            SavedAt = DateTime.MinValue
        },
        new()
        {
            Id = "builtin-install-6-platform",
            Name = "6 أقساط — منصة (8%)",
            Kind = InvoiceTemplateKind.Installment,
            InstallmentType = InstallmentType.Platform,
            NumberOfInstallments = 6,
            InstallmentStartMonthsOffset = 1,
            IsBuiltIn = true,
            SavedAt = DateTime.MinValue
        },
        new()
        {
            Id = "builtin-install-12-platform",
            Name = "12 قسط — منصة (8%)",
            Kind = InvoiceTemplateKind.Installment,
            InstallmentType = InstallmentType.Platform,
            NumberOfInstallments = 12,
            InstallmentStartMonthsOffset = 1,
            IsBuiltIn = true,
            SavedAt = DateTime.MinValue
        }
    ];

    public InvoiceTemplateService(IUserPreferencesService preferences) => _preferences = preferences;

    public IReadOnlyList<InvoiceTemplate> GetTemplates(InvoiceTemplateKind kind)
    {
        if (kind == InvoiceTemplateKind.Installment)
            EnsureBuiltInInstallmentTemplates();
        if (kind == InvoiceTemplateKind.Sale)
            EnsureBuiltInIndustryTemplates();

        var flags = _preferences.Current.FeatureFlags;
        return _preferences.Current.InvoiceTemplates
            .Where(t => t.Kind == kind)
            .Where(t => !t.IsBuiltIn || IsIndustryTemplateEnabled(t, flags))
            .OrderByDescending(t => t.IsBuiltIn)
            .ThenByDescending(t => t.SavedAt)
            .ToList();
    }

    private static bool IsIndustryTemplateEnabled(InvoiceTemplate t, BusinessFeatureFlags flags) => t.IndustryTag switch
    {
        "mobile" => flags.TemplateMobileShop,
        "clothing" => flags.TemplateClothing,
        "construction" => flags.TemplateConstruction,
        "pharmacy" => flags.TemplatePharmacy,
        _ => true
    };

    private void EnsureBuiltInIndustryTemplates()
    {
        var builtIn = new[]
        {
            new InvoiceTemplate { Id = "builtin-mobile", Name = "موبايلات", Kind = InvoiceTemplateKind.Sale, IndustryTag = "mobile", CustomFieldLabels = ["IMEI", "اللون"], IsBuiltIn = true, Notes = "قالب محل موبايلات" },
            new InvoiceTemplate { Id = "builtin-clothing", Name = "ألبسة", Kind = InvoiceTemplateKind.Sale, IndustryTag = "clothing", CustomFieldLabels = ["المقاس", "اللون"], IsBuiltIn = true, Notes = "قالب محل ألبسة" },
            new InvoiceTemplate { Id = "builtin-construction", Name = "مواد بناء", Kind = InvoiceTemplateKind.Sale, IndustryTag = "construction", CustomFieldLabels = ["الوحدة", "المواصفات"], IsBuiltIn = true, Notes = "قالب مواد بناء" },
            new InvoiceTemplate { Id = "builtin-pharmacy", Name = "صيدلية", Kind = InvoiceTemplateKind.Sale, IndustryTag = "pharmacy", CustomFieldLabels = ["تاريخ الانتهاء", "رقم الدفعة"], IsBuiltIn = true, Notes = "قالب صيدلية" }
        };
        var list = _preferences.Current.InvoiceTemplates.ToList();
        var changed = false;
        foreach (var t in builtIn)
        {
            if (list.Any(x => x.Id == t.Id)) continue;
            list.Add(CloneTemplate(t));
            changed = true;
        }
        if (changed)
            _preferences.Update(p => p.InvoiceTemplates = list);
    }

    public void SaveTemplate(InvoiceTemplate template)
    {
        template.IsBuiltIn = false;
        var list = _preferences.Current.InvoiceTemplates.ToList();
        var existing = list.FirstOrDefault(t => t.Id == template.Id);
        if (existing is not null)
            list.Remove(existing);

        template.SavedAt = DateTime.Now;
        list.Insert(0, template);

        var sameKind = list.Where(t => t.Kind == template.Kind && !t.IsBuiltIn).ToList();
        if (sameKind.Count > MaxTemplatesPerKind)
        {
            foreach (var old in sameKind.Skip(MaxTemplatesPerKind))
                list.Remove(old);
        }

        _preferences.Update(p => p.InvoiceTemplates = list);
    }

    public void DeleteTemplate(string templateId)
    {
        var target = _preferences.Current.InvoiceTemplates.FirstOrDefault(t => t.Id == templateId);
        if (target?.IsBuiltIn == true)
            return;

        var list = _preferences.Current.InvoiceTemplates
            .Where(t => t.Id != templateId)
            .ToList();
        _preferences.Update(p => p.InvoiceTemplates = list);
    }

    public void SetDefaultSalesCustomer(int? customerId) =>
        _preferences.Update(p => p.DefaultSalesCustomerId = customerId);

    public void SetDefaultPurchaseSupplier(int? supplierId) =>
        _preferences.Update(p => p.DefaultPurchaseSupplierId = supplierId);

    public void SetDefaultInstallmentCustomer(int? customerId) =>
        _preferences.Update(p => p.DefaultInstallmentCustomerId = customerId);

    public int? GetDefaultSalesCustomerId() => _preferences.Current.DefaultSalesCustomerId;

    public int? GetDefaultPurchaseSupplierId() => _preferences.Current.DefaultPurchaseSupplierId;

    public int? GetDefaultInstallmentCustomerId() => _preferences.Current.DefaultInstallmentCustomerId;

    private void EnsureBuiltInInstallmentTemplates()
    {
        var list = _preferences.Current.InvoiceTemplates.ToList();
        var changed = false;

        foreach (var builtIn in BuiltInInstallmentTemplates)
        {
            if (list.Any(t => t.Id == builtIn.Id))
                continue;

            list.Add(CloneTemplate(builtIn));
            changed = true;
        }

        if (changed)
            _preferences.Update(p => p.InvoiceTemplates = list);
    }

    private static InvoiceTemplate CloneTemplate(InvoiceTemplate source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Kind = source.Kind,
        CustomerId = source.CustomerId,
        SupplierId = source.SupplierId,
        WarehouseId = source.WarehouseId,
        PaymentMethod = source.PaymentMethod,
        CreditDueDate = source.CreditDueDate,
        CashBoxId = source.CashBoxId,
        Notes = source.Notes,
        Lines = source.Lines.Select(l => new InvoiceTemplateLine
        {
            ProductId = l.ProductId,
            ProductName = l.ProductName,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice
        }).ToList(),
        InstallmentType = source.InstallmentType,
        NumberOfInstallments = source.NumberOfInstallments,
        InstallmentStartMonthsOffset = source.InstallmentStartMonthsOffset,
        FileNumber = source.FileNumber,
        IndustryTag = source.IndustryTag,
        CustomFieldLabels = source.CustomFieldLabels.ToList(),
        IsBuiltIn = source.IsBuiltIn,
        SavedAt = source.SavedAt
    };
}
