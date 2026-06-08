using KeyboardCleanerPro.Core.Models;
using KeyboardCleanerPro.Core.Services.Logging;

namespace KeyboardCleanerPro.Core.Services.Recovery;

/// <summary>
/// Implements spec §16 recovery Methods A (timer) and C (scheduled task).
/// The timer fires on the UI thread via a System.Threading.Timer callback.
/// </summary>
public sealed class RecoveryService : IRecoveryService, IDisposable
{
    private readonly ILoggingService          _logger;
    private readonly ScheduledTaskManager     _taskManager;
    private readonly object                   _lock           = new();
    private          System.Threading.Timer?  _timer;
    private          DateTime?                _restoreAt;
    private          bool                     _disposed;

    public const string TaskName = "KeyboardCleanerProRecovery";

    public RecoveryService(ILoggingService logger, ScheduledTaskManager taskManager)
    {
        _logger      = logger      ?? throw new ArgumentNullException(nameof(logger));
        _taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));
    }

    /// <inheritdoc />
    public bool IsTimerActive
    {
        get { lock (_lock) return _timer is not null; }
    }

    /// <inheritdoc />
    public TimeSpan? RemainingTime
    {
        get
        {
            lock (_lock)
            {
                if (_restoreAt is null) return null;
                var remaining = _restoreAt.Value - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
    }

    /// <inheritdoc />
    public void StartAutoRestoreTimer(string deviceInstanceId, TimeSpan delay, Action onRestore)
    {
        ArgumentException.ThrowIfNullOrEmpty(deviceInstanceId);
        ArgumentNullException.ThrowIfNull(onRestore);
        if (delay <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delay), "Delay must be positive.");

        lock (_lock)
        {
            StopTimerInternal();

            _restoreAt = DateTime.UtcNow + delay;

            _timer = new System.Threading.Timer(_ =>
            {
                _logger.Log("AutoRestoreTimer", deviceInstanceId, "Fired", null, Environment.UserName);
                lock (_lock) { StopTimerInternal(); }
                onRestore();
            },
            state: null,
            dueTime: delay,
            period: Timeout.InfiniteTimeSpan);

            _logger.Log("StartAutoRestoreTimer", deviceInstanceId, $"Delay={delay}", null, Environment.UserName);
        }
    }

    /// <inheritdoc />
    public void CancelAutoRestoreTimer()
    {
        lock (_lock)
        {
            bool wasActive = _timer is not null;
            StopTimerInternal();
            if (wasActive)
                _logger.Log("CancelAutoRestoreTimer", "n/a", "Cancelled", null, Environment.UserName);
        }
    }

    /// <inheritdoc />
    public OperationResult RegisterRecoveryScheduledTask(string recoveryExePath, string deviceInstanceId)
    {
        ArgumentException.ThrowIfNullOrEmpty(recoveryExePath);
        ArgumentException.ThrowIfNullOrEmpty(deviceInstanceId);

        var result = _taskManager.CreateLogonTask(TaskName, recoveryExePath, deviceInstanceId);
        if (result.IsSuccess)
            _logger.Log("RegisterRecoveryTask", deviceInstanceId, "Registered", null, Environment.UserName);
        else
            _logger.Log("RegisterRecoveryTask", deviceInstanceId, "Failed", result.ErrorCode, Environment.UserName);

        return result;
    }

    /// <inheritdoc />
    public OperationResult UnregisterRecoveryScheduledTask()
    {
        var result = _taskManager.DeleteTask(TaskName);
        if (result.IsSuccess)
            _logger.Log("UnregisterRecoveryTask", "n/a", "Removed", null, Environment.UserName);
        return result;
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void StopTimerInternal()
    {
        _timer?.Dispose();
        _timer     = null;
        _restoreAt = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        lock (_lock) StopTimerInternal();
    }
}
