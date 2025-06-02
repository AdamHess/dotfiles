namespace CsvProcessing;

public class Program
{


    public static async Task Main()
    {
        var inputFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Account.csv");
        var intermediateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"IntermediateResults-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv");
        var accounts = CsvBinder.LoadCsv<AccountCsvModel>(inputFile);
        var calculator = new MatchCalculator(intermediateFile);
        Console.WriteLine($"Intermediate files: {intermediateFile}");
        var intermediateResults = await calculator.Execute(accounts);
        var groupingResults = await ProcessAndLogGroupingResults(accounts, intermediateResults);
        await GenerateDataloadFile(accounts, groupingResults);
    }


    private static async Task GenerateDataloadFile(List<AccountCsvModel> accounts, List<GroupingResults> groupingResults)
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

    private static async Task<List<GroupingResults>> ProcessAndLogGroupingResults(List<AccountCsvModel> accounts, List<IntermediateResults> intermediateResults)
    {
        var groupingResults =  GetDistinctSingleGroupedRecordsThatChanged(accounts, intermediateResults).OrderBy(m=> m.GroupAccountId).ToList();
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
    public static List<GroupingResults> GetDistinctSingleGroupedRecordsThatChanged(List<AccountCsvModel> accounts, List<IntermediateResults> intermediateResults)
    {
        var accountDict = accounts.ToDictionary(m => m.Id);
        var removeDuplicates = intermediateResults            
            .DistinctBy(m => new { m.AccountId1, m.AccountId2 });
        
        //if it appears in two groups get the best mach 
        var bestAssignments = removeDuplicates
            .GroupBy(x => x.AccountId2)
            .Select(g =>
                g.OrderByDescending(x => x.MatchPercentage)
                    .ThenByDescending(x => x.RoleCount)
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
            .Select(m => new GroupingResults()
            {
                AccountId = m,
                GroupAccountId = m,
                Name = accountDict[m].Name,
                Street = accountDict[m].BillingStreet,
                IsGroupLeader = true
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

        return changedResultsOnly;
    }
}