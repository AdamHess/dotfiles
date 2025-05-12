using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Channels;
using CsvHelper;

public class CsvLogger<T> : IAsyncDisposable
{
    private readonly string _filePath;
    private readonly Channel<T> _channel;
    private readonly Task _backgroundWriterTask;
    private readonly StreamWriter _writer;
    private readonly CsvWriter _csvWriter;
    private readonly CancellationTokenSource _cts = new();

    private const int FlushThreshold = 100; // Write every 100 entries
    private const int FlushIntervalMs = 2000; // Or every 2 seconds

    public CsvLogger(string filePath)
    {
        _filePath = filePath;
        _writer = new StreamWriter(filePath, append: false);
        _csvWriter = new CsvWriter(_writer, CultureInfo.InvariantCulture);
        _csvWriter.WriteHeader<T>();
        _csvWriter.NextRecord();
        _csvWriter.Flush();

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

    private async Task BackgroundWriterAsync()
    {
        var buffer = new List<T>(FlushThreshold);
        var flushTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(FlushIntervalMs));

        try
        {
            while (!_cts.IsCancellationRequested)
            {
                while (buffer.Count < FlushThreshold && _channel.Reader.TryRead(out var item))
                    buffer.Add(item);

                if (buffer.Count == 0)
                    await flushTimer.WaitForNextTickAsync(_cts.Token);
                else if (buffer.Count >= FlushThreshold || !flushTimer.WaitForNextTickAsync(_cts.Token).IsCompletedSuccessfully)
                    await FlushBufferAsync(buffer);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            // Flush remaining entries
            while (_channel.Reader.TryRead(out var item))
                buffer.Add(item);

            if (buffer.Count > 0)
                await FlushBufferAsync(buffer);
        }
    }

    private async Task FlushBufferAsync(List<T> buffer)
    {
        _csvWriter.WriteRecords(buffer);
        await _csvWriter.FlushAsync();
        buffer.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.Complete();
        _cts.Cancel();
        await _backgroundWriterTask;

        _csvWriter.Dispose();
        _writer.Dispose();
        _cts.Dispose();
    }
}
