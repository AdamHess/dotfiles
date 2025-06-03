using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AccountDeduplication.DAL;
using MethodTimer;
using Microsoft.EntityFrameworkCore;

namespace CsvProcessing;

public class MatchCalculator
{
    private const double NameWeight = 0.4;
    private const double AddressWeight = 0.6;
    private const double MinimumMatchThreshold = 0.85;
    private static int _recordsProcessed;
    private IBatchLogger<MatchRate> Logger { get; set; }

    private ConcurrentBag<MatchRate> Results { get; set; } = [];
    [Time]
    public async Task<List<MatchRate>> Execute(List<AccountCsvModel> accounts, IBatchLogger<MatchRate> logger)
    {
        Results = [];
        Logger = logger;
        await Parallel.ForEachAsync(accounts,
            async (account, cancelToken) => { await ProcessAccountAsync(account, accounts, cancelToken); });

        _recordsProcessed = 0;
        return [..Results];
    }

    private static MatchRate CalculateMatchPercentage(AccountCsvModel account1, AccountCsvModel account2)
    {

        var nameMatch = TokenSetSimilarity.TokenSetRatio(account1.Name, account2.Name) / 100.0;
        var billingAddressMatch =
            TokenSetSimilarity.TokenSetRatio(account1.BillingStreet, account2.BillingStreet) / 100.0;
        var shippingAddressMatch =
            TokenSetSimilarity.TokenSetRatio(account1.BillingStreet, account2.BillingStreet) / 100.0;
        var otherNameMatch = TokenSetSimilarity.TokenSetRatio(account1.OtherOrgName, account2.OtherOrgName) / 100.0;
        return new MatchRate()
        {
            AccountId1 = account1.Id,
            AccountId2 = account2.Id,
            BillingStreetMatch = billingAddressMatch,
            ShippingAddressMatch = shippingAddressMatch,
            NameMatch = nameMatch,
            OtherNameMatch = otherNameMatch,
            Account1RoleCount = account1.NumberOfRoles,
            Account2RoleCount = account2.NumberOfRoles,

        };
    }

    private  async Task ProcessAccountAsync(AccountCsvModel account1, List<AccountCsvModel> accounts,
        CancellationToken cancelToken)
    {
        cancelToken.ThrowIfCancellationRequested();

        foreach (var account2 in accounts)
        {
            await ExecuteIndividualCompareAsync(account1, account2, cancelToken);
        }
        
        Interlocked.Increment(ref _recordsProcessed);
        Console.WriteLine($"Processed account ID: {account1.Id}. Total processed: {_recordsProcessed}/{accounts.Count}.");
    }

    private  async Task ExecuteIndividualCompareAsync(AccountCsvModel account1, AccountCsvModel account2,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        //skip self compares
        if (account1.Id == account2.Id) return;

        var matchRate = CalculateMatchPercentage(account1, account2);
        if (matchRate.AllNonZeroRecordsGreaterThanMinimumThreshold)
        {
            await Logger.AddEntryAsync(matchRate);
            Results.Add(matchRate);
        }
    }

 
}
