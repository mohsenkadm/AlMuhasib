namespace AlMuhasib.Core.Intelligence;

/// <summary>سياق محادثة واحد لكل مستخدم داخل الجلسة. لا يُخلط مع مستخدم أو شركة أخرى.</summary>
public sealed class ConversationContextService
{
    private readonly object _gate = new();
    private readonly Dictionary<string, ConversationContext> _contexts = new();

    public ConversationContext Get(int? userId)
    {
        lock (_gate)
        {
            var key = Key(userId);
            if (!_contexts.TryGetValue(key, out var context))
            {
                context = new ConversationContext();
                _contexts[key] = context;
            }

            return context;
        }
    }

    public void Remember(int? userId, QueryPlan plan, AssistantAnswer answer)
    {
        if (!answer.UpdatesContext)
            return;

        lock (_gate)
        {
            var context = Get(userId);
            context.LastIntent = plan.Intent;
            context.From = plan.From;
            context.To = plan.To;
            context.PeriodLabel = plan.PeriodLabel;
            if (answer.FocusCustomerId is int id)
            {
                context.CustomerId = id;
                context.CustomerName = answer.FocusCustomerName;
            }
        }
    }

    public void Clear(int? userId)
    {
        lock (_gate)
            _contexts.Remove(Key(userId));
    }

    private static string Key(int? userId) => userId?.ToString() ?? "anonymous";
}
