namespace AccountCsvProcessing.RunAll
{
    internal class Program
    {
        public static async Task Main()
        {
            //var inputFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Account.csv");
            var inputFile = "D:\\ReducedSet.csv";
            var reducedSetOutputFile = "D:\\ReducedSet.csv";
            await AccountDeduplication.LoadDatabase.Program.LoadDatabaseAndSaveAccounts(inputFile);
            await AccountDeduplication.CalculateMatchRates.Program.CalculateMatchRates();
            await AccountDeduplication.ProcessResults.Program.ProcessResultsForPrefix();
        }
    }
}
