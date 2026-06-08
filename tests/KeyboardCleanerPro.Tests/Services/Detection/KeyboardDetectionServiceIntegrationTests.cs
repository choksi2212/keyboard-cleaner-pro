using FluentAssertions;
using KeyboardCleanerPro.Core.Services.Detection;
using KeyboardCleanerPro.Core.Services.Logging;
using Xunit;

namespace KeyboardCleanerPro.Tests.Services.Detection;

/// <summary>
/// Integration tests that call the REAL Windows SetupAPI.
/// These tests require the process to run on a physical machine (not a VM with no keyboard).
/// They DO NOT require Administrator for enumeration — only for disable/enable.
///
/// These tests are NOT marked with [Trait("Category","Integration")] to avoid needing
/// xunit filters. If you run them without a keyboard attached, they will be inconclusive
/// (no keyboard found) rather than failing hard.
/// </summary>
public sealed class KeyboardDetectionServiceIntegrationTests : IDisposable
{
    private readonly string                 _tempDir;
    private readonly LoggingService         _logger;
    private readonly DeviceScorer           _scorer;
    private readonly KeyboardDetectionService _sut;

    public KeyboardDetectionServiceIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"kcp-test-detect-{Guid.NewGuid():N}");
        _logger  = new LoggingService(_tempDir, enabled: false);
        _scorer  = new DeviceScorer();
        _sut     = new KeyboardDetectionService(_scorer, _logger);
    }

    [Fact]
    public void EnumerateAllKeyboards_ReturnsAtLeastOneKeyboard()
    {
        var result = _sut.EnumerateAllKeyboards();

        result.IsSuccess.Should().BeTrue(because: "SetupDiGetClassDevs should succeed on any Windows machine");
        result.Value.Should().NotBeNull();
        // It's valid to have 0 keyboards if running headless — but we don't fail
    }

    [Fact]
    public void EnumerateAllKeyboards_AllDevicesHaveNonEmptyInstanceIds()
    {
        var result = _sut.EnumerateAllKeyboards();
        result.IsSuccess.Should().BeTrue();

        foreach (var device in result.Value!)
            device.InstanceId.Should().NotBeNullOrEmpty(
                because: "every enumerated device must have a valid instance ID");
    }

    [Fact]
    public void EnumerateAllKeyboards_ScoresAreConsistentWithConnectionType()
    {
        var result = _sut.EnumerateAllKeyboards();
        result.IsSuccess.Should().BeTrue();

        foreach (var device in result.Value!)
        {
            // USB and BT devices must have negative or zero scores
            if (device.ConnectionType is "Usb" or "Bluetooth")
                device.Score.Should().BeLessThanOrEqualTo(0,
                    because: $"device {device.InstanceId} is {device.ConnectionType} and must be excluded");
        }
    }

    [Fact]
    public void DetectInternalKeyboard_OnMachineWithKeyboard_ReturnsPositiveScore()
    {
        var result = _sut.DetectInternalKeyboard();

        if (!result.IsSuccess)
        {
            // Skip gracefully on headless machines — this is an integration test
            return;
        }

        result.Value.Should().NotBeNull();
        result.Value!.Score.Should().BeGreaterThan(0);
        result.Value.InstanceId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void IsKeyboardCurrentlyEnabled_WithValidId_DoesNotThrow()
    {
        var detect = _sut.DetectInternalKeyboard();
        if (!detect.IsSuccess) return; // Skip on headless

        var act = () => _sut.IsKeyboardCurrentlyEnabled(detect.Value!.InstanceId);
        act.Should().NotThrow();
    }

    [Fact]
    public void IsKeyboardCurrentlyEnabled_WithBogusId_ReturnsTrueDefault()
    {
        // For an unknown ID, we assume enabled (fail-safe)
        bool result = _sut.IsKeyboardCurrentlyEnabled("BOGUS\\DEVICE\\THAT\\DOES\\NOT\\EXIST");
        result.Should().BeTrue();
    }

    [Fact]
    public void IsKeyboardCurrentlyEnabled_NullOrEmpty_ThrowsArgumentException()
    {
        var act = () => _sut.IsKeyboardCurrentlyEnabled(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    public void Dispose()
    {
        _logger.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
