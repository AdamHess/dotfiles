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
         var intermediateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IntermediateResults.csv");
        //     $"IntermediateResults-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv");
        // Directory.CreateDirectory(Path.GetDirectoryName(intermediateFile));
        // var calculator = new MatchCalculator(inputFile, intermediateFile);
        var results = MatchCalculator.LoadCsv<IntermediateResults>(intermediateFile);
        var accounts = MatchCalculator.LoadCsv<AccountCsvModel>(inputFile);

        var groupedResults = results
            .GroupBy(r => r.MatchToAccountId)
            .Select(g =>
                new
                {
                    GroupAccountId = g
                        .OrderByDescending(r => r.MatchPercentage)
                        .ThenByDescending(r => r.RoleCount)
                        .First().AccountId,
                    AccountIds = g.Select(r => r.MatchToAccountId)
                })
            .SelectMany(gr => gr.AccountIds.Select(id => new FinalAssingnments
            {
                GroupAccountId = gr.GroupAccountId,
                AccountId = id
            }))
            .DistinctBy(x => x.AccountId)
            .ToList();
        
        var accountLookup = accounts.ToDictionary(m => m.Id, m => m);
        foreach (var result in groupedResults)
        {
            var groupAccount = accountLookup[result.GroupAccountId];
            var original = accountLookup[result.AccountId];
            if (groupAccount.Name != original.Name)
            {
                result.NewName = groupAccount.Name;
                result.OldName = original.Name;
            }

            if (groupAccount.BillingStreet != original.BillingStreet)
            {
                result.NewStreet = groupAccount.BillingStreet;
                result.OldStreet = original.BillingStreet;
            }
        }
        var finalresultsFile  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "results",
            $"finalresults-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv");
        await using var csvLogger = new CsvLogger<FinalAssingnments>(finalresultsFile);
        var fileResults = groupedResults.Where(m => m.NewName != m.OldName || m.NewStreet != m.OldStreet);
        foreach (var groupedResult in fileResults)
        {
            await csvLogger.AddEntryAsync(groupedResult);
        }
    }

}

public class FinalAssingnments
{
    public string GroupAccountId { get; set; }
    public string AccountId { get; set; }
    
    public string NewName { get; set; }
    
    public string OldName { get; set; }
    
    public string NewStreet { get; set;  }
    public string OldStreet { get; set; }
    
    
}