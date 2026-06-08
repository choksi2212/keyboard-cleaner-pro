using System.Runtime.InteropServices;
using KeyboardCleanerPro.Core.Models;
using KeyboardCleanerPro.Core.Services.Logging;

namespace KeyboardCleanerPro.Core.Services.Control;

/// <summary>
/// Implements keyboard blocking via a system-wide WH_KEYBOARD_LL hook.
/// This intercepts ALL keyboard input before it reaches any application —
/// equivalent to how every commercial keyboard-cleaner utility works.
///
/// Unlike the SetupAPI / ConfigMgr device-disable path, this:
///   - Works on ALL keyboard types (PS/2, ACPI, HID, USB, I2C)
///   - Doesn't require the device to be disableable (CR_NOT_DISABLEABLE safe)
///   - Is instantly reversible (unhook = keyboard back immediately)
///   - Requires no knowledge of device instance IDs
///
/// THREADING: <see cref="DisableDevice"/> MUST be called from the WPF UI /
/// Dispatcher thread because WH_KEYBOARD_LL callbacks are delivered via the
/// installing thread's message pump.
/// </summary>
public sealed class KeyboardHookControlService : IDeviceControlService, IDisposable
{
    // ── Win32 declarations ────────────────────────────────────────────────────
    private const int WH_KEYBOARD_LL = 13;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int                  idHook,
        LowLevelKeyboardProc lpfn,
        IntPtr               hMod,
        uint                 dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    // ── State (static so the native callback can reach it) ────────────────────

    /// <summary>Held in a field so the GC never collects the delegate while the hook is active.</summary>
    private static readonly LowLevelKeyboardProc _procDelegate = OnKeyboardEvent;

    /// <summary>volatile — written on UI thread, read inside the hook callback thread.</summary>
    private static volatile bool _isBlocking;

    private static IntPtr _hookHandle = IntPtr.Zero;

    // ── Service ───────────────────────────────────────────────────────────────
    private readonly ILoggingService _logger;

    public KeyboardHookControlService(ILoggingService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Hook callback ─────────────────────────────────────────────────────────

    /// <summary>
    /// Called by Windows for every system-wide keystroke.
    /// Returning a non-zero value swallows the key — it never reaches any app.
    /// </summary>
    private static IntPtr OnKeyboardEvent(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _isBlocking)
            return (IntPtr)1;                                      // ← swallow
        return CallNextHookEx(_hookHandle, nCode, wParam, lParam); // ← pass through
    }

    // ── IDeviceControlService ─────────────────────────────────────────────────

    /// <inheritdoc />
    /// <remarks>Must be called on the WPF Dispatcher (UI) thread.</remarks>
    public OperationResult DisableDevice(string deviceInstanceId)
    {
        if (_isBlocking)
            return OperationResult.Success(); // already blocking — idempotent

        // Set the flag BEFORE installing the hook so no key can slip through
        // even during the tiny window between the hook install and the first callback
        _isBlocking = true;

        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _procDelegate, IntPtr.Zero, 0);

        if (_hookHandle == IntPtr.Zero)
        {
            _isBlocking = false;
            int err = Marshal.GetLastWin32Error();
            _logger.Log(nameof(DisableDevice), deviceInstanceId,
                        "HookInstallFailed", err, Environment.UserName);
            return OperationResult.Failure(
                $"Could not install keyboard hook (Win32={err}). " +
                $"Make sure the application is running on the interactive desktop.", err);
        }

        _logger.Log(nameof(DisableDevice), deviceInstanceId,
                    "KeyboardHookInstalled", null, Environment.UserName);
        return OperationResult.Success();
    }

    /// <inheritdoc />
    public OperationResult EnableDevice(string deviceInstanceId)
    {
        _isBlocking = false; // clear first so no key is swallowed during unhook

        if (_hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
            _logger.Log(nameof(EnableDevice), deviceInstanceId,
                        "KeyboardHookRemoved", null, Environment.UserName);
        }

        return OperationResult.Success();
    }

    public void Dispose() => EnableDevice(string.Empty);
}
