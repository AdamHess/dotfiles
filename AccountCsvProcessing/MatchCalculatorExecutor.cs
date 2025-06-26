using AccountDeduplication.DAL.EF;
using AccountDeduplication.DAL.Models;
using AccountDeduplication.RecordLoggers;
using Microsoft.EntityFrameworkCore;

namespace AccountDeduplication.CalculateMatchRates;

public class MatchCalculatorExecutor(Func<AccountDedupeDb> dbContextFactory)
{


    public async Task CalculateMatchRates(Func<DbContext> dbContextFactory, string groupingPrefix = null)
    {

        await using var matchLogger = new DbLogger<MatchRate>(dbContextFactory);
        await using var statusLogger = new DbLogger<ProcessingStatus>(dbContextFactory);
        var matchCalculator = new MatchCalculator();
        Console.WriteLine("Getting Unprocessed Groups");

        var accountGroups = await GetAccountGroups(groupingPrefix);
        Console.WriteLine("Starting Calculator");

        await matchCalculator.ExecuteAsync(
            matchLogger,
            statusLogger,
            accountGroups);
    }

    private async Task<List<IGrouping<string, Account>>> GetAccountGroups(string groupingPrefix = null, bool skippedProcessedGroups = true)
    {
        await using var db = dbContextFactory();
        var query = db.Accounts.AsQueryable();


        if (groupingPrefix != null)
        {
            query = query.Where(m => m.GroupingCityState.StartsWith(groupingPrefix));

        }

        if (!skippedProcessedGroups)
        {
            query = query.Where(m => !db.ProcessingStatuses.Any(n => n.GroupId == m.GroupingCityState));
        }

        var results = query.GroupBy(m => m.GroupingCityState)
            .ToList()
            .OrderByDescending(m => m.Count())
            .ToList();

        Console.WriteLine($"Total # of Groups {results.Count}");
        return results;

    }

}