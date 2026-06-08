using KeyboardCleanerPro.Core.Services.Configuration;
using KeyboardCleanerPro.Core.Services.Control;
using KeyboardCleanerPro.Core.Services.Logging;
using KeyboardCleanerPro.Core.Services.State;

namespace KeyboardCleanerPro.Recovery;

/// <summary>
/// Lightweight console recovery agent (spec §16 Methods B + C, §17).
/// Invoked either by the Windows Scheduled Task (on logon) or manually.
/// 
/// Responsibilities:
///   1. Check if a device-state.json file exists with IsKeyboardDisabled=true
///   2. Re-enable the keyboard device using the real Windows API
///   3. Clear the state file
///   4. Exit with code 0 (success) or 1 (failure)
/// </summary>
internal static class Program
{
    private static readonly string DataDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                     "KeyboardCleaner");

    private static readonly string LogDirectory = Path.Combine(DataDirectory, "Logs");

    private static int Main(string[] args)
    {
        bool isRecoverFlag = args.Contains("--recover", StringComparer.OrdinalIgnoreCase);

        // ── Compose services ──────────────────────────────────────────────────
        using var logger     = new LoggingService(LogDirectory, enabled: true);
        var stateManager     = new StateManager(DataDirectory);
        var configService    = new ConfigurationService(DataDirectory);
        var controlService   = new DeviceControlService(logger);

        logger.Log("RecoveryAgent", "startup", "Started", null, Environment.UserName);

        // ── Load persisted state ──────────────────────────────────────────────
        var savedState = stateManager.LoadState();

        if (savedState is null)
        {
            logger.Log("RecoveryAgent", "none", "NoStateFile-NothingToDo", null, Environment.UserName);
            return 0;
        }

        if (!savedState.IsKeyboardDisabled)
        {
            logger.Log("RecoveryAgent", savedState.DeviceInstanceId, "KeyboardAlreadyEnabled-NothingToDo", null, Environment.UserName);
            stateManager.ClearState();
            return 0;
        }

        // ── Restore keyboard ──────────────────────────────────────────────────
        logger.Log("RecoveryAgent", savedState.DeviceInstanceId, "AttemptingRestore", null, Environment.UserName);

        var result = controlService.EnableDevice(savedState.DeviceInstanceId);

        if (result.IsSuccess)
        {
            stateManager.ClearState();
            logger.Log("RecoveryAgent", savedState.DeviceInstanceId, "RestoredSuccessfully", null, Environment.UserName);
            Console.WriteLine($"[OK] Keyboard '{savedState.DeviceInstanceId}' successfully restored.");
            return 0;
        }
        else
        {
            logger.Log("RecoveryAgent", savedState.DeviceInstanceId,
                $"RestoreFailed: {result.ErrorMessage}", result.ErrorCode, Environment.UserName);
            Console.Error.WriteLine($"[ERROR] Failed to restore keyboard: {result.ErrorMessage}");
            return 1;
        }
    }
}
