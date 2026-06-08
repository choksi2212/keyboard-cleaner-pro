using KeyboardCleanerPro.Core.Models;

namespace KeyboardCleanerPro.Core.Services.Logging;

/// <summary>
/// Writes structured, timestamped log entries to a rolling daily file under
/// %ProgramData%\KeyboardCleaner\Logs, as specified in the system spec (§18).
/// </summary>
public interface ILoggingService
{
    void Log(string operation, string deviceId, string status, int? errorCode, string userSession);
    void LogException(string operation, string deviceId, Exception exception, string userSession);
    IReadOnlyList<LogEntry> ReadRecentEntries(int count);
}
