using CsvHelper;
using System.Globalization;
using System.Threading.Channels;

namespace AccountDeduplication.RecordLoggers;

public class CsvLogger<T> : IBatchLogger<T>
{
    private readonly Channel<T> _channel;
    private readonly Task _backgroundWriterTask;
    private readonly StreamWriter _writer;
    private readonly CsvWriter _csvWriter;
    private readonly CancellationTokenSource _cts = new();

    private const int FlushThreshold = 300;
    private const int FlushIntervalMs = 2000;

    public CsvLogger(string filePath)
    {
        _writer = new StreamWriter(filePath, append: false); // Change to `append: true` for periodic writes
        _csvWriter = new CsvWriter(_writer, CultureInfo.InvariantCulture);
        _csvWriter.WriteHeader<T>();
        _csvWriter.NextRecord();
        _csvWriter.Flush();  // Flush to start writing

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
                // Try to fill buffer if there's space
                while (buffer.Count < FlushThreshold && _channel.Reader.TryRead(out var item))
                {
                    buffer.Add(item);
                }

                // If we have enough data, or if we timed out, flush to file
                if (buffer.Count > 0)
                {
                    Console.WriteLine($"Records written this interval: {buffer.Count}");
                    await FlushBufferAsync(buffer);
                }

                // If channel is completed and empty, break out of the loop
                if (_channel.Reader.Completion.IsCompleted && _channel.Reader.Count == 0)
                    break;
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            // Final flush, if any records are left
            while (_channel.Reader.TryRead(out var item))
                buffer.Add(item);

            if (buffer.Count > 0)
                await FlushBufferAsync(buffer);
        }
    }

    private async Task FlushBufferAsync(List<T> buffer)
    {
        await _csvWriter.WriteRecordsAsync(buffer);
        await _csvWriter.FlushAsync();
        buffer.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync(); // Stop the timer
        _channel.Writer.Complete(); // Signal no more entries
        await _backgroundWriterTask; // Wait for flush to finish

        await _csvWriter.DisposeAsync();
        await _writer.DisposeAsync();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
