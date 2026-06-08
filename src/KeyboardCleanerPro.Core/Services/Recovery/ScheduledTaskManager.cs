using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using KeyboardCleanerPro.Core.Models;

namespace KeyboardCleanerPro.Core.Services.Recovery;

/// <summary>
/// Creates and deletes Windows Scheduled Tasks using the Task Scheduler COM API directly
/// via P/Invoke interop — no external NuGet dependency required.
/// The task is created to run the recovery agent at user logon (Method C, spec §16).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ScheduledTaskManager
{
    private const string TaskFolder = "\\KeyboardCleanerPro";

    /// <summary>
    /// Creates a "Run at logon" scheduled task under the KeyboardCleanerPro folder.
    /// Uses schtasks.exe to avoid COM interop complexity while remaining fully functional.
    /// </summary>
    public OperationResult CreateLogonTask(string taskName, string exePath, string deviceInstanceId)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskName);
        ArgumentException.ThrowIfNullOrEmpty(exePath);
        ArgumentException.ThrowIfNullOrEmpty(deviceInstanceId);

        if (!File.Exists(exePath))
            return OperationResult.Failure($"Recovery executable not found: {exePath}");

        // Build schtasks command:
        //   /SC ONLOGON  — triggers at user logon
        //   /RL HIGHEST  — run with highest available privilege
        //   /F           — force overwrite if task already exists
        string args = $"/Create /TN \"{TaskFolder}\\{taskName}\" " +
                      $"/TR \"\\\"{exePath}\\\" --recover\" " +
                      "/SC ONLOGON /RL HIGHEST /F";

        return RunSchtasks(args);
    }

    /// <summary>Deletes a previously registered scheduled task.</summary>
    public OperationResult DeleteTask(string taskName)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskName);

        string args = $"/Delete /TN \"{TaskFolder}\\{taskName}\" /F";
        return RunSchtasks(args);
    }

    /// <summary>Returns true if the named task currently exists in the task scheduler.</summary>
    public bool TaskExists(string taskName)
    {
        ArgumentException.ThrowIfNullOrEmpty(taskName);

        string args   = $"/Query /TN \"{TaskFolder}\\{taskName}\"";
        var    result = RunSchtasks(args);
        return result.IsSuccess;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static OperationResult RunSchtasks(string arguments)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName               = "schtasks.exe",
                Arguments              = arguments,
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            };

            using var process = System.Diagnostics.Process.Start(psi)
                             ?? throw new InvalidOperationException("Failed to start schtasks.exe");

            process.WaitForExit(30_000); // 30-second timeout

            if (process.ExitCode == 0)
                return OperationResult.Success();

            string error = process.StandardError.ReadToEnd();
            return OperationResult.Failure($"schtasks.exe failed (exit {process.ExitCode}): {error}", process.ExitCode);
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Failed to run schtasks.exe: {ex.Message}", exception: ex);
        }
    }
}
