using System.Text;
using System.Text.RegularExpressions;

namespace AccountDeduplication.CalculateMatchRates;

public static class AddressParser
{
    private static readonly List<(Regex Pattern, string Replacement)> AbbreviationReplacements = new()
    {
        (new Regex(@"\bSt\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Street"),
        (new Regex(@"\bAve\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Avenue"),
        (new Regex(@"\bBlvd\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Boulevard"),
        (new Regex(@"\bRd\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Road"),
        (new Regex(@"\bLn\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Lane"),
        (new Regex(@"\bDr\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Drive"),
        (new Regex(@"\bCt\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Court"),
        (new Regex(@"\bPl\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Place"),
        (new Regex(@"\bApt\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Apartment"),
        (new Regex(@"\bSte\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Suite"),
        (new Regex(@"\bN\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "North"),
        (new Regex(@"\bS\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "South"),
        (new Regex(@"\bE\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "East"),
        (new Regex(@"\bW\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "West"),
        (new Regex(@"\bN\.?E\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Northeast"),
        (new Regex(@"\bS\.?E\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Southeast"),
        (new Regex(@"\bN\.?W\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Northwest"),
        (new Regex(@"\bS\.?W\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Southwest"),
        (new Regex(@"\bPkwy\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Parkway"),
        (new Regex(@"\bHwy\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Highway"),
        (new Regex(@"\bCir\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Circle"),
        (new Regex(@"\bTer\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Terrace"),
        (new Regex(@"\bBldg\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Building"),
        (new Regex(@"\bFl\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Floor"),
        (new Regex(@"\bRm\.?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Room"),
        (new Regex(@"\bP\.?O\.?\s*Box\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "PO Box"),
        (new Regex(@"\bPost\s+Office\s+Box\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "PO Box")
    };

    public static string NormalizeAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return "";

        string normalized = Regex.Replace(address, @"[\r\n,]+", " ");
        normalized = Regex.Replace(normalized, @"\s+", " ");
        normalized = normalized.Normalize(NormalizationForm.FormC).Trim();

        foreach (var (pattern, replacement) in AbbreviationReplacements)
        {
            normalized = pattern.Replace(normalized, replacement);
        }

        return normalized;
    }
}