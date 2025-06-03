namespace CsvProcessing;

public interface IBatchLogger<T> : IAsyncDisposable
{
    Task AddEntryAsync(T entry);
    Task AddEntriesAsync(IEnumerable<T> entries);
}