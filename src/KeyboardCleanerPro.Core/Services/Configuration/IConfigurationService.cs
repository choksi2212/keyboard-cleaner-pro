using KeyboardCleanerPro.Core.Models;

namespace KeyboardCleanerPro.Core.Services.Configuration;

/// <summary>
/// Reads and writes application settings from/to settings.json.
/// </summary>
public interface IConfigurationService
{
    AppSettings Load();
    void Save(AppSettings settings);
    string GetSettingsFilePath();
}
