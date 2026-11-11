namespace AlMuhasib.Core.Intelligence;

/// <summary>يمنع قراءة أي مجال لا تغطيه صلاحية المساعد وصلاحية الشاشة الأصلية.</summary>
public static class PermissionValidationService
{
    public static AssistantAnswer? Deny(QueryPlan plan, IAssistantAccessGuard guard)
    {
        if (!guard.CanOpen)
        {
            return ArabicResponseGenerator.Denied(
                "عذراً، لا تمتلك الصلاحية اللازمة لاستخدام المساعد الذكي.");
        }

        if (plan.Domain != AssistantDataDomain.None && !guard.CanAccess(plan.Domain))
            return ArabicResponseGenerator.Denied(AccountingKnowledgeBase.DenyMessage(plan.Domain));

        return null;
    }
}
