using System.Collections.Concurrent;
using System.Reflection;

using Parquet;


namespace AccountDeduplication.Parquet;

public class ParquetBinder<T> where T : class, new()
{
    private readonly ConcurrentDictionary<string, string> _snakeCaseCache = new();
    private readonly Lazy<PropertyMapping[]> _cachedMappings;

    public ParquetBinder()
    {
        _cachedMappings = new Lazy<PropertyMapping[]>(InitializeMappings);
    }

    private class PropertyMapping
    {
        public PropertyInfo Property { get; set; }
        public string ColumnName { get; set; }
        public Type TargetType { get; set; }
    }

    public IEnumerable<IList<T>> ReadParallel(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = ParquetReader.CreateAsync(stream).GetAwaiter().GetResult();

        foreach (var i in Enumerable.Range(0, reader.RowGroupCount))
        {
            var resultsForGroup =  ProcessRowGroup(reader, i);
            Console.WriteLine($"Read Group {i}/{reader.RowGroupCount} with {resultsForGroup.Count} records");
            yield return resultsForGroup;
        }
            
     

    }

  
    private List<T> ProcessRowGroup(ParquetReader reader, int rowGroupIndex)
    {
        using var groupReader = reader.OpenRowGroupReader(rowGroupIndex);
        var columns = new Dictionary<string, Array>();

        foreach (var field in reader.Schema.GetDataFields())
            columns[field.Name] = (groupReader.ReadColumnAsync(field))
                .GetAwaiter()
                .GetResult()
                ?.Data;

        var validMappings = _cachedMappings.Value.Where(m => columns.ContainsKey(m.ColumnName)).ToArray();
        int rowCount = columns.Values.FirstOrDefault()?.Length ?? 0;

        var result = new List<T>(rowCount);
        for (int i = 0; i < rowCount; i++)
        {
            var obj = new T();
            foreach (var mapping in validMappings)
            {
                var value = columns[mapping.ColumnName].GetValue(i);
                if (value != null && value != DBNull.Value)
                {
                    try
                    {
                        mapping.Property.SetValue(obj, ConvertValue(value, mapping.TargetType));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error converting value for property {mapping.Property.Name}: {ex.Message}");
                    }
                }
            }
            result.Add(obj);
        }
        return result;
    }

    private object ConvertValue(object value, Type targetType)
    {
        if (value == null || value == DBNull.Value)
            return null;

        if (targetType == typeof(string))
            return value.ToString();

        if (targetType.IsEnum)
            return Enum.Parse(targetType, value.ToString());

        if (targetType == typeof(DateTime) && value is DateTimeOffset dto)
            return dto.DateTime;

        if (targetType == typeof(DateTimeOffset) && value is DateTime dt)
            return new DateTimeOffset(dt);

        return System.Convert.ChangeType(value, targetType);
    }

    private PropertyMapping[] InitializeMappings()
    {
        return typeof(T).GetProperties()
            .Where(p => p.CanWrite)
            .Select(p => new PropertyMapping
            {
                Property = p,
                ColumnName = GetColumnName(p.Name),
                TargetType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType
            })
            .ToArray();
    }

    private string GetColumnName(string propertyName)
    {
        return _snakeCaseCache.GetOrAdd(propertyName, ToSnakeCase);
    }

    private static string ToSnakeCase(string str)
    {
        return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLower();
    }
}