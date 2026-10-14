using Qaid.TelegramBot.Application.Models;

namespace Qaid.TelegramBot.Application.Bot;

/// <summary>
/// Compact callback payloads. Never includes TenantId or tokens.
/// </summary>
public static class CallbackData
{
    public const string Main = "m";
    public const string Unlink = "u:unlink";
    public const string UnlinkConfirm = "u:unlink:yes";
    public const string BranchList = "br";
    public const string PromptCustomer = "st:c";
    public const string PromptSupplier = "st:s";
    public const string SystemDashboard = "sys";

    public static string Group(ReportGroup group) => $"g:{(int)group}";
    public static string Report(ReportKind kind) => $"r:{(int)kind}";
    public static string Period(ReportKind kind, ReportPeriod period) => $"p:{(int)kind}:{(int)period}";
    public static string BackPeriod(ReportKind kind) => $"b:period:{(int)kind}";
    public static string SelectBranch(int branchId) => $"br:{branchId}";
    public static string PickCustomer(Guid syncId) => $"sc:{syncId:N}";
    public static string PickSupplier(Guid syncId) => $"ss:{syncId:N}";

    public static bool TryParse(string? data, out CallbackAction action)
    {
        action = default;
        if (string.IsNullOrWhiteSpace(data))
            return false;

        var parts = data.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1 && parts[0] == "m")
        {
            action = new CallbackAction(CallbackActionType.MainMenu);
            return true;
        }

        if (parts.Length == 1 && parts[0] == "br")
        {
            action = new CallbackAction(CallbackActionType.BranchList);
            return true;
        }

        if (parts.Length == 1 && parts[0] == "sys")
        {
            action = new CallbackAction(CallbackActionType.SystemDashboard);
            return true;
        }

        if (parts.Length == 2 && parts[0] == "g"
            && int.TryParse(parts[1], out var gid)
            && Enum.IsDefined(typeof(ReportGroup), gid))
        {
            action = new CallbackAction(CallbackActionType.OpenGroup, Group: (ReportGroup)gid);
            return true;
        }

        if (parts.Length == 2 && parts[0] == "br" && int.TryParse(parts[1], out var branchId) && branchId > 0)
        {
            action = new CallbackAction(CallbackActionType.SelectBranch, BranchId: branchId);
            return true;
        }

        if (parts.Length == 2 && parts[0] == "st" && parts[1] is "c" or "s")
        {
            action = new CallbackAction(
                parts[1] == "c" ? CallbackActionType.PromptCustomerSearch : CallbackActionType.PromptSupplierSearch);
            return true;
        }

        if (parts.Length == 2 && parts[0] == "sc" && Guid.TryParseExact(parts[1], "N", out var customerSync))
        {
            action = new CallbackAction(CallbackActionType.ShowCustomerStatement, EntitySyncId: customerSync);
            return true;
        }

        if (parts.Length == 2 && parts[0] == "ss" && Guid.TryParseExact(parts[1], "N", out var supplierSync))
        {
            action = new CallbackAction(CallbackActionType.ShowSupplierStatement, EntitySyncId: supplierSync);
            return true;
        }

        if (parts.Length >= 2 && parts[0] == "u")
        {
            if (parts is ["u", "unlink"])
            {
                action = new CallbackAction(CallbackActionType.UnlinkPrompt);
                return true;
            }
            if (parts is ["u", "unlink", "yes"])
            {
                action = new CallbackAction(CallbackActionType.UnlinkConfirm);
                return true;
            }
            return false;
        }

        if (parts.Length == 2 && parts[0] == "r" && int.TryParse(parts[1], out var rk) && Enum.IsDefined(typeof(ReportKind), rk))
        {
            action = new CallbackAction(CallbackActionType.SelectReport, Kind: (ReportKind)rk);
            return true;
        }

        if (parts.Length == 3 && parts[0] == "p"
            && int.TryParse(parts[1], out var pk) && Enum.IsDefined(typeof(ReportKind), pk)
            && int.TryParse(parts[2], out var pp) && Enum.IsDefined(typeof(ReportPeriod), pp))
        {
            action = new CallbackAction(CallbackActionType.RunReport, Kind: (ReportKind)pk, Period: (ReportPeriod)pp);
            return true;
        }

        if (parts.Length == 3 && parts[0] == "b" && parts[1] == "period"
            && int.TryParse(parts[2], out var bk) && Enum.IsDefined(typeof(ReportKind), bk))
        {
            action = new CallbackAction(CallbackActionType.PeriodMenu, Kind: (ReportKind)bk);
            return true;
        }

        return false;
    }
}

public enum CallbackActionType
{
    MainMenu,
    OpenGroup,
    SelectReport,
    PeriodMenu,
    RunReport,
    UnlinkPrompt,
    UnlinkConfirm,
    BranchList,
    SelectBranch,
    PromptCustomerSearch,
    PromptSupplierSearch,
    ShowCustomerStatement,
    ShowSupplierStatement,
    SystemDashboard
}

public readonly record struct CallbackAction(
    CallbackActionType Type,
    ReportKind? Kind = null,
    ReportPeriod? Period = null,
    ReportGroup? Group = null,
    int? BranchId = null,
    Guid? EntitySyncId = null);
