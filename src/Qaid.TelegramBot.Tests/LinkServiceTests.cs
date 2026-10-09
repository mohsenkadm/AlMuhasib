using Qaid.TelegramBot.Infrastructure.Data;

namespace Qaid.TelegramBot.Tests;

public class LinkServiceTests
{
    [Fact]
    public async Task Expired_code_is_rejected()
    {
        var (db, _, links) = TestHelpers.CreateLinkStack();
        await using (db)
        {
            var created = await links.CreateLinkCodeAsync(TestHelpers.SampleCodeRequest(), CancellationToken.None);
            var entity = Assert.Single(db.LinkCodes);
            entity.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();

            var (ok, message, link) = await links.ConsumeLinkCodeAsync(created.PlainCode, telegramUserId: 1001, CancellationToken.None);
            Assert.False(ok);
            Assert.Null(link);
            Assert.Contains("انتهت", message);
        }
    }

    [Fact]
    public async Task Used_code_cannot_be_reused()
    {
        var (db, _, links) = TestHelpers.CreateLinkStack();
        await using (db)
        {
            var created = await links.CreateLinkCodeAsync(TestHelpers.SampleCodeRequest(), CancellationToken.None);
            var first = await links.ConsumeLinkCodeAsync(created.PlainCode, 1001, CancellationToken.None);
            Assert.True(first.Ok);

            var second = await links.ConsumeLinkCodeAsync(created.PlainCode, 1002, CancellationToken.None);
            Assert.False(second.Ok);
            Assert.Contains("مسبقاً", second.Message);
        }
    }

    [Fact]
    public async Task Unlink_blocks_further_active_lookups()
    {
        var (db, _, links) = TestHelpers.CreateLinkStack();
        await using (db)
        {
            var created = await links.CreateLinkCodeAsync(TestHelpers.SampleCodeRequest(), CancellationToken.None);
            await links.ConsumeLinkCodeAsync(created.PlainCode, 55, CancellationToken.None);
            Assert.NotNull(await links.GetActiveLinkAsync(55, CancellationToken.None));

            await links.UnlinkAsync(55, CancellationToken.None);
            Assert.Null(await links.GetActiveLinkAsync(55, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Code_is_stored_as_hash_not_plaintext()
    {
        var (db, _, links) = TestHelpers.CreateLinkStack();
        await using (db)
        {
            var created = await links.CreateLinkCodeAsync(TestHelpers.SampleCodeRequest(), CancellationToken.None);
            var entity = Assert.Single(db.LinkCodes.ToList());
            Assert.DoesNotContain(created.PlainCode, entity.CodeHash, StringComparison.OrdinalIgnoreCase);
            Assert.NotEqual(created.PlainCode, entity.CodeHash);
        }
    }
}
