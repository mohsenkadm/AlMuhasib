namespace AlMuhasib.Core.Intelligence;

/// <summary>يحسب الفروقات والنسب قبل صياغة النص. لا يخترع أرقاماً غير موجودة في اللقطة.</summary>
public static class ResultAnalysisService
{
    public static decimal? ChangePercent(decimal current, decimal previous) =>
        AssistantMoney.ChangePercent(current, previous);

    public static bool CanSplitReturns(decimal net, decimal gross, decimal returns) =>
        Math.Abs((gross - returns) - net) < 1m;

    public static IReadOnlyList<AssistantChartPoint> CompareChart(string currentLabel, decimal current, string previousLabel, decimal previous) =>
    [
        new() { Label = previousLabel, Value = previous },
        new() { Label = currentLabel, Value = current }
    ];
}
