using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Infrastructure;
using AlMuhasib.Infrastructure.Services.Customs;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

/// <summary>
/// Final acceptance gap coverage — exercises production APIs/constants only; no production logic changes.
/// </summary>
public class CustomsFinalAcceptanceGapTests
{
    [Theory]
    [InlineData(CustomsServiceType.Clearance)]
    [InlineData(CustomsServiceType.Transport)]
    [InlineData(CustomsServiceType.ClearanceAndTransport)]
    public async Task Acceptance_create_invoice_for_each_service_type(CustomsServiceType serviceType)
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync($"خدمة {serviceType}");
        var request = CustomsTestHarness.InvoiceRequest(
            customer.Id, AccountingCurrency.IQD, 250_000m, vehiclePlate: $"خدمة-{serviceType}");
        request.ServiceType = serviceType;

        var created = await h.Invoices.CreateAsync(request);
        Assert.Equal(serviceType, created.ServiceType);
        Assert.Equal(CustomsInvoiceStatus.Draft, created.Status);
        Assert.Equal(250_000m, created.TotalAmount);

        var loaded = await h.Invoices.GetByIdAsync(created.Id);
        Assert.NotNull(loaded);
        Assert.Equal(serviceType, loaded!.ServiceType);
    }

    [Fact]
    public async Task Acceptance_multi_vehicle_total_sums_prices_minus_discount()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("عدة مركبات");
        var request = CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100_000m, discount: 50_000m);
        request.Vehicles.Add(new Core.Models.Customs.CustomsInvoiceVehicleDraft
        {
            IraqiPlate = "مركبة2",
            CargoType = "بضائع",
            VehiclePrice = 200_000m,
            Status = CustomsVehicleStatus.InProgress
        });
        request.Vehicles.Add(new Core.Models.Customs.CustomsInvoiceVehicleDraft
        {
            IraqiPlate = "مركبة3",
            CargoType = "بضائع",
            VehiclePrice = 50_000m,
            Status = CustomsVehicleStatus.InProgress
        });

        var created = await h.Invoices.CreateAsync(request);
        Assert.Equal(3, created.Vehicles.Count);
        Assert.Equal(300_000m, created.TotalAmount); // 350k - 50k
        Assert.Equal(300_000m, created.RemainingAmount);
    }

    [Fact]
    public async Task Acceptance_sync_service_is_stub_not_enabled()
    {
        var sync = new CustomsSyncService();
        var connection = await sync.TestConnectionAsync();
        Assert.False(connection.IsSuccess);
        Assert.Contains("غير مفعّلة", connection.Message);

        var run = await sync.SyncNowAsync();
        Assert.False(run.IsSuccess);
        Assert.Contains("غير مفعّلة", run.Message);
    }

    [Fact]
    public void Acceptance_customs_database_isolated_from_other_systems()
    {
        Assert.Equal("AlMuhasibCustomsDb", SystemConnectionStrings.CustomsDatabase);
        Assert.Equal("AlMuhasibDb", SystemConnectionStrings.AccountingDatabase);
        Assert.NotEqual(SystemConnectionStrings.CustomsDatabase, SystemConnectionStrings.AccountingDatabase);
        Assert.NotEqual(SystemConnectionStrings.CustomsDatabase, SystemConnectionStrings.GoldShopDatabase);
        Assert.NotEqual(SystemConnectionStrings.CustomsDatabase, SystemConnectionStrings.CarContractsDatabase);
        Assert.NotEqual(SystemConnectionStrings.CustomsDatabase, SystemConnectionStrings.CarTradingDatabase);
        Assert.NotEqual(SystemConnectionStrings.CustomsDatabase, SystemConnectionStrings.RealEstateContractsDatabase);

        var customsCs = SystemConnectionStrings.Build(
            "Server=.;Database=Placeholder;Trusted_Connection=true;TrustServerCertificate=true;",
            ApplicationSystemType.CustomsClearance);
        Assert.Contains(SystemConnectionStrings.CustomsDatabase, customsCs, StringComparison.OrdinalIgnoreCase);

        var accountingCs = SystemConnectionStrings.Build(
            "Server=.;Database=Placeholder;Trusted_Connection=true;TrustServerCertificate=true;",
            ApplicationSystemType.Accounting);
        Assert.Contains(SystemConnectionStrings.AccountingDatabase, accountingCs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(SystemConnectionStrings.CustomsDatabase, accountingCs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Acceptance_attachment_soft_delete_keeps_metadata_when_disk_file_missing()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var temp = Path.Combine(Path.GetTempPath(), $"customs-acc-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(temp, "body");

        try
        {
            var item = await h.Attachments.AddFromFileAsync(
                CustomsAttachmentOwnerType.Customer, customer.Id, temp, null, "admin");
            Assert.True(File.Exists(item.FilePath));

            // Simulate disk already gone before SoftDelete (best-effort File.Delete path).
            File.Delete(item.FilePath);
            Assert.False(File.Exists(item.FilePath));

            await h.Attachments.SoftDeleteAsync(item.Id, "admin");

            Assert.Empty(await h.Attachments.GetByOwnerAsync(CustomsAttachmentOwnerType.Customer, customer.Id));
            await using var db = h.Factory.CreateDbContext();
            Assert.True(await db.CustomsAttachments.IgnoreQueryFilters()
                .AnyAsync(a => a.Id == item.Id && a.IsDeleted));
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    [Fact]
    public async Task Acceptance_customer_search_and_file_after_create()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("قبول بحث عميل", "07799998888");

        var (searchHits, _) = await h.Customers.GetPagedAsync(1, 20, search: "قبول بحث");
        Assert.Contains(searchHits, c => c.Id == customer.Id && c.Phone == "07799998888");

        var file = await h.Customers.GetCustomerFileAsync(customer.Id);
        Assert.NotNull(file);
        Assert.Equal(customer.Id, file!.Id);
        Assert.Equal("قبول بحث عميل", file.Name);
        Assert.Equal(0, file.UnpaidInvoiceCount);
        Assert.Equal(0m, file.BalanceIqd);
    }
}
