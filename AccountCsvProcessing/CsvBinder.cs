using System.Globalization;
using CsvHelper;

namespace CsvProcessing;

public class CsvBinder
{
    public static List<T> LoadCsv<T>(string inputFile) where T : class
    {
        using var reader = new StreamReader(inputFile);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        return [.. csv.GetRecords<T>()];
    }
}