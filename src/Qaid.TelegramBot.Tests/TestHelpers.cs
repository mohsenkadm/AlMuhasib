using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Qaid.TelegramBot.Application.Abstractions;
using Qaid.TelegramBot.Application.Models;
using Qaid.TelegramBot.Application.Options;
using Qaid.TelegramBot.Application.Services;
using Qaid.TelegramBot.Infrastructure.Data;
using Qaid.TelegramBot.Infrastructure.Security;
using Qaid.TelegramBot.Infrastructure.Services;
using RichardSzalay.MockHttp;

namespace Qaid.TelegramBot.Tests;

internal static class TestHelpers
{
    public static (BotDbContext Db, ITokenProtector Protector, LinkService Links) CreateLinkStack(string? dbName = null)
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        var sp = services.BuildServiceProvider();
        var protector = new TokenProtector(sp.GetRequiredService<IDataProtectionProvider>());

        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseSqlite($"Data Source={dbName ?? Guid.NewGuid().ToString("N")};Mode=Memory;Cache=Shared")
            .Options;
        var db = new BotDbContext(options);
        // Keep connection open for shared in-memory DB.
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        var links = new LinkService(db, protector, Options.Create(new LinkOptions
        {
            CodeLifetimeMinutes = 5,
            CodeLength = 8
        }));
        return (db, protector, links);
    }

    public static CreateLinkCodeRequest SampleCodeRequest(int tenantId = 10)
        => new()
        {
            TenantId = tenantId,
            TenantAccountId = 22,
            CompanyName = "شركة اختبار",
            Username = "user1",
            CurrentBranchId = 1,
            AccessToken = "access-token-" + tenantId,
            RefreshToken = "refresh-token-" + tenantId,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

    public static BotUpdateProcessor CreateProcessor(
        ILinkService links,
        IQaidApiClient api,
        ITelegramBotGateway bot,
        TelegramBotOptions? options = null)
    {
        var sessions = new SessionTokenService(api, links, NullLogger<SessionTokenService>.Instance);
        return new BotUpdateProcessor(
            links,
            api,
            sessions,
            bot,
            new InMemoryRateLimiter(Options.Create(new TelegramBotOptions { RateLimitPerMinute = 100 })),
            Options.Create(options ?? new TelegramBotOptions { PublicBaseUrl = "https://bot.test" }),
            NullLogger<BotUpdateProcessor>.Instance);
    }

    public static Mock<ITelegramBotGateway> CreateBotMock(List<string> sentTexts)
    {
        var mock = new Mock<ITelegramBotGateway>(MockBehavior.Loose);
        mock.Setup(b => b.SendTextAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<IReadOnlyList<BotButton>>?>(), It.IsAny<CancellationToken>()))
            .Callback<long, string, IReadOnlyList<IReadOnlyList<BotButton>>?, CancellationToken>((_, text, _, _) => sentTexts.Add(text))
            .Returns(Task.CompletedTask);
        mock.Setup(b => b.EditTextAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<IReadOnlyList<BotButton>>?>(), It.IsAny<CancellationToken>()))
            .Callback<long, int, string, IReadOnlyList<IReadOnlyList<BotButton>>?, CancellationToken>((_, _, text, _, _) => sentTexts.Add(text))
            .Returns(Task.CompletedTask);
        mock.Setup(b => b.AnswerCallbackAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    public static QaidApiClient CreateApiClient(MockHttpMessageHandler handler, string baseUrl = "https://api.test/")
    {
        var http = handler.ToHttpClient();
        http.BaseAddress = new Uri(baseUrl);
        return new QaidApiClient(http, NullLogger<QaidApiClient>.Instance);
    }
}
