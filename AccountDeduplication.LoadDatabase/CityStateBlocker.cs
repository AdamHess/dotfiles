using FuzzySharp;
using Phonix;
using System.Text.RegularExpressions;

namespace AccountDeduplication.LoadDatabase;

public static class CityStateBlocker
{
    private static readonly DoubleMetaphone DoubleMetaphone = new();


    // Normalize: lowercase, remove non-letters
    private static string Normalize(string input)
    {
        return Regex.Replace(input.ToLowerInvariant().Normalize(), "[^a-z]", "", RegexOptions.Compiled);
    }

    // Phonetic encoding
    private static string GetPhoneticCode(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        var norm = Normalize(input);
        return DoubleMetaphone.BuildKey(norm);
    }

    // Token set hash: sort, deduplicate
    public static string GetTokenSetKey(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        var tokens = Regex.Matches(input.ToLowerInvariant(), "[a-z]")
            .Cast<Match>()
            .Select(m => m.Value)
            .Distinct()
            .OrderBy(x => x).ToList();
        return string.Join("", tokens);
    }

    // Final blocking key using both city and state
    public static string? GetGroupingKey(
        string? city,
        string? state)
    {
        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state))
            return null;

        if (!CityStateListMap.StateMap.TryGetValue(state.ToLowerInvariant(), out var normalizedState))
        {
            var closestMatch = Process.ExtractOne(state.ToLowerInvariant(), CityStateListMap.LowercasedStateNames);
            if (closestMatch.Score < 85)
            {
                return null; // No good match found 
            }
            normalizedState = closestMatch.Value;
        }
        var cityPrefix = GetPhoneticCode(city);
        var stateSuffix = GetPhoneticCode(normalizedState);

        return $"{cityPrefix}__{stateSuffix}";
    }

    public static string GetGroupingPair(string? toPair)
    {


        var cityPhonetic = GetPhoneticCode(toPair);
        var cityTokens = GetTokenSetKey(toPair);
        return $"{cityPhonetic}-{cityTokens}";
    }


}