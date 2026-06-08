using KeyboardCleanerPro.Core.Models;

namespace KeyboardCleanerPro.Core.Services.State;

/// <summary>
/// Reads and writes the persisted <see cref="DeviceState"/> file used by the
/// recovery agent to restore the keyboard if the main process is killed.
/// </summary>
public interface IStateManager
{
    DeviceState? LoadState();
    void SaveState(DeviceState state);
    void ClearState();
    string GetStateFilePath();
}
