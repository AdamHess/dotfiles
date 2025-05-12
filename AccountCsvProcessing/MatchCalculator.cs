using System.Globalization;
using CsvHelper;
using FuzzySharp;

namespace CsvProcessing;

public class MatchCalculator(string inputFile, string outputFile)
{
    
    private const double NameWeight = 0.4;
    private const double AddressWeight = 0.6;
    private const double MinimumMatchThreshold = 0.85;
    private static int _recordsProcessed;

    public async Task<List<IntermediateResults>> Execute()
    {
        var logger = new CsvLogger<IntermediateResults>(outputFile);
        var accounts = LoadCsv<AccountCsvModel>(inputFile);

        await Parallel.ForEachAsync(accounts, new ParallelOptions(), async ( account, cancelToken) => await ProcessAccountAsync(account, accounts, logger, cancelToken));
        _recordsProcessed = 0;
        
        return LoadCsv<IntermediateResults>(outputFile);
    }

    private static List<T> LoadCsv<T>(string inputFile) where T : class
    {
        using var reader = new StreamReader(inputFile);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        return csv.GetRecords<T>().ToList();
    }
        
    private static double CalculateMatchPercentage(AccountCsvModel account1, AccountCsvModel account2)
    {
        var nameMatch = Fuzz.TokenSetRatio(account1.Name, account2.Name) / 100.0;
        
        var account1Address = account1.BillingStreet;
        var account2Address = account2.BillingStreet;

        if (string.IsNullOrEmpty(account1Address) || string.IsNullOrEmpty(account2Address)) return NameWeight*nameMatch;

        var addressMatchPercentage = Fuzz.TokenSetRatio(account1Address, account2Address) / 100.0;

        return NameWeight * nameMatch + AddressWeight * addressMatchPercentage;
    }
    
    
    private static async Task ProcessAccountAsync(AccountCsvModel account1, List<AccountCsvModel> accounts,
        CsvLogger<IntermediateResults> logger, CancellationToken cancelToken)
    {
        cancelToken.ThrowIfCancellationRequested();
        await Parallel.ForEachAsync(accounts, new ParallelOptions(),async (account2, cancellationToken)=> await ExecuteIndividualCompareAsync(account1, account2, logger, cancellationToken));
        Interlocked.Increment(ref _recordsProcessed);
        Console.WriteLine($"Processed account ID: {account1.Id}. Total processed: {_recordsProcessed}/{accounts.Count}.");
    }

    private static async Task ExecuteIndividualCompareAsync(AccountCsvModel account1, AccountCsvModel account2,
        CsvLogger<IntermediateResults> csvLogger, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (account1.Id == account2.Id) return;
        var matchPercentage = CalculateMatchPercentage(account1, account2);
        if (matchPercentage >= MinimumMatchThreshold)
        {
            await csvLogger.AddEntryAsync(new IntermediateResults
            {
                AccountId = account1.Id,
                MatchToAccountId = account2.Id,
                MatchPercentage = matchPercentage,
            }, cancellationToken);
        }
    }
}