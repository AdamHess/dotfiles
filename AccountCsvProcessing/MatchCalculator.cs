using AccountDeduplication.DAL.EF;
using AccountDeduplication.DAL.Models;
using AccountDeduplication.IterableExtensions;
using AccountDeduplication.RecordLoggers;
using Microsoft.EntityFrameworkCore;

namespace AccountDeduplication.CalculateMatchRates;

public class MatchCalculator
{

    private IBatchLogger<MatchRate> Logger { get; set; }

    private static async Task<bool> AlreadyProcessed(string groupKey)
    {
        await using var db = new AccountDedupeDb();
        return await db.ProcessingStatuses.AnyAsync(m => m.GroupId == groupKey);
    }

    public async Task ExecuteAsync(
        IBatchLogger<MatchRate> logger,
        IBatchLogger<ProcessingStatus> statusLogger)
    {
        Logger = logger;
        var groups = await GetUnprocessedGroups();

        await PrintGroupSummary(groups);

        Console.WriteLine("Press Any Key To continue");
        Console.ReadKey();




        // Flatten all pairs across all groups
        var allPairs = StreamAllAccountPairs(groups, statusLogger);

        await Parallel.ForEachAsync(
            allPairs,
            new ParallelOptions { MaxDegreeOfParallelism = 64 },
            async (pair, ct) =>
            {
                var (groupKey, account1, account2) = pair;
                var matchRate = CalculateMatchPercentage(account1, account2);
                if (matchRate.AllNonZeroRecordsGreaterThanMinimumThreshold)
                {
                    await Logger.AddEntryAsync(matchRate);
                }
            });

        Console.WriteLine("[INFO] All groups processed.");
    }

    private static string FormatTime(long seconds)
    {
        if (seconds >= 86400)
        {
            long days = seconds / 86400;
            long hours = (seconds % 86400) / 3600;
            return $"{days}d {hours}h";
        }
        if (seconds >= 3600)
        {
            long hours = seconds / 3600;
            long minutes = (seconds % 3600) / 60;
            return $"{hours}h {minutes}m";
        }
        if (seconds > 120)
            return $"{(seconds / 60.0):0.0} min";
        return $"{seconds} sec";
    }

    private static async Task<IAsyncReadOnlyList<IGrouping<string, Account>>> GetUnprocessedGroups()
    {
        await using var db = new AccountDedupeDb();
        return db.Accounts.GroupBy(a => $"{a.GroupingCityState}_{a.BillingUnit}__{a.BillingHouseNumber}"
        )
            .Where(m => m.Count() > 1 && !db.ProcessingStatuses.Select(m => m.GroupId).Contains(m.Key))
            .OrderByDescending(m => m.Count())
            .AsAsyncEnumerable()
            .AsAsyncReadOnlyList();




    }

    private static async Task PrintGroupSummary(IAsyncReadOnlyList<IGrouping<string, Account>> groups)
    {

        var top10 = await groups.Take(10).ToListAsync();
        for (var i = 0; i < Math.Min(10, top10.Count); i++)
        {
            var group = top10[i];
            Console.WriteLine($"{i}. {group.Key}: {group.Count()}");
        }
    }

    // Streams all pairs across all groups, yielding (groupKey, account1, account2)
    private static async IAsyncEnumerable<(string groupKey, Account, Account)> StreamAllAccountPairs(
        IAsyncReadOnlyList<IGrouping<string, Account>> groups, IBatchLogger<ProcessingStatus> statusLogger)
    {
        await foreach (var group in groups)
        {
            if (await AlreadyProcessed(group.Key)) continue;
            var groupList = group.ToList();
            int count = groupList.Count;
            if (count < 2) continue;
            var start = DateTime.Now;
            for (var account1Index = 0; account1Index < count; account1Index++)
            {
                var account1 = groupList[account1Index];
                for (var account2Index = account1Index + 1; account2Index < count; account2Index++)
                {
                    var account2 = groupList[account2Index];
                    yield return (group.Key, account1, account2);
                    await Task.Yield();
                }
            }

            await statusLogger.AddEntryAsync(new ProcessingStatus()
            {
                GroupId = group.Key,
                ProcessedAt = start,
                ProcessingTime = DateTime.Now - start
            });
            Console.WriteLine($"Finished Streaming Group {group.Key} with {count} records with ({count ^ 2}) compares {FormatTime((DateTime.Now - start).Seconds)}");

        }
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
}
