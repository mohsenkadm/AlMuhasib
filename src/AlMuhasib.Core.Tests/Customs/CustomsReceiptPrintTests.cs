using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Models.Customs;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

public class CustomsReceiptPrintTests
{
    [Fact]
    public async Task BuildReceipt_matches_company_voucher_layout_fields()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("شركة سيدرال");
        var invRequest = CustomsTestHarness.InvoiceRequest(
            customer.Id, AccountingCurrency.USD, 64_800m, fxRate: 1500m);
        invRequest.ServiceType = CustomsServiceType.Transport;
        invRequest.TransportKind = CustomsTransportKind.External;
        invRequest.Description = "أجور نقل";
        invRequest.Vehicles =
        [
            new CustomsInvoiceVehicleDraft { SyrianPlate = "1", VehiclePrice = 9_300m, SerialNumber = "A1" },
            new CustomsInvoiceVehicleDraft { SyrianPlate = "2", VehiclePrice = 9_300m, SerialNumber = "A2" },
            new CustomsInvoiceVehicleDraft { SyrianPlate = "3", VehiclePrice = 9_300m, SerialNumber = "A3" },
            new CustomsInvoiceVehicleDraft { SyrianPlate = "4", VehiclePrice = 9_300m, SerialNumber = "A4" },
            new CustomsInvoiceVehicleDraft { SyrianPlate = "5", VehiclePrice = 9_300m, SerialNumber = "A5" },
            new CustomsInvoiceVehicleDraft { SyrianPlate = "6", VehiclePrice = 9_300m, SerialNumber = "A6" },
            new CustomsInvoiceVehicleDraft { SyrianPlate = "7", VehiclePrice = 9_000m, SerialNumber = "A7" }
        ];
        var inv = await h.Invoices.CreateAsync(invRequest);
        await h.Invoices.ApproveAsync(inv.Id, "admin");

        var receipt = await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, inv.Id, 50_000m, AccountingCurrency.USD, fxRate: 1500m));

        var detail = await h.Receipts.GetByIdAsync(receipt.Id);
        Assert.NotNull(detail);
        Assert.Equal(64_800m, detail!.Allocations[0].InvoiceTotalAmount);
        Assert.Equal(14_800m, detail.Allocations[0].InvoiceRemainingAmount);
        Assert.Equal(7, detail.Allocations[0].InvoiceVehicleCount);

        var model = h.Print.BuildReceipt(detail);
        Assert.True(model.IsCustomsReceipt);
        Assert.NotNull(model.CustomsReceipt);
        Assert.Equal("وصل قبض مالي (Receipt Voucher)", model.CustomsReceipt!.Title);
        Assert.Equal("نقداً (كاش)", model.CustomsReceipt.PaymentMethodLabel);
        Assert.Contains("المحترمين", model.CustomsReceipt.CustomerName);
        Assert.Contains("دولار أمريكي", model.CustomsReceipt.AmountInWords);
        Assert.Contains("$50,000", model.CustomsReceipt.AmountInWords);
        Assert.Single(model.CustomsReceipt.Allocations);
        Assert.Contains("فاتورة", model.CustomsReceipt.Allocations[0].AccountDetails);
        Assert.Equal("$64,800.00", model.CustomsReceipt.Allocations[0].InvoiceTotalDisplay);
        Assert.Equal("$50,000.00", model.CustomsReceipt.Allocations[0].PaidAmountDisplay);
        Assert.Equal("$14,800.00", model.CustomsReceipt.Allocations[0].RemainingAmountDisplay);
        Assert.Contains("USD", model.CustomsReceipt.TotalReceivedDisplay);
        Assert.Contains("تم استلام مبلغ", model.CustomsReceipt.NoteText);
        Assert.Contains("توقيع المسلّم", model.CustomsReceipt.PayerSignatureLabel);

        var path = h.Print.ExportReceiptPdf(detail);
        Assert.False(string.IsNullOrWhiteSpace(path));
        Assert.Equal(1, h.Export.ExportVoucherPdfCalls);
        Assert.True(h.Export.LastVoucher!.IsCustomsReceipt);
    }

    [Fact]
    public async Task BuildReceipt_iqd_cash_uses_dinar_wording()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("عميل تجريبي");
        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100_000m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");
        var receipt = await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, inv.Id, 40_000m, AccountingCurrency.IQD));

        var model = h.Print.BuildReceipt(receipt);
        Assert.True(model.IsCustomsReceipt);
        Assert.Contains("دينار عراقي", model.CustomsReceipt!.AmountInWords);
        Assert.Equal("نقداً (كاش)", model.CustomsReceipt.PaymentMethodLabel);
        Assert.Equal("استلمنا من السادة", model.PartyLabel);
    }
}
