using AccountDeduplication.CsvModels;
using AccountDeduplication.DAL;
using AccountDeduplication.RecordLoggers;
using Microsoft.EntityFrameworkCore;

namespace AccountDeduplication.ProcessResults
{
    internal class Program
    {
        private const double MinimumMatchRate = 0.85;
        static async Task Main()
        {
            var allMatchRates = await GetMatchRatesForGroupingKey("LNKT-dglnow__FL-fl");
            var phase1Results = Phase1(allMatchRates);
            var fileTime = DateTime.Now.ToFileTime();
            await SaveResultsToFile($"Phase1Results.csv", phase1Results);
            var phase2 = Phase2(allMatchRates, phase1Results);
            await SaveResultsToFile($"Phase2Results.csv", phase2);
            var phase3 = Phase3(allMatchRates, phase2);
            await SaveResultsToFile($"Phase3Results.csv", phase3);
            var phase4 = Phase4(allMatchRates, phase3);
            await SaveResultsToFile($"Phase4Results.csv", phase4);

        }

        private static async Task SaveResultsToFile(string fileName, List<GroupMapping> groupingResults)
        {
            await using CsvLogger<GroupingResults> csvLogger = new(fileName);
            await csvLogger.AddEntriesAsync(groupingResults.SelectMany(p =>
                p.GroupMembers
                    .Select(gm => new GroupingResults()
                    {
                        GroupAccountId = p.GroupLeader.Id,
                        AccountId = gm.Id,
                        Name = gm.Name,
                        Street = gm.ShippingStreet,
                        IsGroupLeader = p.GroupLeader.Id == gm.Id,
                        NPI = gm.NPI
                    })).OrderBy(x => x.GroupAccountId)
                .ThenBy(m => m.IsGroupLeader));
        }





        private static List<GroupMapping> PhaseN(List<MatchRate> allMatchRates,
            List<GroupMapping> phaseNLastResults,
            Func<MatchRate, double> matchCalculatingCriterion)
        {
            var leaders = phaseNLastResults.Select(m => m.GroupLeader).Select(m => m.Id).ToList();
            //get all the match rates only for the leaders since we only care about grouping them. 
            var filterMatchRates = allMatchRates.Where(m => leaders.Contains(m.AccountId1) &&
                                                            leaders.Contains(m.AccountId2))
                .ToList();
            var leaderGrouping = GroupingAlgorithm(filterMatchRates, matchCalculatingCriterion);
            var phaseNResults = MergeGroupsAndRebuildGroupingList(phaseNLastResults, leaderGrouping);
            return phaseNResults;

        }

        private static List<GroupMapping> MergeGroupsAndRebuildGroupingList(List<GroupMapping> phaseNLastResults, List<GroupMapping> leaderGrouping)
        {
            List<GroupMapping> phaseNResults = [];
            foreach (var group in leaderGrouping)
            {
                var groupMembers = phaseNLastResults
                    .Where(m => group.GroupMembers.Any(a => a.Id == m.GroupLeader.Id))
                    .SelectMany(m => m.GroupMembers);

                phaseNResults.Add(new GroupMapping()
                {
                    GroupLeader = group.GroupLeader,
                    GroupMembers = [.. groupMembers] //should also include group leader in this group
                });
            }
            phaseNResults.AddRange(phaseNLastResults.Where(m => phaseNResults
                .SelectMany(n => n.GroupMembers)
                .All(n => n.Id != m.GroupLeader.Id))); //add all the groups to the new list to return (that havent been merged)
            return phaseNResults;
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
                .Where(m => matchCalculatingCriterion(m) >= MinimumMatchRate)
                .GroupBy(x => x.AccountId2) //treat account1 like the group leader
                .Select(g =>
                    g
                        .OrderBy(matchCalculatingCriterion)
                        .ThenByDescending(x => x.Account1RoleCount)
                        .First());

            //build grouping object 

            var possibleGroups = assignAccountsToSingleGroup
                .Where(m => matchCalculatingCriterion(m) >= MinimumMatchRate)
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

            possibleGroups = CleanupNnpiRecords(allMatchRates, matchCalculatingCriterion, possibleGroups);
            var recordsWhereGroupLeaderIsNotInList =
                possibleGroups.Where(m => m.GroupMembers.All(a => a.Id != m.GroupLeader.Id)).ToList();
            foreach (var group in recordsWhereGroupLeaderIsNotInList)
            {
                group.GroupMembers.Add(group.GroupLeader);
                group.GroupMembers = group.GroupMembers
                    .DistinctBy(m => m.Id) //remove duplicates
                    .ToList(); //make sure we have unique members in the group
            }
            return possibleGroups;

        }


        private static List<GroupMapping> CleanupNnpiRecords(List<MatchRate> allMatchRates, Func<MatchRate, double> matchCalculatingCriterion, List<GroupMapping> possibleGroups)
        {
            AssignRecordWithNpiAsLeaderIfOnlyOne(possibleGroups);

            // multiple NPI records

            AssignNpiRecordWhenMoreThanOneInGroup(allMatchRates, matchCalculatingCriterion, possibleGroups);
            return possibleGroups;
        }

        private static void AssignNpiRecordWhenMoreThanOneInGroup(List<MatchRate> allMatchRates, Func<MatchRate, double> matchCalculatingCriterion,
            List<GroupMapping> possibleGroups)
        {
            //groups with more than one NPI
            var groupsWithMultipleNpiNotAsLeader = possibleGroups.Where(m =>
                string.IsNullOrWhiteSpace(m.GroupLeader.NPI) &&
                m.GroupMembers.Count(n => !string.IsNullOrWhiteSpace(n.NPI)) > 1).ToList();


            // find the group leader that has the highest average match rate against all members of the group and if there is a tie
            // use the one with the most roles

            FindAndAssignBestNpiAsGroupLeader(allMatchRates, matchCalculatingCriterion, groupsWithMultipleNpiNotAsLeader);
        }

        private static void FindAndAssignBestNpiAsGroupLeader(
                    List<MatchRate> allMatchRates,
                    Func<MatchRate, double> matchCalculatingCriterion,
                    List<GroupMapping> groupsWithMultipleNpiNotAsLeader)
        {
            // Build a lookup for fast access to match rates between any two accounts
            var matchRateLookup = allMatchRates
                .ToDictionary(m => (m.AccountId1, m.AccountId2), m => m);

            foreach (var group in groupsWithMultipleNpiNotAsLeader)
            {
                var npiCandidates = group.GroupMembers.Where(a => !string.IsNullOrWhiteSpace(a.NPI));

                var best = npiCandidates
                    .Select(candidate => new
                    {
                        Candidate = candidate,
                        Avg = group.GroupMembers
                            .Where(m => m.Id != candidate.Id)
                            .Select(m =>
                                matchRateLookup.TryGetValue((candidate.Id, m.Id), out var r1) ? r1 :
                                matchRateLookup.TryGetValue((m.Id, candidate.Id), out var r2) ? r2 : null)
                            .Where(r => r != null)
                            .DefaultIfEmpty()
                            .Average(r => r == null ? 0 : matchCalculatingCriterion(r)),
                        Roles = candidate.NumberOfRoles
                    })
                    .OrderByDescending(x => x.Avg)
                    .ThenByDescending(x => x.Roles)
                    .FirstOrDefault();

                if (best != null)
                {
                    group.GroupLeader = best.Candidate;
                    group.GroupMembers = group.GroupMembers
                        .Where(m => m.Id == best.Candidate.Id || string.IsNullOrWhiteSpace(m.NPI))
                        .ToList();
                }
            }
        }

        private static void AssignRecordWithNpiAsLeaderIfOnlyOne(List<GroupMapping> possibleGroups)
        {
            //If an NPI record is in a group and its not a group leader, make it the group leader (this is only if there is only 1 npi account in the group) 
            var groupsWithNonNpiAsLeader = possibleGroups.Where(m => string.IsNullOrWhiteSpace(m.GroupLeader.NPI) &&
                                                                     m.GroupMembers.Count(n => !string.IsNullOrWhiteSpace(n.NPI)) == 1);

            foreach (var group in groupsWithNonNpiAsLeader)
            {
                var npiAccount = group.GroupMembers.First(n => !string.IsNullOrWhiteSpace(n.NPI));
                group.GroupLeader = npiAccount;
            }
        }

        public static double Phase1Criterion(MatchRate matchRate)
        {
            return 0.4 * matchRate.NameMatch + 0.6 * matchRate.BillingStreetMatch;
        }
        private static double Phase2Criterion(MatchRate matchRate)
        {
            return 0.4 * matchRate.NameMatch + 0.6 * matchRate.ShippingAddressMatch;
        }

        private static double Phase3Criterion(MatchRate matchRate)
        {
            return 0.4 * matchRate.OtherNameMatch + 0.6 * matchRate.BillingStreetMatch;
        }
        private static double Phase4Criterion(MatchRate matchRate)
        {
            return 0.4 * matchRate.OtherNameMatch + 0.6 * matchRate.ShippingAddressMatch;
        }
        private static List<GroupMapping> Phase1(List<MatchRate> allMatchRates)
        {

            return GroupingAlgorithm(allMatchRates, Phase1Criterion);

        }

        private static List<GroupMapping> Phase2(List<MatchRate> allMatchRates, List<GroupMapping> phase1Results)
        {
            return PhaseN(allMatchRates, phase1Results, Phase2Criterion);
        }
        private static List<GroupMapping> Phase3(List<MatchRate> allMatchRates, List<GroupMapping> phase2Results)
        {
            return PhaseN(allMatchRates, phase2Results, Phase3Criterion);
        }
        private static List<GroupMapping> Phase4(List<MatchRate> allMatchRates, List<GroupMapping> phase3Results)
        {
            return PhaseN(allMatchRates, phase3Results, Phase4Criterion);
        }


    }
}
