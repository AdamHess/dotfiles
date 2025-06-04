using System.Threading.Channels;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace AccountDeduplication.RecordLoggers;

public class DbLogger<T> : IBatchLogger<T> where T : class
{
    private readonly Channel<T> _channel;
    private readonly Task _backgroundWriterTask;
    private readonly CancellationTokenSource _cts = new();

    private const int FlushThreshold = 300;
    private const int FlushIntervalMs = 2000;
    private readonly Func<DbContext> _contextFactory;

    public DbLogger(Func<DbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = false
        });

        _backgroundWriterTask = Task.Run(BackgroundWriterAsync);
    }

    public async Task AddEntryAsync(T entry)
    {
        await _channel.Writer.WriteAsync(entry);
    }

    public async Task AddEntriesAsync(IEnumerable<T> entries)
    {
        foreach (var entry in entries)
        {
            await _channel.Writer.WriteAsync(entry);
        }
    }

    private async Task BackgroundWriterAsync()
    {
        var buffer = new List<T>(FlushThreshold);
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(FlushIntervalMs));

        try
        {
            while (await timer.WaitForNextTickAsync(_cts.Token) || buffer.Count > 0)
            {
                while (buffer.Count < FlushThreshold && _channel.Reader.TryRead(out var item))
                {
                    buffer.Add(item);
                }

                if (buffer.Count > 0)
                {
                    Console.WriteLine("*");
                    await FlushBufferAsync(buffer);
                }

                if (_channel.Reader.Completion.IsCompleted && _channel.Reader.Count == 0)
                    break;
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            while (_channel.Reader.TryRead(out var item))
                buffer.Add(item);

            if (buffer.Count > 0)
                await FlushBufferAsync(buffer);
        }
    }

    private async Task FlushBufferAsync(List<T> buffer)
    {
        await using var context = _contextFactory();
        await context.BulkInsertOrUpdateAsync(buffer);
        buffer.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _channel.Writer.Complete();
        await _backgroundWriterTask;
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}