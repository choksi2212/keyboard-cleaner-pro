using System.Text.Json;
using System.Reflection;
using KeyboardCleanerPro.Core.Models;

namespace KeyboardCleanerPro.Core.Services.State;

/// <summary>
/// Atomically persists <see cref="DeviceState"/> to disk using a write-then-rename
/// strategy so the file is never left in a partially-written state during a crash.
/// </summary>
public sealed class StateManager : IStateManager
{
    private readonly string _stateFilePath;
    private readonly string _tempFilePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public StateManager(string dataDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataDirectory);
        Directory.CreateDirectory(dataDirectory);
        _stateFilePath = Path.Combine(dataDirectory, "device-state.json");
        _tempFilePath  = _stateFilePath + ".tmp";
    }

    /// <inheritdoc />
    public DeviceState? LoadState()
    {
        if (!File.Exists(_stateFilePath)) return null;

        try
        {
            string json  = File.ReadAllText(_stateFilePath);
            return JsonSerializer.Deserialize<DeviceState>(json, JsonOptions);
        }
        catch
        {
            // Corrupted state file — treat as no saved state
            return null;
        }
    }

    /// <inheritdoc />
    public void SaveState(DeviceState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        string json = JsonSerializer.Serialize(state, JsonOptions);

        // Write to temp first, then atomic rename — crash-safe
        File.WriteAllText(_tempFilePath, json);
        File.Move(_tempFilePath, _stateFilePath, overwrite: true);
    }

    /// <inheritdoc />
    public void ClearState()
    {
        if (File.Exists(_stateFilePath))
            File.Delete(_stateFilePath);

        if (File.Exists(_tempFilePath))
            File.Delete(_tempFilePath);
    }

    /// <inheritdoc />
    public string GetStateFilePath() => _stateFilePath;
}
