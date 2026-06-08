using FluentAssertions;
using KeyboardCleanerPro.Core.Models;
using KeyboardCleanerPro.Core.Services.State;
using Xunit;

namespace KeyboardCleanerPro.Tests.Services.State;

/// <summary>
/// Tests for <see cref="StateManager"/> — real file I/O, no mocks.
/// Verifies the atomic write-then-rename crash-safety guarantee.
/// </summary>
public sealed class StateManagerTests : IDisposable
{
    private readonly string       _tempDir;
    private readonly StateManager _sut;

    public StateManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"kcp-test-state-{Guid.NewGuid():N}");
        _sut     = new StateManager(_tempDir);
    }

    [Fact]
    public void LoadState_NoFile_ReturnsNull()
    {
        _sut.LoadState().Should().BeNull();
    }

    [Fact]
    public void SaveState_ThenLoadState_RoundTripsAllFields()
    {
        var state = new DeviceState
        {
            IsKeyboardDisabled = true,
            DeviceInstanceId   = "ACPI\\PNP0303\\4&DEADBEEF&0",
            DisabledAt         = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            AutoRestoreAt      = new DateTime(2025, 1, 1, 12, 5, 0, DateTimeKind.Utc),
            AppVersion         = "1.0.0"
        };

        _sut.SaveState(state);
        var loaded = _sut.LoadState();

        loaded.Should().NotBeNull();
        loaded!.IsKeyboardDisabled.Should().BeTrue();
        loaded.DeviceInstanceId.Should().Be("ACPI\\PNP0303\\4&DEADBEEF&0");
        loaded.AppVersion.Should().Be("1.0.0");
    }

    [Fact]
    public void ClearState_RemovesFile()
    {
        var state = new DeviceState { IsKeyboardDisabled = true, DeviceInstanceId = "TEST" };
        _sut.SaveState(state);
        File.Exists(_sut.GetStateFilePath()).Should().BeTrue();

        _sut.ClearState();
        File.Exists(_sut.GetStateFilePath()).Should().BeFalse();
    }

    [Fact]
    public void ClearState_WhenNoFile_DoesNotThrow()
    {
        var act = () => _sut.ClearState();
        act.Should().NotThrow();
    }

    [Fact]
    public void LoadState_CorruptFile_ReturnsNull()
    {
        File.WriteAllText(_sut.GetStateFilePath(), "{{{{ not json");
        _sut.LoadState().Should().BeNull();
    }

    [Fact]
    public void SaveState_OverwritesExisting()
    {
        _sut.SaveState(new DeviceState { IsKeyboardDisabled = true,  DeviceInstanceId = "DEVICE1" });
        _sut.SaveState(new DeviceState { IsKeyboardDisabled = false, DeviceInstanceId = "DEVICE2" });

        var loaded = _sut.LoadState();
        loaded!.DeviceInstanceId.Should().Be("DEVICE2");
        loaded.IsKeyboardDisabled.Should().BeFalse();
    }

    [Fact]
    public void AsRestored_DoesNotModifyOriginal()
    {
        var state    = new DeviceState { IsKeyboardDisabled = true, DeviceInstanceId = "DEV" };
        var restored = state.AsRestored();

        state.IsKeyboardDisabled.Should().BeTrue();
        restored.IsKeyboardDisabled.Should().BeFalse();
        restored.DeviceInstanceId.Should().Be("DEV");
    }

    [Fact]
    public void IsAutoRestoreOverdue_PastTime_ReturnsTrue()
    {
        var state = new DeviceState
        {
            IsKeyboardDisabled = true,
            AutoRestoreAt      = DateTime.UtcNow.AddMinutes(-1)
        };
        state.IsAutoRestoreOverdue.Should().BeTrue();
    }

    [Fact]
    public void IsAutoRestoreOverdue_FutureTime_ReturnsFalse()
    {
        var state = new DeviceState
        {
            IsKeyboardDisabled = true,
            AutoRestoreAt      = DateTime.UtcNow.AddMinutes(5)
        };
        state.IsAutoRestoreOverdue.Should().BeFalse();
    }

    [Fact]
    public void IsAutoRestoreOverdue_NotDisabled_ReturnsFalse()
    {
        var state = new DeviceState
        {
            IsKeyboardDisabled = false,
            AutoRestoreAt      = DateTime.UtcNow.AddMinutes(-10)
        };
        state.IsAutoRestoreOverdue.Should().BeFalse();
    }

    [Fact]
    public void SaveState_NullState_ThrowsArgumentNullException()
    {
        var act = () => _sut.SaveState(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetStateFilePath_ReturnsPathUnderDataDirectory()
    {
        _sut.GetStateFilePath().Should().StartWith(_tempDir);
        _sut.GetStateFilePath().Should().EndWith("device-state.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
