using AccountDeduplication.CsvModels;
using AccountDeduplication.DAL;
using AccountDeduplication.RecordLoggers;
using Microsoft.EntityFrameworkCore;

namespace AccountDeduplication.ProcessResults
{
    internal class Program
    {
        static async Task Main()
        {
            InitDatabase();
            var phase1Results = await Phase1();
            await using CsvLogger<GroupingResults> csvLogger = new($"Phase1Results-{DateTime.Now.ToFileTime()}.csv");
            await csvLogger.AddEntriesAsync(phase1Results.SelectMany(p =>
                p.GroupMembers
                    .Select(gm => new GroupingResults()
                    {
                        GroupAccountId = p.GroupLeader.Id,
                        AccountId = gm.Id,
                        Name = gm.Name,
                        Street = gm.ShippingStreet,
                        IsGroupLeader = p.GroupLeader.Id == gm.Id,
                        NPI = gm.NPI
                    })));

        }

        private static void InitDatabase()
        {
            using var db = new AccountDedupeDb();
            db.Database.Migrate();
        }

        private static async Task<List<GroupMapping>> Phase1()
        {
            var allMatchRates = await GetMatchRatesForGroupingKey("LNKT-dglnow__FL-fl");

            return GroupingAlgorithm(allMatchRates, Phase1Criterion);
        }

        private static async Task<List<MatchRate>> GetMatchRatesForGroupingKey(string groupingKey)
        {
            await using var db = new AccountDedupeDb();

            var allMatchRates = await db.MatchRates
                .Where(m => EF.Functions.Like(m.Account1.Grouping, groupingKey))
                .Include(m => m.Account1)
                .Include(m => m.Account2)
                .AsSplitQuery()
                .AsNoTracking()
                .ToListAsync();

            allMatchRates.AddRange(allMatchRates.Select(m =>
                new MatchRate()
                {
                    AccountId1 = m.AccountId2,
                    AccountId2 = m.AccountId1,
                    Account1RoleCount = m.Account2RoleCount,
                    Account2RoleCount = m.Account1RoleCount,
                }).ToList());
            return allMatchRates;
        }

        public static List<GroupMapping> GroupingAlgorithm(List<MatchRate> allMatchRates,
            Func<MatchRate, double> matchCalculatingCriterion)
        {

            //move accounts to a single group leader 
            var assignAccountsToSingleGroup = allMatchRates
                .Where(m => matchCalculatingCriterion(m) > 0.85)
                .GroupBy(x => x.AccountId2) //treat account1 like the group leader
                .Select(g =>
                    g
                        .OrderBy(matchCalculatingCriterion)
                        .ThenByDescending(x => x.Account1RoleCount)
                        .First());

            //build grouping object 

            var possibleGroups = allMatchRates
                .Where(m => matchCalculatingCriterion(m) > 0.85)
                .GroupBy(x => x.AccountId1)
                .Where(m => m.Count() > 1) //only groups with more than one member
                .Select(m => new GroupMapping()
                {
                    GroupLeader = m.First(n => n.AccountId1 == m.Key).Account1,
                    GroupMembers = m.Select(n => n.Account2).ToList()
                }).ToList();



            //------------NPI Processing ------------

            //if the number of NPI records == number of group members, remove it from possible group leader list 
            possibleGroups = possibleGroups.Where(m =>
                    m.GroupMembers.Count(n => !string.IsNullOrWhiteSpace(n.NPI)) != m.GroupMembers.Count)
                .ToList();


            CleanupNnpiRecords(allMatchRates, matchCalculatingCriterion, possibleGroups);
            return possibleGroups;

        }


        private static void CleanupNnpiRecords(List<MatchRate> allMatchRates, Func<MatchRate, double> matchCalculatingCriterion, List<GroupMapping> possibleGroups)
        {
            //If an NPI record is in a group and its not a group leader, make it the group leader (this is only if there is only 1 npi account in the group) 
            var groupsWithNonNpiAsLeader = possibleGroups.Where(m => string.IsNullOrWhiteSpace(m.GroupLeader.NPI) &&
                                                                     m.GroupMembers.Count(n => !string.IsNullOrWhiteSpace(n.NPI)) == 1);

            foreach (var group in groupsWithNonNpiAsLeader)
            {
                var npiAccount = group.GroupMembers.First(n => string.IsNullOrWhiteSpace(n.NPI));
                group.GroupLeader = npiAccount;
            }

            // multiple NPI records

            //groups with more than one NPI
            var groupsWithMultipleNpiNotAsLeader = possibleGroups.Where(m =>
                string.IsNullOrWhiteSpace(m.GroupLeader.NPI) &&
                m.GroupMembers.Count(n => !string.IsNullOrWhiteSpace(n.NPI)) > 1).ToList();


            // find the group leader that has the highest average match rate against all members of the group and if there is a tie
            // use the one with the most roles

            foreach (var group in groupsWithMultipleNpiNotAsLeader)
            {
                // Candidates: all NPI accounts in the group
                var npiCandidates = group.GroupMembers.Where(a => !string.IsNullOrWhiteSpace(a.NPI)).ToList();
                double bestAvg = double.MinValue;
                Account bestLeader = null;
                int bestRoles = -1;

                foreach (var candidate in npiCandidates)
                {
                    // Find all match rates between candidate and other group members
                    var matchRates = group.GroupMembers
                        .Where(m => m.Id != candidate.Id)
                        .Select(m =>
                            allMatchRates.FirstOrDefault(r =>
                                (r.AccountId1 == candidate.Id && r.AccountId2 == m.Id) ||
                                (r.AccountId2 == candidate.Id && r.AccountId1 == m.Id)))
                        .Where(r => r != null)
                        .ToList();

                    // Calculate average match rate (using the same formula as before)
                    double avg = matchRates.Count > 0
                        ? matchRates.Average(matchCalculatingCriterion!)
                        : 0;

                    // Use number of roles to break ties
                    int roles = candidate.NumberOfRoles;

                    if (avg > bestAvg || (Math.Abs(avg - bestAvg) < 1e-6 && roles > bestRoles))
                    {
                        bestAvg = avg;
                        bestLeader = candidate;
                        bestRoles = roles;
                    }
                }
                group.GroupLeader = bestLeader;
                //remove all other other NPI records from the list (except the best leader)
                group.GroupMembers = group.GroupMembers.Where(m => m.Id == bestLeader.Id ||
                                                                   string.IsNullOrWhiteSpace(m.NPI))
                    .ToList();
            }
        }

        public static double Phase1Criterion(MatchRate matchRate)
        {
            return 0.4 * matchRate.NameMatch + 0.6 * matchRate.ShippingAddressMatch;
        }
    }
}
