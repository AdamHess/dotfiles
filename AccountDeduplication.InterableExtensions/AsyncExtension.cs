namespace AccountDeduplication.IterableExtensions;

public static class AsyncExtension
{
    public static IAsyncReadOnlyList<T> AsAsyncReadOnlyList<T>(this IAsyncEnumerable<T> source)
    {
        return new AsyncReadOnlyList<T>(source);
    }
}