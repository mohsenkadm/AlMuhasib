using System.Text;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.UI.Services;

public sealed class VoiceCommandMatcher
{
    public VoiceCommandMatch? Match(string? input, IReadOnlyList<VoiceCommandDefinition> commands)
    {
        var normalized = NormalizeArabic(input);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        VoiceCommandMatch? best = null;

        foreach (var command in commands)
        {
            foreach (var phrase in command.Phrases)
            {
                var normalizedPhrase = NormalizeArabic(phrase);
                if (string.IsNullOrWhiteSpace(normalizedPhrase))
                    continue;

                var score = ScorePhrase(normalized, normalizedPhrase);
                if (score <= 0)
                    continue;

                if (best is null || score > best.Score)
                {
                    best = new VoiceCommandMatch
                    {
                        Command = command,
                        Score = score,
                        RecognizedPhrase = phrase
                    };
                }
            }
        }

        return best is { Score: >= 0.48 } ? best : null;
    }

    private static double ScorePhrase(string input, string phrase)
    {
        if (input == phrase)
            return 1.0;

        if (input.Contains(phrase, StringComparison.Ordinal))
            return 0.92;

        if (phrase.Contains(input, StringComparison.Ordinal) && input.Length >= 3)
            return 0.85;

        var distance = LevenshteinDistance(input, phrase);
        var maxLen = Math.Max(input.Length, phrase.Length);
        if (maxLen == 0)
            return 0;

        var similarity = 1.0 - (double)distance / maxLen;
        return similarity >= 0.72 ? similarity * 0.9 : 0;
    }

    public static string NormalizeArabic(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length);
        foreach (var ch in text.Trim().ToLowerInvariant())
        {
            if (char.IsWhiteSpace(ch) || ch is '،' or ',' or '.' or '؟' or '!' or '-' or '_')
                continue;

            sb.Append(ch switch
            {
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
                'ى' => 'ي',
                'ة' => 'ه',
                'ؤ' => 'و',
                'ئ' => 'ي',
                _ => ch
            });
        }

        return sb.ToString();
    }

    private static int LevenshteinDistance(string a, string b)
    {
        var n = a.Length;
        var m = b.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}
