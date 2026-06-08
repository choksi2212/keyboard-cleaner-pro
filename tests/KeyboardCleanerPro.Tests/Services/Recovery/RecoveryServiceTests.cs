using FluentAssertions;
using KeyboardCleanerPro.Core.Services.Logging;
using KeyboardCleanerPro.Core.Services.Recovery;
using Xunit;

namespace KeyboardCleanerPro.Tests.Services.Recovery;

/// <summary>
/// Tests for <see cref="RecoveryService"/> timer management logic.
/// Uses a real ScheduledTaskManager (which delegates to schtasks.exe).
/// Timer tests are kept fast by using sub-second delays.
/// </summary>
public sealed class RecoveryServiceTests : IDisposable
{
    private readonly string              _tempDir;
    private readonly LoggingService      _logger;
    private readonly ScheduledTaskManager _taskManager;
    private readonly RecoveryService     _sut;

    public RecoveryServiceTests()
    {
        _tempDir     = Path.Combine(Path.GetTempPath(), $"kcp-test-recovery-{Guid.NewGuid():N}");
        _logger      = new LoggingService(_tempDir, enabled: false);
        _taskManager = new ScheduledTaskManager();
        _sut         = new RecoveryService(_logger, _taskManager);
    }

    [Fact]
    public void Initially_IsTimerActive_IsFalse()
    {
        _sut.IsTimerActive.Should().BeFalse();
    }

    [Fact]
    public void Initially_RemainingTime_IsNull()
    {
        _sut.RemainingTime.Should().BeNull();
    }

    [Fact]
    public void StartAutoRestoreTimer_SetsIsTimerActiveTrue()
    {
        _sut.StartAutoRestoreTimer("DEV\\001", TimeSpan.FromMinutes(5), () => { });
        _sut.IsTimerActive.Should().BeTrue();
    }

    [Fact]
    public void StartAutoRestoreTimer_RemainingTimeIsPositive()
    {
        _sut.StartAutoRestoreTimer("DEV\\001", TimeSpan.FromMinutes(5), () => { });
        _sut.RemainingTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void CancelAutoRestoreTimer_AfterStart_SetsIsTimerActiveFalse()
    {
        _sut.StartAutoRestoreTimer("DEV\\001", TimeSpan.FromMinutes(5), () => { });
        _sut.CancelAutoRestoreTimer();
        _sut.IsTimerActive.Should().BeFalse();
    }

    [Fact]
    public async Task StartAutoRestoreTimer_FiresCallbackAfterDelay()
    {
        bool callbackFired = false;
        _sut.StartAutoRestoreTimer("DEV\\001", TimeSpan.FromMilliseconds(200), () => callbackFired = true);

        await Task.Delay(500);
        callbackFired.Should().BeTrue();
    }

    [Fact]
    public async Task CancelAutoRestoreTimer_PreventsCallbackFromFiring()
    {
        bool callbackFired = false;
        _sut.StartAutoRestoreTimer("DEV\\001", TimeSpan.FromMilliseconds(300), () => callbackFired = true);
        _sut.CancelAutoRestoreTimer();

        await Task.Delay(600);
        callbackFired.Should().BeFalse();
    }

    [Fact]
    public void StartAutoRestoreTimer_ReplacesExistingTimer()
    {
        int fireCount = 0;
        _sut.StartAutoRestoreTimer("DEV\\001", TimeSpan.FromMinutes(5), () => fireCount++);
        _sut.StartAutoRestoreTimer("DEV\\001", TimeSpan.FromMinutes(10), () => fireCount++);

        _sut.IsTimerActive.Should().BeTrue();
        // Only one timer should be active
    }

    [Fact]
    public void StartAutoRestoreTimer_NullOrEmptyDeviceId_ThrowsArgumentException()
    {
        var act = () => _sut.StartAutoRestoreTimer(string.Empty, TimeSpan.FromMinutes(1), () => { });
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void StartAutoRestoreTimer_NullCallback_ThrowsArgumentNullException()
    {
        var act = () => _sut.StartAutoRestoreTimer("DEV", TimeSpan.FromMinutes(1), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StartAutoRestoreTimer_NegativeOrZeroDelay_ThrowsArgumentOutOfRangeException()
    {
        var act = () => _sut.StartAutoRestoreTimer("DEV", TimeSpan.Zero, () => { });
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Dispose_CancelsActiveTimer()
    {
        _sut.StartAutoRestoreTimer("DEV", TimeSpan.FromMinutes(5), () => { });
        _sut.Dispose();
        // Should not throw and timer should be stopped
        _sut.IsTimerActive.Should().BeFalse();
    }

    public void Dispose()
    {
        _sut.Dispose();
        _logger.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
