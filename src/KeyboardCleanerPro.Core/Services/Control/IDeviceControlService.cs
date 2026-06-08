using KeyboardCleanerPro.Core.Models;

namespace KeyboardCleanerPro.Core.Services.Control;

/// <summary>
/// Enables and disables physical keyboard devices via the Windows PnP manager.
/// All operations require Administrator privileges.
/// </summary>
public interface IDeviceControlService
{
    /// <summary>Disables the keyboard device identified by <paramref name="deviceInstanceId"/>.</summary>
    OperationResult DisableDevice(string deviceInstanceId);

    /// <summary>Re-enables the keyboard device identified by <paramref name="deviceInstanceId"/>.</summary>
    OperationResult EnableDevice(string deviceInstanceId);
}
