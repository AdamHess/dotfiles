namespace AccountDeduplication.IterableExtensions;

public interface IAsyncReadOnlyList<T> : IAsyncEnumerable<T>
{
    Task<T> GetAsync(int index);
    Task<int> CountAsync();
}