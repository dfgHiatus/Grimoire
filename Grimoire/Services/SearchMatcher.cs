using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Grimoire.Services;

/// <summary>Partial, accent- and case-insensitive text matching for library search.</summary>
public static class SearchMatcher
{
    /// <summary>Lower-cases and strips diacritics so "Pokemon" matches "Pokémon".</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string[] Tokenize(string? query) =>
        Normalize(query).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// True when every query token appears somewhere in the given fields, so "fox ki"
    /// matches "Moonlit Fox Avatar" by "Kitsune Works".
    /// </summary>
    public static bool Matches(string[] tokens, params string[] normalizedFields) =>
        tokens.All(t => normalizedFields.Any(f => f.Contains(t, StringComparison.Ordinal)));
}
