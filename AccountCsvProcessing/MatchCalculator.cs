using AccountDeduplication.DAL.Models;
using AccountDeduplication.RecordLoggers;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AccountDeduplication.CalculateMatchRates;

public class MatchCalculator
{
    private static Random Random { get; } = new();
    private IBatchLogger<MatchRate> Logger { get; set; }

    public async Task ExecuteAsync(
         IBatchLogger<MatchRate> logger,
         IBatchLogger<ProcessingStatus> statusLogger,
         List<IGrouping<string, Account>> accountGroups)
    {
        Logger = logger;
        Console.WriteLine("Loading Records...");

        PrintGroupSummary(accountGroups);

        var totalGroups = accountGroups.Count;

        // Prepare group info for progress tracking
        Console.WriteLine("Creating Count Lookup Dictionary");
        var groupPairCounts = accountGroups.ToDictionary(
            g => g.Key,
            g => (long)g.Count() * (g.Count() - 1) / 2
        );
        var groupProcessedPairs = new ConcurrentDictionary<string, long>();
        var groupStopwatches = new ConcurrentDictionary<string, Stopwatch>();
        var groupStatuses = new ConcurrentDictionary<string, ProcessingStatus>();
        Console.WriteLine("Creating Status Lookup Dictionary");
        foreach (var group in accountGroups)
        {
            groupStopwatches[group.Key] = Stopwatch.StartNew();
            groupStatuses[group.Key] = new ProcessingStatus
            {
                AccountsInGroup = group.Count(),
                GroupId = group.Key,
                ProcessedAt = DateTime.UtcNow
            };
        }

        // Flatten all pairs across all groups
        var allPairs = StreamAllAccountPairs(accountGroups);

        // For global progress
        long totalPairs = groupPairCounts.Values.Sum();
        long pairsProcessed = 0;
        var globalStopwatch = Stopwatch.StartNew();
        int groupsProcessed = 0;
        int entriesSaved = 0;
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
                    Interlocked.Increment(ref entriesSaved);
                }

                // Track group progress
                var processed = groupProcessedPairs.AddOrUpdate(groupKey, 1, (_, v) => v + 1);
                var globalProcessed = Interlocked.Increment(ref pairsProcessed);

                if (processed == groupPairCounts[groupKey])
                {
                    // Group done
                    groupStopwatches[groupKey].Stop();
                    int groupIndex = Interlocked.Increment(ref groupsProcessed);
                    var status = groupStatuses[groupKey];
                    status.ProcessingTime = groupStopwatches[groupKey].Elapsed;
                    await statusLogger.AddEntryAsync(status);
                    Console.WriteLine(
                        $"[DONE] Completed group '{groupKey}' ({status.AccountsInGroup} accounts, {groupPairCounts[groupKey]} pairs). " +
                        $"Group {groupIndex} of {totalGroups}. " +
                        $"Elapsed: {status.ProcessingTime.TotalMilliseconds} ms.");
                }
                else
                {
                    if (processed % 1_000_000 == 0)
                    {
                        // Estimate global time left
                        var globalElapsed = globalStopwatch.Elapsed.TotalSeconds;
                        var globalAvgPerPair = globalElapsed / globalProcessed;
                        var globalPairsLeft = totalPairs - globalProcessed;
                        var globalEstSecondsLeft = (long)(globalPairsLeft * globalAvgPerPair);

                        // Estimate local (group) time left
                        var groupElapsed = groupStopwatches[groupKey].Elapsed.TotalSeconds;
                        var groupAvgPerPair = groupElapsed / processed;
                        var groupPairsLeft = groupPairCounts[groupKey] - processed;
                        var groupEstSecondsLeft = (long)(groupPairsLeft * groupAvgPerPair);

                        Console.WriteLine(
                            $"[PROGRESS] Group '{groupKey}': Compared {processed}/{groupPairCounts[groupKey]} pairs, " +
                            $"est. {FormatTime(groupEstSecondsLeft)} left for group. " +
                            $"[GLOBAL] {globalProcessed}/{totalPairs} pairs, est. {FormatTime(globalEstSecondsLeft)} left total.");
                    }
                }
            });

        globalStopwatch.Stop();
        Console.WriteLine($"[INFO] All groups processed. Records Saved {entriesSaved}");
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


    private static void PrintGroupSummary(IList<IGrouping<string, Account>> groups)
    {

        var top10 = groups.Take(10).ToList();
        for (var i = 0; i < top10.Count; i++)
        {
            var group = top10[i];
            Console.WriteLine($"{i}. {group.Key}: {group.Count()}");
        }
    }

    // Streams all pairs across all groups, yielding (groupKey, account1, account2)
    private static IEnumerable<(string Key, Account account1, Account account2)> StreamAllAccountPairs(
        List<IGrouping<string, Account>> groups)
    {
        foreach (var group in groups)
        {
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
                }
            }
        }
    }

    private static MatchRate CalculateMatchPercentage(Account account1, Account account2)
    {
        var nameMatch = TokenSetSimilarity.TokenSetRatio(account1.Name, account2.Name) / 100.0;

        var billingAddressMatch =
            TokenSetSimilarity.TokenSetRatio(account1.BillingStreetNormalized, account2.BillingStreetNormalized) / 100.0;
        var shippingAddressMatch =
            TokenSetSimilarity.TokenSetRatio(account1.ShippingStreetNormalized, account2.ShippingStreetNormalized) / 100.0;
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
