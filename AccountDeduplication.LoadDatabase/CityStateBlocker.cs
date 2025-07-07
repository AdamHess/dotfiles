using FuzzySharp;
using Phonix;
using System.Text.RegularExpressions;

namespace AccountDeduplication.LoadDatabase;

public static class CityStateBlocker
{
    private static readonly DoubleMetaphone DoubleMetaphone = new();


    // Normalize: lowercase, remove non-letters
    public static string Normalize(string input)
    {
        return string.IsNullOrWhiteSpace(input)
            ? string.Empty
            : Regex.Replace(input.ToLowerInvariant()
                    .Normalize(),
                "[^a-z]",
                "",
                RegexOptions.Compiled);
    }

    // Phonetic encoding
    private static string GetPhoneticCode(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        var norm = Normalize(input);
        return DoubleMetaphone.BuildKey(norm);
    }

    // Token set hash: sort, deduplicate
    public static string GetTokenSetKey(string input)
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
    public static string GetGroupingKey(
        string accountType,
        string house,
        string city,
        string state,
        string unit)
    {
        if (!CityStateListMap.StateMap.TryGetValue(state.ToLowerInvariant(), out var normalizedState))
        {
            var closestMatch = Process.ExtractOne(state.ToLowerInvariant(), CityStateListMap.LowercasedStateNames);
            if (closestMatch.Score > 85)
            {
                return null; // No good match found 
            }

        }
        var cityCode = GetGroupingPair(city);
        var stateCode = GetTokenSetKey(normalizedState);

        return $"{accountType}|{stateCode}|{cityCode}|{house}|{unit}";
    }


    public static string GetGroupingPair(string toPair)
    {


        var cityPhonetic = GetPhoneticCode(toPair);
        var cityTokens = GetTokenSetKey(toPair);
        return $"{cityPhonetic}-{cityTokens}";
    }


}