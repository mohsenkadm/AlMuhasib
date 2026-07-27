using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AlMuhasib.Core.Entities.RealEstate;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Services;
using AlMuhasib.Shared.Services;

namespace AlMuhasib.UI.Services;

public sealed class RealEstateContractPrintService : IRealEstateContractPrintService
{
    private static readonly CultureInfo ArabicCulture = CultureInfo.GetCultureInfo("ar-IQ");
    private static readonly Brush BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));

    public void PrintContract(RealEstateContract contract, int copies = 1)
    {
        var document = BuildFlowDocument(contract);
        DocumentPrintHelper.PrintWithPreview(document, $"عقد عقاري {contract.ContractNumber}", defaultCopies: copies);
    }

    public void PrintContracts(IEnumerable<RealEstateContract> contracts, int copiesEach = 1)
    {
        foreach (var contract in contracts)
            PrintContract(contract, copiesEach);
    }

    private static FlowDocument BuildFlowDocument(RealEstateContract contract)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 12.5,
            FlowDirection = FlowDirection.RightToLeft,
            PagePadding = new Thickness(28, 10, 28, 40)
        };

        PrintBrandingFlowDocumentHelper.PrependBrandingHeader(doc);

        doc.Blocks.Add(new Paragraph(new Run("عقد بيع / شراء عقار"))
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 12)
        });

        doc.Blocks.Add(BuildMeta(contract));
        doc.Blocks.Add(BuildParties(contract));
        doc.Blocks.Add(BuildProperty(contract));
        doc.Blocks.Add(BuildFinancials(contract));
        doc.Blocks.Add(BuildClauses(contract));
        doc.Blocks.Add(BuildSignatures(contract));

        PrintBrandingFlowDocumentHelper.AppendBrandingFooter(
            doc,
            systemLine: $"طُبع بتاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}");

        return doc;
    }

    private static Block BuildMeta(RealEstateContract c)
    {
        var p = new Paragraph { Margin = new Thickness(0, 0, 0, 10) };
        p.Inlines.Add(new Run($"رقم العقد: {c.ContractNumber}    "));
        p.Inlines.Add(new Run($"التاريخ: {c.ContractDate.ToString("yyyy/MM/dd", ArabicCulture)}    "));
        p.Inlines.Add(new Run($"النوع: {RealEstateContractService.GetContractTypeLabel(c.ContractType)}"));
        return p;
    }

    private static Block BuildParties(RealEstateContract c)
    {
        var section = new Section();
        section.Blocks.Add(new Paragraph(new Bold(new Run("الأطراف"))) { Margin = new Thickness(0, 0, 0, 4) });
        section.Blocks.Add(new Paragraph(new Run(
            $"الطرف الأول (البائع): {c.SellerName} — هاتف: {c.SellerPhone} — هوية: {c.SellerIdNumber}\nالعنوان: {c.SellerAddress}")));
        section.Blocks.Add(new Paragraph(new Run(
            $"الطرف الثاني (المشتري): {c.BuyerName} — هاتف: {c.BuyerPhone} — هوية: {c.BuyerIdNumber}\nالعنوان: {c.BuyerAddress}"))
        { Margin = new Thickness(0, 0, 0, 10) });
        return section;
    }

    private static Block BuildProperty(RealEstateContract c)
    {
        var section = new Section();
        section.Blocks.Add(new Paragraph(new Bold(new Run("العقار"))) { Margin = new Thickness(0, 0, 0, 4) });
        section.Blocks.Add(new Paragraph(new Run(
            $"النوع: {RealEstateContractService.GetPropertyTypeLabel(c.PropertyType)} — الموقع: {c.PropertyLocation} — المساحة: {c.PropertyAreaSqm:N2} م²\n" +
            $"العنوان: {c.PropertyAddress}\n{c.PropertyDescription}"))
        { Margin = new Thickness(0, 0, 0, 10) });
        return section;
    }

    private static Block BuildFinancials(RealEstateContract c)
    {
        var payment = c.PaymentMode == RealEstatePaymentMode.Credit ? "آجل" : "نقدي";
        var debtor = c.DebtorParty switch
        {
            RealEstateDebtorParty.Buyer => "المشتري",
            RealEstateDebtorParty.Seller => "البائع",
            _ => "-"
        };
        var section = new Section();
        section.Blocks.Add(new Paragraph(new Bold(new Run("الشروط المالية"))) { Margin = new Thickness(0, 0, 0, 4) });
        section.Blocks.Add(new Paragraph(new Run(
            $"السعر الكلي: {c.TotalPrice:N0} ({c.TotalPriceInWords})\n" +
            $"العربون/المدفوع: {c.AmountPaid:N0} — المتبقي: {c.RemainingAmount:N0}\n" +
            $"طريقة الدفع: {payment} — طرف المدين: {debtor}" +
            (c.DueDate.HasValue ? $" — الاستحقاق: {c.DueDate:yyyy/MM/dd}" : string.Empty)))
        { Margin = new Thickness(0, 0, 0, 10) });
        return section;
    }

    private static Block BuildClauses(RealEstateContract c)
    {
        var section = new Section();
        section.Blocks.Add(new Paragraph(new Bold(new Run("البنود"))) { Margin = new Thickness(0, 0, 0, 4) });
        var ordered = c.Clauses?.OrderBy(x => x.SortOrder).ToList() ?? [];
        if (ordered.Count == 0)
        {
            section.Blocks.Add(new Paragraph(new Run("لا توجد بنود.")) { Margin = new Thickness(0, 0, 0, 10) });
            return section;
        }

        var i = 1;
        foreach (var clause in ordered)
        {
            section.Blocks.Add(new Paragraph(new Run($"{i}. {clause.Title}: {clause.Body}"))
            {
                Margin = new Thickness(0, 0, 0, 4)
            });
            i++;
        }

        return section;
    }

    private static Block BuildSignatures(RealEstateContract c)
    {
        var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 16, 0, 0) };
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var group = new TableRowGroup();
        var row = new TableRow();
        row.Cells.Add(SigCell($"الطرف الأول\n{c.SellerName}"));
        row.Cells.Add(SigCell($"الشهود\n{c.WitnessOneName}\n{c.WitnessTwoName}"));
        row.Cells.Add(SigCell($"الطرف الثاني\n{c.BuyerName}"));
        group.Rows.Add(row);
        table.RowGroups.Add(group);
        return table;
    }

    private static TableCell SigCell(string text) => new(new Paragraph(new Run(text))
    {
        TextAlignment = TextAlignment.Center,
        Margin = new Thickness(4)
    })
    {
        BorderBrush = BorderBrush,
        BorderThickness = new Thickness(1),
        Padding = new Thickness(8)
    };
}
