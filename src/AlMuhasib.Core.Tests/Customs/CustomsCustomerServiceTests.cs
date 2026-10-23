using AlMuhasib.Core.Enums.Customs;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

public class CustomsCustomerServiceTests
{
    [Fact]
    public async Task Create_and_edit_customer()
    {
        var h = CustomsTestHarness.CreateAdmin();

        var created = await h.SeedCustomerAsync("أحمد", "07711111111");
        Assert.True(created.Id > 0);
        Assert.Equal("أحمد", created.Name);

        created.Name = "أحمد المحدّث";
        created.Phone = "07722222222";
        created.CustomerType = CustomsCustomerType.Company;
        created.CompanyName = "شركة النقل";
        created.UpdatedBy = "admin";

        var updated = await h.Customers.UpdateAsync(created);
        Assert.Equal("أحمد المحدّث", updated.Name);
        Assert.Equal("07722222222", updated.Phone);
        Assert.Equal("شركة النقل", updated.CompanyName);

        var loaded = await h.Customers.GetByIdAsync(updated.Id);
        Assert.NotNull(loaded);
        Assert.Equal("أحمد المحدّث", loaded!.Name);
    }

    [Fact]
    public async Task Create_requires_name()
    {
        var h = CustomsTestHarness.CreateAdmin();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Customers.CreateAsync(new Core.Entities.Customs.CustomsCustomer
            {
                Name = "  ",
                CreatedBy = "admin",
                RowVersion = [1]
            }));
    }
}
