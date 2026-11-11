namespace AlMuhasib.Core.Intelligence;

/// <summary>ينظّف السؤال العربي ويفتح المرادفات قبل تحديد النية.</summary>
public static class ArabicLanguageProcessor
{
    public static string Prepare(string? question)
    {
        var normalized = ArabicTextNormalizer.Normalize(question);
        normalized = SqlTextGuard.Strip(normalized);
        return AccountingLexicon.Apply(normalized);
    }
}
