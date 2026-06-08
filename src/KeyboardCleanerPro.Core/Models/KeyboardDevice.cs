namespace KeyboardCleanerPro.Core.Models;

/// <summary>
/// Represents a physical keyboard device detected on the system.
/// Immutable value object — create via constructor or object initializer.
/// </summary>
public sealed class KeyboardDevice
{
    public string   InstanceId      { get; init; } = string.Empty;
    public string   Description     { get; init; } = string.Empty;
    public string[] HardwareIds     { get; init; } = [];
    public string   ConnectionType  { get; init; } = string.Empty;
    public int      Score           { get; init; }
    public bool     IsEnabled       { get; init; }

    public KeyboardDevice() { }

    public KeyboardDevice(
        string   instanceId,
        string   description,
        string[] hardwareIds,
        string   connectionType,
        int      score,
        bool     isEnabled)
    {
        InstanceId     = instanceId;
        Description    = description;
        HardwareIds    = hardwareIds;
        ConnectionType = connectionType;
        Score          = score;
        IsEnabled      = isEnabled;
    }

    /// <summary>Returns a copy of this device with the enabled flag changed.</summary>
    public KeyboardDevice WithEnabled(bool isEnabled) =>
        new(InstanceId, Description, HardwareIds, ConnectionType, Score, isEnabled);

    public override bool Equals(object? obj) =>
        obj is KeyboardDevice other &&
        string.Equals(InstanceId, other.InstanceId, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(InstanceId);

    public override string ToString() =>
        $"{Description} [{ConnectionType}] Score={Score} Enabled={IsEnabled}";
}
