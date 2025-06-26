using AccountDeduplication.DAL.EF;
using AccountDeduplication.DAL.Models;
using AccountDeduplication.RecordLoggers;

namespace AccountDeduplication.CalculateMatchRates;

public class Program
{

    public static void Main()
    {

    }
    public static async Task CalculateMatchRates(string groupingPrefix = null)
    {
        var matchLogger = new DbLogger<MatchRate>(() => new AccountDedupeDb());
        var statusLogger = new DbLogger<ProcessingStatus>(() => new AccountDedupeDb());
        var matchCalculator = new MatchCalculator();
        Console.WriteLine("Getting Unprocessed Groups");

        var accountGroups = await GetUnprocessedGroups(groupingPrefix);
        Console.WriteLine("Starting Calculator");

        await matchCalculator.ExecuteAsync(
            matchLogger,
            statusLogger,
            accountGroups);
    }

    private static async Task<List<IGrouping<string, Account>>> GetUnprocessedGroups(string groupingPrefix = null)
    {
        await using var db = new AccountDedupeDb();

        List<IGrouping<string, Account>> groups;
        if (groupingPrefix != null)
        {
            groups = db.Accounts.Where(m => m.GroupingCityState.StartsWith(groupingPrefix))
                .GroupBy(a => a.GroupingCityState)
                .ToList();
        }
        else
        {
            groups = db.Accounts
                .GroupBy(a => a.GroupingCityState)
                .ToList();
        }

        Console.WriteLine($"Total # of Groups {groups.Count}");
        return groups.OrderByDescending(m => m.Count())
            .ToList();

    }

}