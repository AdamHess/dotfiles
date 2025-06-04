using System.Text.RegularExpressions;
using Phonix;

namespace CsvProcessing;

public static class CityStateBlocker
{
    private static readonly DoubleMetaphone Metaphone = new();

    // Normalize: lowercase, remove non-letters
    private static string Normalize(string input)
    {
        return Regex.Replace(input.ToLowerInvariant(), "[^a-z]", "");
    }

    // Phonetic encoding
    private static string GetPhoneticCode(string input)
    {
        
        var norm = Normalize(input);
        return Metaphone.BuildKey(norm);
    }

    // Token set hash: sort, deduplicate
    public static string GetTokenSetKey(string input)
    {
        var tokens = Regex.Matches(input.ToLowerInvariant(), "[a-z]+")
            .Cast<Match>()
            .Select(m => m.Value)
            .Distinct()
            .OrderBy(x => x);
        return string.Join("", tokens);
    }

    // Final blocking key using both city and state
    public static string GetGroupingKey(string city, string state)
    {
        if (city == null || state == null)
            return null;
        var cityPhonetic = GetPhoneticCode(city);
        var cityTokens = GetTokenSetKey(city);

        var statePhonetic = GetPhoneticCode(state);
        var stateTokens = GetTokenSetKey(state);

        return $"{cityPhonetic}-{cityTokens}__{statePhonetic}-{stateTokens}";
    }


}