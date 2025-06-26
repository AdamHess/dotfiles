using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace AccountDeduplication.CsvModels;

public static class CsvBinder
{
    public static List<T> LoadCsv<T>(string inputFile) where T : struct
    {
        string fileContent = File.ReadAllText(inputFile);
        using var reader = new StringReader(fileContent);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectColumnCountChanges = false,
            IgnoreBlankLines = true,
            HeaderValidated = null, // skip header validation callback
            MissingFieldFound = null, // skip missing field callback
        };
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<AccountCsvModelMap>();

        return csv.GetRecords<T>().ToList();
    }
    //public static IAsyncEnumerable<T> LoadCsvAsync<T>(string inputFile) where T : class
    //{
    //    using var reader = new StreamReader(inputFile);
    //    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
    //    // Register all ClassMaps from the assembly where T is defined
    //    csv.Context.RegisterClassMap<AccountCsvModelMap>();

    //    return csv.GetRecordsAsync<T>();
    //}
    //private static void RegisterAllMaps(CsvReader csv, Assembly assembly)
    //{
    //    var mapTypes = assembly.GetTypes()
    //        .Where(t => !t.IsAbstract && !t.IsInterface)
    //        .Where(t =>
    //        {
    //            var baseType = t.BaseType;
    //            while (baseType != null)
    //            {
    //                if (baseType.IsGenericType &&
    //                    baseType.GetGenericTypeDefinition() == typeof(ClassMap<>))
    //                    return true;
    //                baseType = baseType.BaseType;
    //            }
    //            return false;
    //        });

    //    foreach (var mapType in mapTypes)
    //    {
    //        var mapInstance = Activator.CreateInstance(mapType);
    //        csv.Context.RegisterClassMap((ClassMap)mapInstance);
    //    }
    //}
}