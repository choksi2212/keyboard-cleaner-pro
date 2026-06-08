namespace KeyboardCleanerPro.Core.Models;

/// <summary>
/// Represents all possible application states in the state machine.
/// </summary>
public enum AppState
{
    /// <summary>Initial state before any detection has run.</summary>
    Idle,

    /// <summary>Actively scanning for the internal keyboard device.</summary>
    Detecting,

    /// <summary>Keyboard device detected and currently enabled.</summary>
    KeyboardEnabled,

    /// <summary>In the process of disabling the keyboard device.</summary>
    Disabling,

    /// <summary>Keyboard device is currently disabled. Timer is running.</summary>
    KeyboardDisabled,

    /// <summary>In the process of re-enabling the keyboard device.</summary>
    Enabling,

    /// <summary>Recovery process is running (e.g. auto-restore timer fired).</summary>
    Recovering,

    /// <summary>An unrecoverable error has occurred.</summary>
    Error
}
