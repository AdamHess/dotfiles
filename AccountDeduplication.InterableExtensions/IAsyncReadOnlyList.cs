namespace AccountDeduplication.IterableExtensions;

public interface IAsyncReadOnlyList<T> : IAsyncEnumerable<T>, IEnumerable<T>
{
    Task<T> GetAsync(int index);
    Task<int> CountAsync();
}