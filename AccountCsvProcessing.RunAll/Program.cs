using AccountDeduplication.CalculateMatchRates;
using AccountDeduplication.DAL.EF;
using AccountDeduplication.LoadDatabase;
using AccountDeduplication.ProcessResults;
using Microsoft.EntityFrameworkCore;

namespace AccountCsvProcessing.RunAll
{
    internal class Program
    {
        public static async Task Main()
        {
            // var inputFile = Path.Combine(GetSolutionDirectory(), "Accounts.parquet");
            //var inputFile = "D:\\Accounts.csv";
            //var reducedSetOutputFile = "D:\\ReducedSet.csv";
            // await InitializeDb();
            // var dbLoader = new LoaderAndProcessor(DbContextFactory);
            // await dbLoader.LoadDatabaseAndSaveAccounts(inputFile);
            var matchCalculatorExecutor = new MatchCalculatorExecutor(DbContextFactory);
            await matchCalculatorExecutor.CalculateMatchRates(DbContextFactory);
            // var grouperAlgorithm = new GrouperAlgorithms(DbContextFactory, "D:\\");
            // await grouperAlgorithm.ProcessResultsForPrefix();
        }


        private static async Task InitializeDb()
        {
            await using var db = new AccountDedupeDb(Path.Join(GetSolutionDirectory(), "MatchRate.db"));
            await db.Database.MigrateAsync();
        }

        private static AccountDedupeDb DbContextFactory()
        {
            var solutionDirectory = GetSolutionDirectory();

            return new AccountDedupeDb(Path.Join(solutionDirectory, "MatchRate.db"));
        }

        private static string GetSolutionDirectory()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (dir != null && !dir.GetFiles("*.sln").Any())
            {
                dir = dir.Parent;
            }

            return dir?.FullName;
        }
    }
}
