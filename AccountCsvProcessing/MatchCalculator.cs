namespace CsvProcessing;

public class MatchCalculator(string outputFile)
{
    private const double NameWeight = 0.4;
    private const double AddressWeight = 0.6;
    private const double MinimumMatchThreshold = 0.85;
    private static int _recordsProcessed;

    public async Task<List<IntermediateResults>> Execute(List<AccountCsvModel> accounts)
    {
        await using (var logger = new CsvLogger<IntermediateResults>(outputFile))
        {
        

            await Parallel.ForEachAsync(accounts,
                async (account, cancelToken) => { await ProcessAccountAsync(account, accounts, logger, cancelToken); });

            _recordsProcessed = 0;
        }
        

        return CsvBinder.LoadCsv<IntermediateResults>(outputFile);
    }

    private static double CalculateMatchPercentage(AccountCsvModel account1, AccountCsvModel account2)
    {
        
        var nameMatch = TokenSetSimilarity.TokenSetRatio(account1.Name, account2.Name) / 100.0;    

        var addressMatch = TokenSetSimilarity.TokenSetRatio(account1.BillingStreet, account2.BillingStreet) / 100.0;

        return NameWeight * nameMatch + AddressWeight * addressMatch;
    }

    private static async Task ProcessAccountAsync(AccountCsvModel account1, List<AccountCsvModel> accounts,
        CsvLogger<IntermediateResults> logger, CancellationToken cancelToken)
    {
        cancelToken.ThrowIfCancellationRequested();

        foreach (var account2 in accounts)
        {
            await ExecuteIndividualCompareAsync(account1, account2, logger, cancelToken);
        }
        
        Interlocked.Increment(ref _recordsProcessed);
        Console.WriteLine($"Processed account ID: {account1.Id}. Total processed: {_recordsProcessed}/{accounts.Count}.");
    }

    private static async Task ExecuteIndividualCompareAsync(AccountCsvModel account1, AccountCsvModel account2,
        CsvLogger<IntermediateResults> logger, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        //skip self compares
        if (account1.Id == account2.Id) return;

        var matchPercentage = CalculateMatchPercentage(account1, account2);
        if (matchPercentage >= MinimumMatchThreshold)
        {
            await logger.AddEntryAsync(new IntermediateResults
            {
                AccountId1 = account1.Id,
                AccountId2 = account2.Id,
                MatchPercentage = matchPercentage,
                RoleCount = account1.NumberOfRoles
            });
        }
    }

 
}
