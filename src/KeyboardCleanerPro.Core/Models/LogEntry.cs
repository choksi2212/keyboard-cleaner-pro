namespace KeyboardCleanerPro.Core.Models;

/// <summary>Structured log entry written to the rolling text log file.</summary>
public sealed record LogEntry(
    DateTime Timestamp,
    string   Operation,
    string   DeviceId,
    string   Status,
    int?     ErrorCode,
    string   UserSession)
{
    public override string ToString() =>
        $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff UTC}] [{Status,-12}] {Operation,-30} " +
        $"Device={DeviceId} User={UserSession}" +
        (ErrorCode.HasValue ? $" Win32={ErrorCode}" : string.Empty);
}
