using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Intelligence;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AlMuhasib.Infrastructure.Intelligence;

/// <summary>
/// محرك فهم الأسئلة المحاسبية داخل النظام. لا يتصل بأي نموذج لغوي أو خدمة خارجية.
/// </summary>
public sealed class QaidIntelligenceEngine : IQaidIntelligenceEngine
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ConversationContextService _conversation;
    private readonly IAssistantAccessGuard _guard;
    private readonly ISystemProfileService _profile;
    private readonly IBranchContext _branch;
    private readonly ICurrentUserService _user;
    private readonly ILogger<QaidIntelligenceEngine> _logger;
    private readonly SemaphoreSlim _queryGate = new(1, 1);

    public QaidIntelligenceEngine(
        IServiceScopeFactory scopes,
        ConversationContextService conversation,
        IAssistantAccessGuard guard,
        ISystemProfileService profile,
        IBranchContext branch,
        ICurrentUserService user,
        ILogger<QaidIntelligenceEngine> logger)
    {
        _scopes = scopes;
        _conversation = conversation;
        _guard = guard;
        _profile = profile;
        _branch = branch;
        _user = user;
        _logger = logger;
    }

    public async Task<AssistantAnswer> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        var userId = _user.UserId;
        var context = _conversation.Get(userId);
        var plan = QueryPlanningService.Plan(question, context);
        var denied = PermissionValidationService.Deny(plan, _guard);
        if (denied is not null)
            return denied;

        var direct = plan.Intent switch
        {
            AccountingIntent.Help => ArabicResponseGenerator.Help(),
            AccountingIntent.Unsafe => ArabicResponseGenerator.Unsafe(),
            AccountingIntent.Unknown => ArabicResponseGenerator.Unknown(),
            AccountingIntent.Unsupported => ArabicResponseGenerator.Unsupported(),
            _ => null
        };
        if (direct is not null)
            return direct;

        if (_profile.ActiveSystem != ApplicationSystemType.Accounting)
            return ArabicResponseGenerator.WrongModule();

        var shared = _profile.IsBranchClient || _profile.IsSharedServer;
        var entered = false;
        var handedOff = false;
        try
        {
            await _queryGate.WaitAsync(cancellationToken);
            entered = true;
            using var scope = _scopes.CreateScope();
            var query = new SecureDataQueryService(
                scope.ServiceProvider.GetRequiredService<IReportService>(),
                scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>());

            var running = query.ExecuteAsync(plan, cancellationToken);
            var finished = await Task.WhenAny(running, Task.Delay(TimeSpan.FromSeconds(25), cancellationToken));
            if (cancellationToken.IsCancellationRequested)
            {
                handedOff = true;
                ObserveAndRelease(running);
                throw new OperationCanceledException(cancellationToken);
            }

            if (finished != running)
            {
                handedOff = true;
                ObserveAndRelease(running);
                return Stamp(ArabicResponseGenerator.Timeout(shared));
            }

            var answer = await running;
            answer = Stamp(answer);
            _conversation.Remember(userId, plan, answer);
            return answer;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Stamp(ArabicResponseGenerator.Timeout(shared));
        }
        catch (Exception ex) when (IsConnectivity(ex))
        {
            _logger.LogWarning(ex, "تعذّر اتصال المساعد بقاعدة البيانات.");
            return Stamp(ArabicResponseGenerator.ConnectionFailed(shared));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل سؤال المساعد للنية {Intent}", plan.Intent);
            return Stamp(ArabicResponseGenerator.ReadFailed());
        }
        finally
        {
            if (entered && !handedOff)
                _queryGate.Release();
        }
    }

    public void ResetSession() => _conversation.Clear(_user.UserId);

    private AssistantAnswer Stamp(AssistantAnswer answer)
    {
        var branch = _branch.IsAllBranchesMode
            ? "كل الفروع المسموحة"
            : string.IsNullOrWhiteSpace(_branch.CurrentBranchName) ? "الفرع الحالي" : _branch.CurrentBranchName;
        var connection = _profile.IsBranchClient || _profile.IsSharedServer
            ? "قاعدة البيانات المشتركة"
            : "قاعدة البيانات المحلية";
        answer.Stamp(branch, connection);
        return answer;
    }

    private void ObserveAndRelease(Task running)
    {
        _ = running.ContinueWith(task =>
        {
            _queryGate.Release();
            if (task.IsFaulted)
                _logger.LogWarning(task.Exception, "أُلغي سؤال المساعد أو انتهت مهلته والاستعلام ما زال يُغلق.");
        }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }

    private static bool IsConnectivity(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException)
                return true;
            if (current is SqlException sql && sql.Number is -2 or -1 or 2 or 53 or 64 or 121 or 233 or 258 or 4060 or 10053 or 10054 or 10060 or 10061 or 18456)
                return true;
        }

        return false;
    }
}
