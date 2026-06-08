using FluentAssertions;
using KeyboardCleanerPro.Core.Models;
using KeyboardCleanerPro.Core.Services.Configuration;
using Xunit;

namespace KeyboardCleanerPro.Tests.Services.Configuration;

/// <summary>
/// Tests for <see cref="ConfigurationService"/> — uses a real temp directory so
/// all file I/O is exercised without any mocks.
/// </summary>
public sealed class ConfigurationServiceTests : IDisposable
{
    private readonly string             _tempDir;
    private readonly ConfigurationService _sut;

    public ConfigurationServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"kcp-test-config-{Guid.NewGuid():N}");
        _sut     = new ConfigurationService(_tempDir);
    }

    [Fact]
    public void Load_NoFile_ReturnsDefaults()
    {
        var settings = _sut.Load();
        settings.AutoRestoreMinutes.Should().Be(AppSettings.DefaultAutoRestoreMinutes);
        settings.EnableLogging.Should().Be(AppSettings.DefaultEnableLogging);
    }

    [Fact]
    public void Load_NoFile_CreatesSettingsFile()
    {
        _sut.Load();
        File.Exists(_sut.GetSettingsFilePath()).Should().BeTrue();
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsAllProperties()
    {
        var original = new AppSettings
        {
            AutoRestoreMinutes = 10,
            EnableLogging      = false,
            Theme              = "Light",
            RecoveryEnabled    = false
        };

        _sut.Save(original);
        var loaded = _sut.Load();

        loaded.AutoRestoreMinutes.Should().Be(10);
        loaded.EnableLogging.Should().BeFalse();
        loaded.Theme.Should().Be("Light");
        loaded.RecoveryEnabled.Should().BeFalse();
    }

    [Fact]
    public void Load_CorruptedJson_ReturnsDefaults()
    {
        // Write intentionally invalid JSON
        File.WriteAllText(_sut.GetSettingsFilePath(), "{ this is not json at all !!!!");

        var settings = _sut.Load();
        settings.AutoRestoreMinutes.Should().Be(AppSettings.DefaultAutoRestoreMinutes);
    }

    [Fact]
    public void Save_OutOfRangeValues_ClampsBeforePersisting()
    {
        var settings = new AppSettings { AutoRestoreMinutes = 9999 };
        _sut.Save(settings);
        var loaded = _sut.Load();
        loaded.AutoRestoreMinutes.Should().Be(AppSettings.DefaultAutoRestoreMinutes);
    }

    [Fact]
    public void GetSettingsFilePath_ReturnsPathUnderDataDirectory()
    {
        _sut.GetSettingsFilePath().Should().StartWith(_tempDir);
        _sut.GetSettingsFilePath().Should().EndWith("settings.json");
    }

    [Fact]
    public void Constructor_CreatesDirectory_WhenNotExists()
    {
        string newDir = Path.Combine(_tempDir, "subdir");
        var svc = new ConfigurationService(newDir);
        Directory.Exists(newDir).Should().BeTrue();
    }

    [Fact]
    public void Save_NullSettings_ThrowsArgumentNullException()
    {
        var act = () => _sut.Save(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
