using System.Globalization;
using CsvHelper;

public class CsvLogger<T> : IDisposable
{
    private readonly string _filePath;
    private readonly object _lock = new(); // Lock object for thread safety
    private readonly CsvWriter _csvWriter;
    private readonly StreamWriter _writer;

    public CsvLogger(string filePath)
    {
        _filePath = filePath;
        _writer = new StreamWriter(_filePath);
        _csvWriter = new CsvWriter(_writer, CultureInfo.InvariantCulture);
        _csvWriter.WriteHeader<T>();
        _csvWriter.NextRecord();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _csvWriter?.Dispose();
            _writer?.Dispose();
        }
    }

    // public void AddEntry(T entry)
    // {
    //     lock (_lock)
    //     {
    //         _csvWriter.WriteRecord(entry);
    //         _csvWriter.NextRecord();
    //     }
    // }

    public async Task AddEntryAsync(T entry, CancellationToken cancellationToken)
    {
        await _csvWriter.WriteRecordsAsync([entry], cancellationToken);
        await _csvWriter.NextRecordAsync();

    }
}