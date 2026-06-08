using KeyboardCleanerPro.Core.Models;

namespace KeyboardCleanerPro.Core.Services.Detection;

/// <summary>
/// Enumerates keyboard devices from the Windows device manager and selects
/// the best internal-keyboard candidate using the spec §13 scoring model.
/// </summary>
public interface IKeyboardDetectionService
{
    /// <summary>Detects and returns the internal keyboard with the highest positive score.</summary>
    OperationResult<KeyboardDevice> DetectInternalKeyboard();

    /// <summary>Returns all keyboard-class devices present on the system.</summary>
    OperationResult<IReadOnlyList<KeyboardDevice>> EnumerateAllKeyboards();

    /// <summary>Queries Windows to determine whether a specific device is currently enabled.</summary>
    bool IsKeyboardCurrentlyEnabled(string deviceInstanceId);
}
