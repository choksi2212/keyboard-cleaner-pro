using System.Text.Json.Serialization;

namespace KeyboardCleanerPro.Core.Models;

/// <summary>
/// Persisted device state written to disk before disabling the keyboard so the recovery
/// agent can restore the device even if the main process is killed.
/// </summary>
public sealed class DeviceState
{
    [JsonPropertyName("isKeyboardDisabled")]
    public bool IsKeyboardDisabled { get; init; }

    [JsonPropertyName("deviceInstanceId")]
    public string DeviceInstanceId { get; init; } = string.Empty;

    [JsonPropertyName("disabledAt")]
    public DateTime? DisabledAt { get; init; }

    [JsonPropertyName("autoRestoreAt")]
    public DateTime? AutoRestoreAt { get; init; }

    [JsonPropertyName("appVersion")]
    public string AppVersion { get; init; } = string.Empty;

    /// <summary>True if the automatic restore window has already passed.</summary>
    [JsonIgnore]
    public bool IsAutoRestoreOverdue =>
        IsKeyboardDisabled &&
        AutoRestoreAt.HasValue &&
        DateTime.UtcNow > AutoRestoreAt.Value;

    /// <summary>Returns a new state representing the keyboard being restored.</summary>
    public DeviceState AsRestored() =>
        new()
        {
            IsKeyboardDisabled = false,
            DeviceInstanceId   = DeviceInstanceId,
            DisabledAt         = DisabledAt,
            AutoRestoreAt      = AutoRestoreAt,
            AppVersion         = AppVersion
        };
}
