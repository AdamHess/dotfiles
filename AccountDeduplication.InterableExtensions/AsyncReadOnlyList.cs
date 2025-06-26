using System.Collections;

namespace AccountDeduplication.IterableExtensions;

public class AsyncReadOnlyList<T>(IAsyncEnumerable<T> source) : IAsyncReadOnlyList<T>
{
    private readonly IAsyncEnumerator<T> _enumerator = source.GetAsyncEnumerator();
    private readonly List<T> _buffer = [];
    private bool _done = false;

    public async Task<T> GetAsync(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        while (_buffer.Count <= index)
        {
            if (_done) throw new ArgumentOutOfRangeException(nameof(index));

            if (!await _enumerator.MoveNextAsync())
            {
                _done = true;
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            _buffer.Add(_enumerator.Current);
        }

        return _buffer[index];
    }

    public async Task<int> CountAsync()
    {
        if (!_done)
        {
            while (await _enumerator.MoveNextAsync())
            {
                _buffer.Add(_enumerator.Current);
            }
            _done = true;
        }
        return _buffer.Count;
    }

    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
    {
        var result = default(T);
        for (var i = 0; ; i++)
        {
            try
            {
                result = await GetAsync(i).ConfigureAwait(false);
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            if (result == null)
            {
                yield break;
            }
        }

    }

    public IEnumerator<T> GetEnumerator()
    {
        var result = default(T);
        for (var i = 0; ; i++)
        {
            try
            {
                result = GetAsync(i).GetAwaiter().GetResult();
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            if (result == null)
            {
                yield break;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}