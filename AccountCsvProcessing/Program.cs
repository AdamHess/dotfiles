using AccountDeduplication.DAL.EF;
using AccountDeduplication.DAL.Models;
using AccountDeduplication.RecordLoggers;
using Microsoft.EntityFrameworkCore;

namespace AccountDeduplication.CalculateMatchRates;

public class Program
{
    public static async Task Main()
    {
        await CalculateMatchPercentages();

    }

    private static async Task CalculateMatchPercentages()
    {
        var accounts = await LoadAccountsFromDb();
        var matchLogger = new DbLogger<MatchRate>(() => new AccountDedupeDb());
        var statusLogger = new DbLogger<ProcessingStatus>(() => new AccountDedupeDb());
        var matchCalculator = new MatchCalculator();
        Console.WriteLine("Starting Calculator");
        await matchCalculator.ExecuteAsync(
            matchLogger,
            statusLogger);
    }

    private static async Task<List<Account>> LoadAccountsFromDb()
    {
        Console.WriteLine("Loading Accounts from DB");
        await using var db = new AccountDedupeDb();
        var accounts = await db.Accounts.Where(m => !string.IsNullOrWhiteSpace(m.GroupingCityState)).AsNoTracking().ToListAsync();
        return accounts;
    }


}