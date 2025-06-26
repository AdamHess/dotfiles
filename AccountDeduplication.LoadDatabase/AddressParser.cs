using System.Text;
using System.Text.RegularExpressions;

namespace AccountDeduplication.LoadDatabase;

public static class AddressParser
{
    // Dictionary of street suffix abbreviations and their full forms
    private static readonly (string regex, string mapping)[] AbrvMap =
    [
        ("st", "Street"),
        ("ave", "Avenue"),
        ("rd", "Road"),
        ("dr", "Drive"),
        ("blvd", "Boulevard"),
        ("ln", "Lane"),
        ("ct", "Court"),
        ("way", "Way"),
        ("pl", "Place"),
        ("ter", "Terrace"),
        ("terr", "Terrace"),
        ("pkwy", "Parkway"),
        ("cir", "Circle"),
        ("hwy", "Highway"),
        ("trl", "Trail"),
        ("sq", "Square"),
        ("cres", "Crescent"),
        ("expy", "Expressway"),
        ("pass", "Pass"),
        ("pt", "Point"),
        ("bnd", "Bend"),
        ("aly", "Alley"),
        ("blfs", "Bluffs"),
        ("brg", "Bridge"),
        ("byp", "Bypass"),
        ("cswy", "Causeway"),
        ("ctr", "Center"),
        ("cmn", "Common"),
        ("cor", "Corner"),
        ("cv", "Cove"),
        ("crst", "Crest"),
        ("xing", "Crossing"),
        ("frst", "Forest"),
        ("fwy", "Freeway"),
        ("gdn", "Garden"),
        ("gdns", "Gardens"),
        ("gtwy", "Gateway"),
        ("gln", "Glen"),
        ("grn", "Green"),
        ("grv", "Grove"),
        ("hbr", "Harbor"),
        ("hvn", "Haven"),
        ("hts", "Heights"),
        ("holw", "Hollow"),
        ("isle", "Isle"),
        ("jct", "Junction"),
        ("ky", "Key"),
        ("knl", "Knoll"),
        ("lk", "Lake"),
        ("lndg", "Landing"),
        ("loop", "Loop"),
        ("mall", "Mall"),
        ("mnr", "Manor"),
        ("mdw", "Meadow"),
        ("mdws", "Meadows"),
        ("mews", "Mews"),
        ("ml", "Mill"),
        ("msn", "Mission"),
        ("mtwy", "Motorway"),
        ("mnt", "Mount"),
        ("mtn", "Mountain"),
        ("orch", "Orchard"),
        ("oval", "Oval"),
        ("park", "Park"),
        ("path", "Path"),
        ("pike", "Pike"),
        ("pne", "Pine"),
        ("plz", "Plaza"),
        ("prk", "Park"),
        ("psge", "Passage"),
        ("rdg", "Ridge"),
        ("riv", "River"),
        ("row", "Row"),
        ("run", "Run"),
        ("shl", "Shoal"),
        ("shr", "Shore"),
        ("skwy", "Skyway"),
        ("spg", "Spring"),
        ("spgs", "Springs"),
        ("spur", "Spur"),
        ("sta", "Station"),
        ("strm", "Stream"),
        ("vly", "Valley"),
        ("via", "Via"),
        ("vw", "View"),
        ("vlg", "Village"),
        ("vl", "Ville"),
        ("vis", "Vista"),
        ("walk", "Walk"),
        ("trce", "Trace"),
        ("est", "Estate"),
        ("ests", "Estates"),
        ("cyn", "Canyon"),
        ("prom", "Promenade"),
        ("bay", "Bay"),
        ("trwy", "Throughway"),
        ("dl", "Dale"),
        ("hlw", "Hollow"),
        ("gate", "Gate"),
        (@"n\.?", "North"),
        (@"s\.?", "South"),
        (@"e\.?", "East"),
        (@"w\.?", "West"),
        (@"n\.?e\.?", "Northeast"),
        (@"s\.?e\.?", "Southeast"),
        (@"n\.?w\.?", "Northwest"),
        (@"s\.?w\.?", "Southwest"),
        ("apt", "Apartment"),
        ("ste", "Suite"),
        ("bldg", "Building"),
        ("fl", "Floor"),
        ("rm", "Room"),
        (@"po\s*box", "PO Box"),
        (@"post\s+office\s+box", "PO Box"),
        //units
        ("apt", "Apartment"),
        ("ste", "Suite"),
        ("unit", "Unit"),
        ("bldg", "Building"),
        ("fl", "Floor"),
        ("rm", "Room"),
        ("#", "Number"),
        ("no", "Number"),
        ("dept", "Department"),
        ("ofc", "Office"),
        ("space", "Space"),
        ("twr", "Tower"),
        ("lbby", "Lobby"),
        ("ste", "Suite"),
        ("lot", "Lot"),
        ("bsmt", "Basement"),
        ("rear", "Rear"),
        ("frnt", "Front"),
        ("lowr", "Lower"),
        ("uppr", "Upper"),
        ("mz", "Mezzanine"),
        ("ph", "Penthouse"),
        ("pnth", "Penthouse"),
        ("gar", "Garage"),
        ("slip", "Slip"),
        ("pier", "Pier"),
        ("dock", "Dock"),
        ("mezz", "Mezzanine"),
        ("annex", "Annex"),
        ("subu", "Suburban Unit")
    ];
    private static readonly Dictionary<Regex, string> AbbreviationPatterns = AbrvMap
        .ToDictionary(
            kvp => new Regex(@"\b" + kvp.regex + @"\.?\b\.?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            kvp => kvp.mapping
        );

    private static readonly string[] UnitTypes =
    [

        "Apartment",
        "Suite",
        "Unit",
        "Building",
        "Floor",
        "Room",
        "Number",
        "Department",
        "Office",
        "Space",
        "Tower",
        "Lobby",
        "Lot",
        "Basement",
        "Rear",
        "Mezzanine",
        "Penthouse",
        "Garage",
        "Slip",
        "Pier",
        "Dock",
        "Annex",
        "Suburban Unit"
    ];
    private static readonly Regex AddressPartsRegex = new(
        $@"^\s*
    (?<house>\d+)\s+                                # House number
    (?<street>.*?)                                   # Non-greedy capture of street name
    (?:\s+(?:{string.Join("|", UnitTypes.Select(Regex.Escape))})\s*
    (?<unit>[A-Za-z0-9\-]+))?                        # Optional unit with dash support
    \s*$
    ",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace
    );



    public static string NormalizeAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return "";
        //remove line endings
        string normalized = Regex.Replace(address, @"[\r\n,]+", " ");
        // remove extra spaces
        normalized = Regex.Replace(normalized, @"\s+", " ");

        normalized = normalized.Normalize(NormalizationForm.FormC).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return "";

        foreach (var (pattern, replacement) in AbbreviationPatterns)
        {

            normalized = pattern.Replace(normalized, replacement);
        }

        return normalized.ToLowerInvariant();

    }

    public static (string houseNumber, string streetAddress, string unit) GetAddressParts(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return (string.Empty, string.Empty, string.Empty);
        var normalize = NormalizeAddress(address);
        var match = AddressPartsRegex.Match(normalize);
        if (match.Success)
        {
            return (
                match.Groups["house"].Value,
                match.Groups["street"].Value.Trim(),
                match.Groups["unit"].Success ? match.Groups["unit"].Value.Trim() : ""
            );
        }

        return (string.Empty, string.Empty, string.Empty);
    }
}