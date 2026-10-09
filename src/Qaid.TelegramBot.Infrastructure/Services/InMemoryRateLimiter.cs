using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Qaid.TelegramBot.Application.Abstractions;
using Qaid.TelegramBot.Application.Options;

namespace Qaid.TelegramBot.Infrastructure.Services;

public sealed class InMemoryRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<long, Queue<DateTime>> _hits = new();
    private readonly int _limit;

    public InMemoryRateLimiter(IOptions<TelegramBotOptions> options)
    {
        _limit = Math.Max(1, options.Value.RateLimitPerMinute);
    }

    public bool TryAcquire(long telegramUserId)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-1);
        var q = _hits.GetOrAdd(telegramUserId, _ => new Queue<DateTime>());
        lock (q)
        {
            while (q.Count > 0 && q.Peek() < windowStart)
                q.Dequeue();
            if (q.Count >= _limit)
                return false;
            q.Enqueue(now);
            return true;
        }
    }
}
