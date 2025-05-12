using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvProcessing;
using FuzzySharp;

public class Program
{


    public static int ProcessedCount;

    private static readonly string IntermediateResultsFilename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
        "results",
        $"Intermediate-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv");
    
    

    //
    // public static void NotMain(string[] args)
    // {
    //     var csvFile =
    //         "/workspaces/docr/src/python/mx2/clouddingo/account_cleanup/faster_account/Final-2025-05-08_20-41-34.csv";
    //
    //     var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    //     {
    //         MissingFieldFound = null
    //     };
    //
    //     using var reader = new StreamReader(csvFile);
    //     using var csv = new CsvReader(reader, config);
    //
    //     var accounts = csv.GetRecords<FinalResults>().ToList();
    //     var accountWithMultipleEntries = accounts.AsParallel().GroupBy(x => x.MatchToAccountId).Select(m => new
    //             DuplicateEntries
    //             {
    //                 AccountId = m.Key,
    //                 Count = m.Count(),
    //                 Entries = string.Join(", ",
    //                     m.Select(x => $"({x.AccountId}, ({x.MatchPercentage}))").Distinct().ToList()),
    //                 HighestMatch = m.Max(x => x.MatchPercentage),
    //                 HighestMatchAccountId = m.First(x => x.MatchPercentage == m.Max(y => y.MatchPercentage)).AccountId
    //             }).Where(m => m.Count > 1)
    //         .ToList();
    //     foreach (var account in accountWithMultipleEntries) Logger.AddEntry(account);
    //
    //     Console.WriteLine(
    //         $"Total accounts with multiple entries: {accountWithMultipleEntries.Count} number not exact match  {accountWithMultipleEntries.Where(m => m.HighestMatch != 1).Count()}");
    // }

    
    public static async Task Main()
    {
        var inputFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Account.csv");
        var intermediateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "results",
            $"IntermediateResults-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(intermediateFile));
        var calculator = new MatchCalculator(inputFile, intermediateFile);
        var results = await calculator.Execute();
            
        var accountWithMultipleEntries = results.AsParallel().GroupBy(x => x.MatchToAccountId).Select(m => new
            DuplicateEntries
            {
                AccountId = m.Key,
                Count = m.Count(),
                Entries = string.Join(", ",
                    m.Select(x => $"({x.AccountId}, ({x.MatchPercentage}))").Distinct().ToList()),
                HighestMatch = m.Max(x => x.MatchPercentage),
                HighestMatchAccountId = m.First(x => x.MatchPercentage == m.Max(y => y.MatchPercentage)).AccountId
            }).Where(m => m.Count > 1)
        .ToList();

    
    Console.WriteLine(
        $"Total accounts with multiple entries: {accountWithMultipleEntries.Count} number not exact match  {accountWithMultipleEntries.Where(m => m.HighestMatch != 1).Count()}");


        
        


    }

}

public class DuplicateEntries
{
    public string AccountId { get; set; }
    public int Count { get; set; }
    public string Entries { get; set; }
    public double HighestMatch { get; set; }
    public string HighestMatchAccountId { get; set; }
}