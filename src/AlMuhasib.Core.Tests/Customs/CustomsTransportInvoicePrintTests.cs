using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Models.Customs;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

public class CustomsTransportInvoicePrintTests
{
    [Fact]
    public async Task BuildTransportInvoice_external_uses_portrait_form_and_price_groups()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("شركة سيدرال");
        var request = CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.USD, 9300m, fxRate: 1500m);
        request.ServiceType = CustomsServiceType.Transport;
        request.TransportKind = CustomsTransportKind.External;
        request.Vehicles =
        [
            new CustomsInvoiceVehicleDraft
            {
                SerialNumber = "031581177",
                SyrianPlate = "5514826",
                SyrianDriver = "محمد أحمد",
                CargoType = "مستلزمات طبية",
                VehiclePrice = 9300m
            },
            new CustomsInvoiceVehicleDraft
            {
                SerialNumber = "2031581174",
                SyrianPlate = "216241",
                SyrianDriver = "علي حسن",
                CargoType = "مستلزمات طبية",
                VehiclePrice = 9300m
            },
            new CustomsInvoiceVehicleDraft
            {
                SerialNumber = "X-9000",
                SyrianPlate = "999001",
                SyrianDriver = "سائق ثالث",
                CargoType = "مستلزمات طبية",
                VehiclePrice = 9000m
            }
        ];

        var inv = await h.Invoices.CreateAsync(request);
        var detail = await h.Invoices.GetByIdAsync(inv.Id);
        Assert.NotNull(detail);

        var model = h.Print.BuildTransportInvoice(detail!, customer.Phone);
        Assert.True(model.IsCustomsTransportInvoice);
        Assert.False(model.IsCustomsDailyInvoice);
        Assert.Equal("A4", model.PaperSize);
        Assert.Equal("فاتورة حساب أجور نقل", model.Title);
        Assert.NotNull(model.CustomsTransport);
        Assert.Equal(CustomsTransportKind.External, model.CustomsTransport!.TransportKind);
        Assert.Equal(3, model.CustomsTransport.Rows.Count);
        Assert.Equal(2, model.CustomsTransport.PriceGroups.Count);
        Assert.Contains(model.CustomsTransport.PriceGroups, g => g.VehicleCount == 2 && g.UnitPrice == 9300m);
        Assert.Contains(model.CustomsTransport.PriceGroups, g => g.VehicleCount == 1 && g.UnitPrice == 9000m);
        Assert.Equal(27_600m, model.CustomsTransport.GrandTotal);
        Assert.Contains("دولار أمريكي", model.CustomsTransport.AmountInWords);
        Assert.Contains("المحترمين", model.CustomsTransport.CustomerName);
        Assert.Equal("5514826", model.CustomsTransport.Rows[0].SyrianPlate);
        Assert.Equal("031581177", model.CustomsTransport.Rows[0].ReferenceNumber);
    }

    [Fact]
    public async Task BuildTransportInvoice_internal_includes_route_penalties_and_words()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("شركة سيدرال");
        var request = CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 1_200_000m);
        request.ServiceType = CustomsServiceType.Transport;
        request.TransportKind = CustomsTransportKind.Internal;
        request.TransportRoute = "من منفذ ربيعة الحدودية ← إلى بغداد (داخل العراق)";
        request.Description = "شحن ونقل داخلي (2 شاحنات) مع احتساب الغرامات التأخيرية";
        request.Vehicles =
        [
            new CustomsInvoiceVehicleDraft
            {
                SerialNumber = "BRI202600000027",
                SyrianPlate = "724030",
                SyrianDriver = "أسعد الحج جاسم",
                UnloadingDate = new DateTime(2026, 8, 2),
                VehiclePrice = 1_200_000m,
                DelayPenaltyAmount = 50_000m
            },
            new CustomsInvoiceVehicleDraft
            {
                SerialNumber = "56401246",
                SyrianPlate = "8154964",
                SyrianDriver = "إسماعيل موسى العلي",
                UnloadingDate = new DateTime(2026, 8, 2),
                VehiclePrice = 1_200_000m,
                DelayPenaltyAmount = 0m
            }
        ];

        var inv = await h.Invoices.CreateAsync(request);
        var detail = await h.Invoices.GetByIdAsync(inv.Id);
        Assert.NotNull(detail);
        Assert.Equal(CustomsTransportKind.Internal, detail!.TransportKind);
        Assert.Equal(2_450_000m, detail.TotalAmount); // 2×1.2M + 50k
        Assert.Equal(50_000m, detail.DelayPenaltiesSum);

        var model = h.Print.BuildCustomerInvoice(detail, customer.Phone);
        Assert.True(model.IsCustomsTransportInvoice);
        Assert.Contains("فاتورة نقل داخلي", model.Title);
        Assert.Equal(CustomsTransportKind.Internal, model.CustomsTransport!.TransportKind);
        Assert.Equal(request.TransportRoute, model.CustomsTransport.TransportRoute);
        Assert.Equal(request.Description, model.CustomsTransport.ServiceDescription);
        Assert.Equal(50_000m, model.CustomsTransport.DelayPenaltiesTotal);
        Assert.StartsWith("المبلغ فقط وقدره:", model.CustomsTransport.AmountInWords);
        Assert.Contains("دينار عراقي", model.CustomsTransport.AmountInWords);
        Assert.Equal("BRI202600000027", model.CustomsTransport.Rows[0].ReferenceNumber);
        Assert.Equal("2026/8/2", model.CustomsTransport.Rows[0].UnloadingDate);
    }

    [Fact]
    public void TransportKind_display_labels_match_company_wording()
    {
        Assert.Equal("نقل داخلي", CustomsTransportKindDisplay.ToArabic(CustomsTransportKind.Internal));
        Assert.Equal("نقل خارجي", CustomsTransportKindDisplay.ToArabic(CustomsTransportKind.External));
        Assert.Equal(
            "نقل خارجي",
            CustomsServiceTypeDisplay.ToArabic(CustomsServiceType.Transport, CustomsTransportKind.External));
        Assert.Equal(
            "تخليص",
            CustomsServiceTypeDisplay.ToArabic(CustomsServiceType.Clearance, CustomsTransportKind.External));
    }

    [Fact]
    public async Task Create_transport_without_kind_is_rejected()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var request = CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100_000m);
        request.ServiceType = CustomsServiceType.Transport;
        request.TransportKind = null;

        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Invoices.CreateAsync(request));
    }
}
