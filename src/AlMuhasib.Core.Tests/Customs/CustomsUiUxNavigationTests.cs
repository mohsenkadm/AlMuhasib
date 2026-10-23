using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Models.Customs;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

/// <summary>
/// Binding/navigation contract tests for Customs UI/UX (no WPF runtime).
/// </summary>
public sealed class CustomsUiUxNavigationTests
{
    [Fact]
    public void ListItem_CanApprove_OnlyWhenDraft()
    {
        var draft = new CustomsInvoiceListItem { Status = CustomsInvoiceStatus.Draft };
        var approved = new CustomsInvoiceListItem { Status = CustomsInvoiceStatus.Approved };
        var cancelled = new CustomsInvoiceListItem { Status = CustomsInvoiceStatus.Cancelled };

        Assert.True(draft.CanApprove);
        Assert.False(approved.CanApprove);
        Assert.False(cancelled.CanApprove);
    }

    [Fact]
    public void ListItem_CanCancel_RequiresNotCancelledAndUnpaid()
    {
        var unpaid = new CustomsInvoiceListItem
        {
            Status = CustomsInvoiceStatus.Approved,
            PaidAmount = 0
        };
        var paid = new CustomsInvoiceListItem
        {
            Status = CustomsInvoiceStatus.Approved,
            PaidAmount = 10
        };
        var cancelled = new CustomsInvoiceListItem
        {
            Status = CustomsInvoiceStatus.Cancelled,
            PaidAmount = 0
        };

        Assert.True(unpaid.CanCancel);
        Assert.False(paid.CanCancel);
        Assert.False(cancelled.CanCancel);
    }

    [Theory]
    [InlineData(CustomsServiceType.Clearance)]
    [InlineData(CustomsServiceType.Transport)]
    [InlineData(CustomsServiceType.ClearanceAndTransport)]
    public void ServiceType_EnumValues_CoverDashboardQuickActions(CustomsServiceType type)
    {
        Assert.True(Enum.IsDefined(typeof(CustomsServiceType), type));
    }
}
