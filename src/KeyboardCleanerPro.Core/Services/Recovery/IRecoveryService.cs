using KeyboardCleanerPro.Core.Models;

namespace KeyboardCleanerPro.Core.Services.Recovery;

/// <summary>
/// Manages automatic keyboard recovery via in-process timer, Windows Scheduled Task,
/// and startup-check mechanisms (spec §16 Methods A–C).
/// </summary>
public interface IRecoveryService
{
    /// <summary>Starts the in-process auto-restore countdown timer.</summary>
    void StartAutoRestoreTimer(string deviceInstanceId, TimeSpan delay, Action onRestore);

    /// <summary>Cancels the in-process countdown timer.</summary>
    void CancelAutoRestoreTimer();

    /// <summary>
    /// Registers a Windows Scheduled Task (Method C) that runs the recovery agent
    /// on user logon so the keyboard is restored even after a reboot/crash.
    /// </summary>
    OperationResult RegisterRecoveryScheduledTask(string recoveryExePath, string deviceInstanceId);

    /// <summary>Removes the Windows Scheduled Task created by <see cref="RegisterRecoveryScheduledTask"/>.</summary>
    OperationResult UnregisterRecoveryScheduledTask();

    /// <summary>True if the auto-restore timer is currently active.</summary>
    bool IsTimerActive { get; }

    /// <summary>Remaining time before auto-restore fires. Null when timer is not active.</summary>
    TimeSpan? RemainingTime { get; }
}
