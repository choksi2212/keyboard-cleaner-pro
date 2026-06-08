using System.Diagnostics;
using System.Security.Principal;

namespace KeyboardCleanerPro;

/// <summary>
/// Provides UAC elevation verification and re-launch-as-admin capability.
/// </summary>
internal static class AdminGuard
{
    /// <summary>Returns true when the current process token has the Administrator role.</summary>
    public static bool IsRunningAsAdmin()
    {
        using var identity  = WindowsIdentity.GetCurrent();
        var       principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>Re-launches the current executable with UAC elevation request.</summary>
    public static void RelaunchAsAdmin()
    {
        string exePath = Process.GetCurrentProcess().MainModule?.FileName
                      ?? Environment.ProcessPath
                      ?? throw new InvalidOperationException("Cannot determine executable path.");

        var psi = new ProcessStartInfo
        {
            FileName        = exePath,
            UseShellExecute = true,
            Verb            = "runas"
        };

        try
        {
            Process.Start(psi);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User declined UAC prompt — silently exit
        }
    }
}
