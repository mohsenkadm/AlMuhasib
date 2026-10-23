using AlMuhasib.Core.Constants;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Infrastructure.Services.Customs;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

public class CustomsPermissionGuardTests
{
    [Fact]
    public void Admin_bypasses_all_ensures()
    {
        var user = new CustomsFakeUser { IsAdmin = true, Role = UserRole.Admin };
        var guard = new CustomsPermissionGuard(user);

        guard.EnsureView(CustomsScreenNames.Invoices);
        guard.EnsureAdd(CustomsScreenNames.Invoices);
        guard.EnsureEdit(CustomsScreenNames.Receipts);
        guard.EnsureDelete(CustomsScreenNames.Customers);
        guard.EnsurePrint(CustomsScreenNames.Reports);
        guard.EnsureExport(CustomsScreenNames.ProfitReport);
        Assert.True(guard.CanViewExpenses);
        Assert.True(guard.CanViewProfit);
    }

    [Fact]
    public void User_without_view_is_denied()
    {
        var user = new CustomsFakeUser { IsAdmin = false, Role = UserRole.User };
        var guard = new CustomsPermissionGuard(user);

        Assert.Throws<UnauthorizedAccessException>(() =>
            guard.EnsureView(CustomsScreenNames.Customers));
    }

    [Theory]
    [InlineData(CustomsScreenNames.Customers)]
    [InlineData(CustomsScreenNames.Invoices)]
    [InlineData(CustomsScreenNames.Receipts)]
    [InlineData(CustomsScreenNames.Statement)]
    [InlineData(CustomsScreenNames.Reports)]
    [InlineData(CustomsScreenNames.ProfitReport)]
    [InlineData(CustomsScreenNames.Expenses)]
    [InlineData(CustomsScreenNames.Dashboard)]
    public void User_view_only_cannot_mutate_or_print(string screen)
    {
        var user = new CustomsFakeUser { IsAdmin = false, Role = UserRole.User };
        user.Grant(screen, view: true);
        var guard = new CustomsPermissionGuard(user);

        guard.EnsureView(screen);
        Assert.Throws<UnauthorizedAccessException>(() => guard.EnsureAdd(screen));
        Assert.Throws<UnauthorizedAccessException>(() => guard.EnsureEdit(screen));
        Assert.Throws<UnauthorizedAccessException>(() => guard.EnsureDelete(screen));
        Assert.Throws<UnauthorizedAccessException>(() => guard.EnsurePrint(screen));
        Assert.Throws<UnauthorizedAccessException>(() => guard.EnsureExport(screen));
    }

    [Fact]
    public void User_with_print_but_not_export_is_denied_export()
    {
        var user = new CustomsFakeUser { IsAdmin = false, Role = UserRole.User };
        user.Grant(CustomsScreenNames.Invoices, view: true, print: true);
        var guard = new CustomsPermissionGuard(user);

        guard.EnsurePrint(CustomsScreenNames.Invoices);
        Assert.Throws<UnauthorizedAccessException>(() =>
            guard.EnsureExport(CustomsScreenNames.Invoices));
    }

    [Fact]
    public void User_with_print_or_export_passes_EnsurePrintOrExport()
    {
        var user = new CustomsFakeUser { IsAdmin = false, Role = UserRole.User };
        user.Grant(CustomsScreenNames.Receipts, view: true, export: true);
        var guard = new CustomsPermissionGuard(user);

        guard.EnsurePrintOrExport(CustomsScreenNames.Receipts);
    }

    [Fact]
    public void ScreenForOwner_maps_attachment_owners()
    {
        var guard = new CustomsPermissionGuard(new CustomsFakeUser());
        Assert.Equal(CustomsScreenNames.Customers, guard.ScreenForOwner(CustomsAttachmentOwnerType.Customer));
        Assert.Equal(CustomsScreenNames.Receipts, guard.ScreenForOwner(CustomsAttachmentOwnerType.Receipt));
        Assert.Equal(CustomsScreenNames.Invoices, guard.ScreenForOwner(CustomsAttachmentOwnerType.Invoice));
        Assert.Equal(CustomsScreenNames.Invoices, guard.ScreenForOwner(CustomsAttachmentOwnerType.Vehicle));
        Assert.Equal(CustomsScreenNames.Invoices, guard.ScreenForOwner(CustomsAttachmentOwnerType.Expense));
    }

    [Fact]
    public async Task Service_denies_customer_create_without_add()
    {
        var h = CustomsTestHarness.CreateUser(u =>
            u.Grant(CustomsScreenNames.Customers, view: true));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            h.SeedCustomerAsync());
    }

    [Fact]
    public async Task Service_denies_invoice_list_without_view()
    {
        var h = CustomsTestHarness.CreateUser(); // no grants

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            h.Invoices.GetPagedAsync(1, 20));
    }
}
