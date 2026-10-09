using Qaid.TelegramBot.Application.Models;

namespace Qaid.TelegramBot.Application.Bot;

/// <summary>
/// Compact callback payloads. Never includes TenantId or tokens.
/// Formats: m | r:{kind} | p:{kind}:{period} | b:main | b:period:{kind} | u:unlink | u:unlink:yes
/// </summary>
public static class CallbackData
{
    public const string Main = "m";
    public const string Unlink = "u:unlink";
    public const string UnlinkConfirm = "u:unlink:yes";

    public static string Report(ReportKind kind) => $"r:{(int)kind}";
    public static string Period(ReportKind kind, ReportPeriod period) => $"p:{(int)kind}:{(int)period}";
    public static string BackPeriod(ReportKind kind) => $"b:period:{(int)kind}";

    public static bool TryParse(string? data, out CallbackAction action)
    {
        action = default;
        if (string.IsNullOrWhiteSpace(data))
            return false;

        var parts = data.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1 && parts[0] == "m")
        {
            action = new CallbackAction(CallbackActionType.MainMenu, null, null);
            return true;
        }

        if (parts.Length >= 2 && parts[0] == "u")
        {
            if (parts is ["u", "unlink"])
            {
                action = new CallbackAction(CallbackActionType.UnlinkPrompt, null, null);
                return true;
            }
            if (parts is ["u", "unlink", "yes"])
            {
                action = new CallbackAction(CallbackActionType.UnlinkConfirm, null, null);
                return true;
            }
            return false;
        }

        if (parts.Length == 2 && parts[0] == "r" && int.TryParse(parts[1], out var rk) && Enum.IsDefined(typeof(ReportKind), rk))
        {
            action = new CallbackAction(CallbackActionType.SelectReport, (ReportKind)rk, null);
            return true;
        }

        if (parts.Length == 3 && parts[0] == "p"
            && int.TryParse(parts[1], out var pk) && Enum.IsDefined(typeof(ReportKind), pk)
            && int.TryParse(parts[2], out var pp) && Enum.IsDefined(typeof(ReportPeriod), pp))
        {
            action = new CallbackAction(CallbackActionType.RunReport, (ReportKind)pk, (ReportPeriod)pp);
            return true;
        }

        if (parts.Length == 3 && parts[0] == "b" && parts[1] == "period"
            && int.TryParse(parts[2], out var bk) && Enum.IsDefined(typeof(ReportKind), bk))
        {
            action = new CallbackAction(CallbackActionType.PeriodMenu, (ReportKind)bk, null);
            return true;
        }

        return false;
    }
}

public enum CallbackActionType
{
    MainMenu,
    SelectReport,
    PeriodMenu,
    RunReport,
    UnlinkPrompt,
    UnlinkConfirm
}

public readonly record struct CallbackAction(CallbackActionType Type, ReportKind? Kind, ReportPeriod? Period);
