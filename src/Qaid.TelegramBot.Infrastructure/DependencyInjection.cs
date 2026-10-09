using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qaid.TelegramBot.Application.Abstractions;
using Qaid.TelegramBot.Application.Options;
using Qaid.TelegramBot.Application.Services;
using Qaid.TelegramBot.Infrastructure.Data;
using Qaid.TelegramBot.Infrastructure.Security;
using Qaid.TelegramBot.Infrastructure.Services;
using Qaid.TelegramBot.Infrastructure.Telegram;
using Telegram.Bot;

namespace Qaid.TelegramBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddQaidTelegramBot(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TelegramBotOptions>(configuration.GetSection(TelegramBotOptions.SectionName));
        services.Configure<QaidApiOptions>(configuration.GetSection(QaidApiOptions.SectionName));
        services.Configure<LinkOptions>(configuration.GetSection(LinkOptions.SectionName));

        var conn = configuration.GetConnectionString("BotDb") ?? "Data Source=qaid-telegram-bot.db";
        services.AddDbContext<BotDbContext>(opt => opt.UseSqlite(conn));

        services.AddDataProtection()
            .PersistKeysToDbContext<BotDbContext>();

        services.AddSingleton<ITokenProtector, TokenProtector>();
        services.AddSingleton<IRateLimiter, InMemoryRateLimiter>();
        services.AddScoped<ILinkService, LinkService>();
        services.AddScoped<ISessionTokenService, SessionTokenService>();
        services.AddScoped<IBotUpdateProcessor, BotUpdateProcessor>();
        services.AddScoped<ITelegramBotGateway, TelegramBotGateway>();

        var apiOptions = configuration.GetSection(QaidApiOptions.SectionName).Get<QaidApiOptions>() ?? new QaidApiOptions();
        services.AddHttpClient<IQaidApiClient, QaidApiClient>(client =>
        {
            if (!string.IsNullOrWhiteSpace(apiOptions.BaseUrl))
                client.BaseAddress = new Uri(apiOptions.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(apiOptions.TimeoutSeconds <= 0 ? 30 : apiOptions.TimeoutSeconds);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        var telegram = configuration.GetSection(TelegramBotOptions.SectionName).Get<TelegramBotOptions>() ?? new TelegramBotOptions();
        services.AddSingleton<ITelegramBotClient>(_ =>
        {
            var token = string.IsNullOrWhiteSpace(telegram.BotToken)
                ? "0000000000:TEST_TOKEN_PLACEHOLDER_DO_NOT_USE"
                : telegram.BotToken;
            return new TelegramBotClient(token);
        });

        return services;
    }
}
