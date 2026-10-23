using AlMuhasib.Core.Enums.Customs;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

public class CustomsAttachmentServiceTests
{
    [Fact]
    public async Task Attach_file_and_soft_delete()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();

        var temp = Path.Combine(Path.GetTempPath(), $"customs-att-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(temp, "attachment-body");

        try
        {
            var item = await h.Attachments.AddFromFileAsync(
                CustomsAttachmentOwnerType.Customer,
                customer.Id,
                temp,
                "مستند هوية",
                "admin");

            Assert.True(item.Id > 0);
            Assert.Equal(Path.GetFileName(temp), item.FileName);
            Assert.True(File.Exists(item.FilePath));

            var list = await h.Attachments.GetByOwnerAsync(CustomsAttachmentOwnerType.Customer, customer.Id);
            Assert.Single(list);

            await h.Attachments.SoftDeleteAsync(item.Id, "admin");
            var after = await h.Attachments.GetByOwnerAsync(CustomsAttachmentOwnerType.Customer, customer.Id);
            Assert.Empty(after);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    [Fact]
    public async Task Attach_requires_existing_source_file()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Attachments.AddFromFileAsync(
                CustomsAttachmentOwnerType.Customer,
                customer.Id,
                Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.bin"),
                null,
                "admin"));
    }
}
