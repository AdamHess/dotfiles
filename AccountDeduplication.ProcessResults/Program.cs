using AccountDeduplication.CsvModels;
using AccountDeduplication.DAL;

namespace AccountDeduplication.ProcessResults
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var inputFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Account.csv");
            var accounts = CsvBinder.LoadCsv<Account>(inputFile);
        
        }
    }
}
