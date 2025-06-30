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
            //var inputFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Account.csv");
            var inputFile = "D:\\Accounts.csv";
            var reducedSetOutputFile = "D:\\ReducedSet.csv";
            await InitializeDb();
            var dbLoader = new LoaderAndProcessor(DbContextFactory);
            await dbLoader.LoadDatabaseAndSaveAccounts(inputFile, "dglnow");
            var matchCalculatorExecutor = new MatchCalculatorExecutor(DbContextFactory);
            await matchCalculatorExecutor.CalculateMatchRates(DbContextFactory);
            var grouperAlgorithm = new GrouperAlgorithms(DbContextFactory, "D:\\");
            await grouperAlgorithm.ProcessResultsForPrefix();
        }


        private static async Task InitializeDb()
        {
            await using var db = new AccountDedupeDb();
            await db.Database.MigrateAsync();
        }

        private static AccountDedupeDb DbContextFactory()
        {
            return new AccountDedupeDb();
        }
    }
}
