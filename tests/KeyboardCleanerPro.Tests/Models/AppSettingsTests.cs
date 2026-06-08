using FluentAssertions;
using KeyboardCleanerPro.Core.Models;
using Xunit;

namespace KeyboardCleanerPro.Tests.Models;

/// <summary>Tests for <see cref="AppSettings"/> defaults and validation.</summary>
public sealed class AppSettingsTests
{
    [Fact]
    public void CreateDefault_HasExpectedDefaults()
    {
        var settings = AppSettings.CreateDefault();

        settings.AutoRestoreMinutes.Should().Be(AppSettings.DefaultAutoRestoreMinutes);
        settings.EnableLogging.Should().Be(AppSettings.DefaultEnableLogging);
        settings.Theme.Should().Be(AppSettings.DefaultTheme);
        settings.RecoveryEnabled.Should().Be(AppSettings.DefaultRecoveryEnabled);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(61)]
    [InlineData(int.MaxValue)]
    public void Validate_OutOfRangeAutoRestoreMinutes_ClampsToDefault(int invalid)
    {
        var settings = new AppSettings { AutoRestoreMinutes = invalid };
        settings.Validate();
        settings.AutoRestoreMinutes.Should().Be(AppSettings.DefaultAutoRestoreMinutes);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(60)]
    public void Validate_ValidAutoRestoreMinutes_Unchanged(int valid)
    {
        var settings = new AppSettings { AutoRestoreMinutes = valid };
        settings.Validate();
        settings.AutoRestoreMinutes.Should().Be(valid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NullOrWhitespaceTheme_ResetsToDefault(string? theme)
    {
        var settings = new AppSettings { Theme = theme! };
        settings.Validate();
        settings.Theme.Should().Be(AppSettings.DefaultTheme);
    }

    [Fact]
    public void Validate_ValidTheme_Unchanged()
    {
        var settings = new AppSettings { Theme = "Light" };
        settings.Validate();
        settings.Theme.Should().Be("Light");
    }
}
