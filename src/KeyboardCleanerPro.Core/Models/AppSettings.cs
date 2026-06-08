using System.Text.Json.Serialization;

namespace KeyboardCleanerPro.Core.Models;

/// <summary>
/// User-facing application settings persisted to settings.json.
/// </summary>
public sealed class AppSettings
{
    // ── Defaults ──────────────────────────────────────────────────────────────
    public const int    DefaultAutoRestoreMinutes = 5;
    public const bool   DefaultEnableLogging      = true;
    public const string DefaultTheme              = "Dark";
    public const bool   DefaultRecoveryEnabled    = true;
    public const int    MinAutoRestoreMinutes      = 1;
    public const int    MaxAutoRestoreMinutes      = 60;

    [JsonPropertyName("autoRestoreMinutes")]
    public int AutoRestoreMinutes { get; set; } = DefaultAutoRestoreMinutes;

    [JsonPropertyName("enableLogging")]
    public bool EnableLogging { get; set; } = DefaultEnableLogging;

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = DefaultTheme;

    [JsonPropertyName("recoveryEnabled")]
    public bool RecoveryEnabled { get; set; } = DefaultRecoveryEnabled;

    /// <summary>
    /// Clamps any out-of-range values back to safe defaults.
    /// Called after deserializing settings from disk.
    /// </summary>
    public void Validate()
    {
        if (AutoRestoreMinutes < MinAutoRestoreMinutes || AutoRestoreMinutes > MaxAutoRestoreMinutes)
            AutoRestoreMinutes = DefaultAutoRestoreMinutes;

        if (string.IsNullOrWhiteSpace(Theme))
            Theme = DefaultTheme;
    }

    /// <summary>Returns a default-populated settings instance.</summary>
    public static AppSettings CreateDefault() => new();
}
