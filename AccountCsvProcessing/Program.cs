using System.Globalization;
using System.Security.AccessControl;
using AccountDeduplication.CsvModels;
using AccountDeduplication.DAL;
using AccountDeduplication.RecordLoggers;
using CsvHelper;
using CsvHelper.Configuration;
using EFCore.BulkExtensions;
using MethodTimer;
using Microsoft.EntityFrameworkCore;

namespace CsvProcessing;

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("""
                          Select from the following options:
                          1. Process existing accounts stored in database
                          2. Load account.csv into database
                          """);
        var input = Console.ReadKey();
   
        switch (input.KeyChar)
        {
            case '1':
                await CalculateMatchPercentages();
                break;
            case '2':
                await LoadDatabaseWithAccounts();
                break;
            default:
                Console.WriteLine("Invalid input");
                break;
        }

    }

    private static async Task CalculateMatchPercentages()
    {
        var accounts = await LoadAccountsFromDb();
        var matchLogger = new DbLogger<MatchRate>(() => new AccountDedupeDb());
        var statusLogger = new DbLogger<ProcessingStatus>(() => new AccountDedupeDb());
        var matchCalculator = new MatchCalculator();
        Console.WriteLine("Starting Calculator");
        await matchCalculator.ExecuteAsync(accounts,
            matchLogger,
            statusLogger);
    }

    private static async Task<List<Account>> LoadAccountsFromDb()
    {
        Console.WriteLine("Loading Accounts from DB");
        await using var db = new AccountDedupeDb();
        var accounts = await db.Accounts.Where(m => !string.IsNullOrWhiteSpace(m.Grouping)).AsNoTracking().ToListAsync();
        return accounts;
    }


    public static async Task LoadDatabaseWithAccounts()
    {
        await InitializeDb();
        var inputFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Account.csv");
        Console.WriteLine("Loading Csv");
        var accounts = CsvBinder.LoadCsv<Account>(inputFile);
        Console.WriteLine("Assigning Group Key");
        AccountGroupAssigner.AssignGroupKeys(accounts);
        Console.WriteLine("Save to Db");
        await SaveOrUpdateAccountsToDb(accounts);
    }


    private static List<AccountDeduplication.DAL.Account> LoadAccounts(string inputFile)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null,  // Ignore missing columns instead of throwing
        };

        using var reader = new StreamReader("accounts.csv");
        using var csv = new CsvReader(reader, config);

        csv.Context.RegisterClassMap<AccountCsvModelMap>();
        return csv.GetRecords<AccountDeduplication.DAL.Account>().ToList();
    }

    private static async Task SaveOrUpdateAccountsToDb(List<Account> accounts)
    {
        await using var db = new AccountDedupeDb();

        await db.BulkInsertOrUpdateAsync(accounts);
    }

    
    private static async Task InitializeDb()
    {
        await using var db = new AccountDedupeDb();
        await db.Database.MigrateAsync();
    }


    private static async Task GenerateDataloadFile(List<Account> accounts, List<GroupingResults> groupingResults)
    {
        var accountDict = accounts.ToDictionary(m => m.Id);

        //remove group leaders
        var dataLoadModels = groupingResults.Where(m =>
        {
            var groupAccount = accountDict[m.GroupAccountId];
            return m.Name != groupAccount.Name || m.Street != groupAccount.BillingStreet;
        }).Select(m =>
        {
            var groupAccount = accountDict[m.GroupAccountId];
            return new DataLoadModel
            {
                Id = m.AccountId,
                Name = m.Name == groupAccount.Name ? null : groupAccount.Name,
                OldName = m.Name == groupAccount.Name ? null : m.Name,
                Street = m.Street == groupAccount.BillingStreet ? null : groupAccount.BillingStreet,
                OldStreet = m.Street == groupAccount.BillingStreet ? null : m.Street
            };

        });
        var dataloadFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Dataload-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv");
        await using var logger = new CsvLogger<DataLoadModel>(dataloadFile);
        await logger.AddEntriesAsync(dataLoadModels);
        Console.WriteLine($"Dataload file {dataloadFile}");

        
    }

    private static async Task<List<GroupingResults>> ProcessAndLogGroupingResults(List<Account> accounts, List<MatchRate> intermediateResults)
    {
        var groupingResults =  GetDistinctSingleGroupedRecordsThatChanged(accounts, intermediateResults).OrderBy(m=> m.GroupAccountId).ToList();
        var x = groupingResults.GroupBy(m => m.AccountId).Where(m => m.Count() > 1).SelectMany(m => m).ToList();
        var resultsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"GroupingResults-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv");
        await using var logger = new CsvLogger<GroupingResults>(resultsFile);
        await logger.AddEntriesAsync(groupingResults);
        Console.WriteLine($"Grouping Results: {resultsFile}");
        return groupingResults;
    }


    /// <summary>
    ///  Returns deduplicated results and if a match belongs to multiple groups,
    /// it selects the best match based on match percentage and role count.
    /// Keep only records where the name or address will change when compared to the group account
    /// finally, adds all accounts that have an entry in the list (even though they are not included) to the list
    /// </summary>
    /// <param name="accounts"></param>
    /// <param name="intermediateResults"></param>
    /// <returns></returns>
    public static List<GroupingResults> GetDistinctSingleGroupedRecordsThatChanged(List<Account> accounts, List<MatchRate> intermediateResults)
    {
        var accountDict = accounts.ToDictionary(m => m.Id);
        var removeDuplicates = intermediateResults            
            .DistinctBy(m => new { m.AccountId1, m.AccountId2 });
        
        //if it appears in two groups get the best mach 
        var bestAssignments = removeDuplicates
            .Where(m => 0.4* m.NameMatch + 0.6* m.ShippingAddressMatch > 0.85)
            .GroupBy(x => x.AccountId2)
            .Select(g =>
                g                                                                                                                                                                       
                    .OrderBy(m => 0.4* m.NameMatch + 0.6* m.ShippingAddressMatch)
                    .ThenByDescending(x => x.Account1RoleCount)
                    .First());
        var mappedToFinalResults = bestAssignments.Select(m => new GroupingResults()
            {
                GroupAccountId = m.AccountId1,
                AccountId = m.AccountId2,
            });

        

        var populatedWithData = mappedToFinalResults.Select(m =>
        {
            var account = accountDict[m.AccountId];
            m.Name = account.Name;
            m.Street = account.BillingStreet;
            m.NPI = account.NPI;
            return m;
        }).ToList();
        
        
        var groupLeaders = populatedWithData
            .Select(m => m.GroupAccountId)
            .Distinct()
            .Select(m =>
            {
                var account = accountDict[m];
                return new GroupingResults()
                {
                    AccountId = m,
                    GroupAccountId = m,
                    Name = account.Name,
                    Street = account.BillingStreet,
                    IsGroupLeader = true,
                    NPI = account.NPI
                };
            }).ToList();
        
        populatedWithData.AddRange(groupLeaders);

        var nonGroupLeaderNpiRecords = populatedWithData.GroupBy(m => m.GroupAccountId)
            .Where(m => m.Any(g => !g.IsGroupLeader && !string.IsNullOrEmpty(g.NPI)))
            .ToList();
        Console.WriteLine($"There are {nonGroupLeaderNpiRecords.Count} groups where the NPI account is not a leader");
        //NPI records should never be overwritten and instantly become the group leader
        List<string> forcedChangedGroupLeaderIds = [];
        foreach (var group in nonGroupLeaderNpiRecords)
        {
            var groupLeader = group.First(m => m.IsGroupLeader);
            var forcedLeader = group.First(m => !string.IsNullOrWhiteSpace(m.NPI));
            Console.WriteLine($"group leader was {groupLeader.AccountId} and is now {forcedLeader.AccountId}" );
            groupLeader.IsGroupLeader = false;
            forcedLeader.IsGroupLeader = true;
            foreach (var member in group)
            {
                member.GroupAccountId = forcedLeader.AccountId;
            }
            forcedChangedGroupLeaderIds.Add(forcedLeader.AccountId);

        }
        var changedResultsOnly = populatedWithData.Where(m =>
        {
            var account = accountDict[m.GroupAccountId];
            return account.Name != m.Name || account.BillingStreet != m.Street;
        }).ToList();

        var list = changedResultsOnly.GroupBy(m => m.AccountId).Where(m => m.Count() > 1).SelectMany(m => m).ToList();
        // need to add back group leaders because the diff removed them
        var addBackInGroupLeaders = changedResultsOnly.Select(m => m.GroupAccountId)
            .Distinct()
            .Select(m =>
            {
                var account = accountDict[m];
                return new GroupingResults()
                {
                    AccountId = m,
                    GroupAccountId = m,
                    Name = account.Name,
                    Street = account.BillingStreet,
                    NPI = account.NPI,
                    IsGroupLeader = true,
                    IsForcedGroupLeader = forcedChangedGroupLeaderIds.Contains(m)
                };
            }).ToList();

        changedResultsOnly.AddRange(addBackInGroupLeaders);
        
        var dedupeAgain = changedResultsOnly
            .GroupBy(x => x.AccountId)
            .Select(g => g .First())
            .ToList();
        
        return dedupeAgain;
    }
}