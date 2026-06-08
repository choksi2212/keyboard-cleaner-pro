using System.Text.Json;
using KeyboardCleanerPro.Core.Models;

namespace KeyboardCleanerPro.Core.Services.Configuration;

/// <summary>
/// Persists <see cref="AppSettings"/> as JSON to %ProgramData%\KeyboardCleaner\settings.json.
/// Falls back to default settings if the file is missing or malformed — never throws to the caller.
/// </summary>
public sealed class ConfigurationService : IConfigurationService
{
    private readonly string _settingsFilePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented       = true,
        PropertyNameCaseInsensitive = true
    };

    public ConfigurationService(string dataDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataDirectory);
        Directory.CreateDirectory(dataDirectory);
        _settingsFilePath = Path.Combine(dataDirectory, "settings.json");
    }

    /// <inheritdoc />
    public AppSettings Load()
    {
        if (!File.Exists(_settingsFilePath))
        {
            var defaults = AppSettings.CreateDefault();
            Save(defaults); // Materialise defaults on first run
            return defaults;
        }

        try
        {
            string json     = File.ReadAllText(_settingsFilePath);
            var    settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                           ?? AppSettings.CreateDefault();
            settings.Validate();
            return settings;
        }
        catch (JsonException)
        {
            // Corrupted settings file — reset to defaults
            var defaults = AppSettings.CreateDefault();
            Save(defaults);
            return defaults;
        }
    }

    /// <inheritdoc />
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        string json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsFilePath, json);
    }

    /// <inheritdoc />
    public string GetSettingsFilePath() => _settingsFilePath;
}
