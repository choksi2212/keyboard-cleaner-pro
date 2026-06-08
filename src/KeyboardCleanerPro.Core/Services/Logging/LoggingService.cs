using System.Collections.Concurrent;
using KeyboardCleanerPro.Core.Models;

namespace KeyboardCleanerPro.Core.Services.Logging;

/// <summary>
/// Thread-safe logging service.
/// Writes structured log entries to a rolling daily file at:
///   %ProgramData%\KeyboardCleaner\Logs\kcp-yyyy-MM-dd.log
/// Also maintains an in-memory circular buffer of the last 500 entries for UI display.
/// </summary>
public sealed class LoggingService : ILoggingService, IDisposable
{
    private readonly string               _logDirectory;
    private readonly bool                 _enabled;
    private readonly SemaphoreSlim        _writeLock  = new(1, 1);
    private readonly ConcurrentQueue<LogEntry> _buffer = new();
    private const int BufferCapacity = 500;

    public LoggingService(string logDirectory, bool enabled = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(logDirectory);
        _logDirectory = logDirectory;
        _enabled      = enabled;

        if (_enabled)
            Directory.CreateDirectory(_logDirectory);
    }

    /// <summary>
    /// Builds and writes a structured log entry.  Never throws — logging failures are swallowed
    /// so that a logging problem never disrupts the main device-control flow.
    /// </summary>
    public void Log(string operation, string deviceId, string status, int? errorCode, string userSession)
    {
        var entry = new LogEntry(
            Timestamp:   DateTime.UtcNow,
            Operation:   operation,
            DeviceId:    deviceId,
            Status:      status,
            ErrorCode:   errorCode,
            UserSession: userSession);

        EnqueueToBuffer(entry);

        if (!_enabled) return;
        _ = WriteEntryAsync(entry);
    }

    /// <inheritdoc />
    public void LogException(string operation, string deviceId, Exception exception, string userSession)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Log(operation, deviceId, $"EXCEPTION: {exception.GetType().Name}: {exception.Message}", null, userSession);
    }

    /// <inheritdoc />
    public IReadOnlyList<LogEntry> ReadRecentEntries(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        return _buffer.TakeLast(Math.Min(count, BufferCapacity)).ToList().AsReadOnly();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void EnqueueToBuffer(LogEntry entry)
    {
        _buffer.Enqueue(entry);
        // Trim oldest entries if over capacity
        while (_buffer.Count > BufferCapacity)
            _buffer.TryDequeue(out _);
    }

    private async Task WriteEntryAsync(LogEntry entry)
    {
        string filePath = GetDailyLogFilePath(entry.Timestamp);

        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(filePath, entry.ToString() + Environment.NewLine)
                      .ConfigureAwait(false);
        }
        catch
        {
            // Logging must never throw — swallow all I/O errors silently.
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private string GetDailyLogFilePath(DateTime date) =>
        Path.Combine(_logDirectory, $"kcp-{date:yyyy-MM-dd}.log");

    public void Dispose()
    {
        _writeLock.Dispose();
    }
}
