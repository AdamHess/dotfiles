using System.Globalization;
using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;

namespace AccountDeduplication.CsvModels;

public static class CsvBinder
{
    public static List<T> LoadCsv<T>(string inputFile) where T : class
    {
        using var reader = new StreamReader(inputFile);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        // Register all ClassMaps from the assembly where T is defined
        RegisterAllMaps(csv, Assembly.GetCallingAssembly());

        var records = csv.GetRecords<T>().ToList();
        return records;
    }

    private static void RegisterAllMaps(CsvReader csv, Assembly assembly)
    {
        var mapTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t =>
            {
                var baseType = t.BaseType;
                while (baseType != null)
                {
                    if (baseType.IsGenericType &&
                        baseType.GetGenericTypeDefinition() == typeof(ClassMap<>))
                        return true;
                    baseType = baseType.BaseType;
                }
                return false;
            });

        foreach (var mapType in mapTypes)
        {
            var mapInstance = Activator.CreateInstance(mapType);
            csv.Context.RegisterClassMap((ClassMap)mapInstance);
        }
    }
}