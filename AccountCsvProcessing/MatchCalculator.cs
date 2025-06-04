using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using AccountDeduplication.DAL;
using AccountDeduplication.RecordLoggers;
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
    public async Task<List<MatchRate>> ExecuteAsync(List<Account> accounts, IBatchLogger<MatchRate> logger)
    {
        Results = [];
        Logger = logger;

        var groups = accounts.GroupBy(a => a.Grouping).ToList();
        int totalGroups = groups.Count;
        int groupsProcessed = 0;

        await Parallel.ForEachAsync(
            groups,
            new ParallelOptions { MaxDegreeOfParallelism = 8 }, // Tune as needed
            async (group, groupCancelToken) =>
            {
                var groupList = group.ToList();
                int count = groupList.Count;
                if (count < 2) return;
                var groupStopwatch = Stopwatch.StartNew();

                for (int i = 0; i < count; i++)
                {
                    var account1 = groupList[i];
                    for (int j = i + 1; j < count; j++)
                    {
                        var account2 = groupList[j];
                        await ExecuteIndividualCompareAsync(account1, account2, groupCancelToken);
                    }
                }

                groupStopwatch.Stop();
                int processed = Interlocked.Increment(ref groupsProcessed);
                Console.WriteLine(
                    $"Completed processing group '{group.Key}' ({count} accounts). " +
                    $"Group {processed} of {totalGroups}. " +
                    $"Group time: {groupStopwatch.ElapsedMilliseconds} ms.");
            });

        _recordsProcessed = 0;
        return [..Results];
    }

    private static MatchRate CalculateMatchPercentage(Account account1, Account account2)
    {
        var nameMatch = TokenSetSimilarity.TokenSetRatio(account1.Name, account2.Name) / 100.0;
        var billingAddressMatch =
            TokenSetSimilarity.TokenSetRatio(account1.BillingStreet, account2.BillingStreet) / 100.0;
        var shippingAddressMatch =
            TokenSetSimilarity.TokenSetRatio(account1.ShippingStreet, account2.ShippingStreet) / 100.0;
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

    private async Task ProcessAccountAsync(Account account1, List<Account> accounts, CancellationToken cancelToken)
    {
        cancelToken.ThrowIfCancellationRequested();

        var tasks = new List<Task>();
        foreach (var account2 in accounts)
        {
            tasks.Add(ExecuteIndividualCompareAsync(account1, account2, cancelToken));
        }
        await Task.WhenAll(tasks);

        Interlocked.Increment(ref _recordsProcessed);
        Console.WriteLine($"Processed account ID: {account1.Id}. Total processed: {_recordsProcessed}/{accounts.Count}.");
    }

    private  async Task ExecuteIndividualCompareAsync(Account account1, Account account2,
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
