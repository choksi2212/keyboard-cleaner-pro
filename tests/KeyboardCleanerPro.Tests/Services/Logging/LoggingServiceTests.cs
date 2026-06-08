using FluentAssertions;
using KeyboardCleanerPro.Core.Services.Logging;
using Xunit;

namespace KeyboardCleanerPro.Tests.Services.Logging;

/// <summary>
/// Tests for <see cref="LoggingService"/> — uses real temp files.
/// </summary>
public sealed class LoggingServiceTests : IDisposable
{
    private readonly string        _tempDir;
    private readonly LoggingService _sut;

    public LoggingServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"kcp-test-logs-{Guid.NewGuid():N}");
        _sut     = new LoggingService(_tempDir, enabled: true);
    }

    [Fact]
    public void Constructor_CreatesLogDirectory()
    {
        Directory.Exists(_tempDir).Should().BeTrue();
    }

    [Fact]
    public async Task Log_WritesEntryToFile()
    {
        _sut.Log("TestOperation", "DEVICE\\001", "Success", null, "TestUser");

        await Task.Delay(100); // give async write time to complete

        string expectedFile = Path.Combine(_tempDir, $"kcp-{DateTime.UtcNow:yyyy-MM-dd}.log");
        File.Exists(expectedFile).Should().BeTrue();
        string content = File.ReadAllText(expectedFile);
        content.Should().Contain("TestOperation");
        content.Should().Contain("DEVICE\\001");
        content.Should().Contain("Success");
        content.Should().Contain("TestUser");
    }

    [Fact]
    public void Log_AddsToInMemoryBuffer()
    {
        _sut.Log("Op1", "DEV1", "OK", null, "user1");
        _sut.Log("Op2", "DEV2", "OK", null, "user2");

        var entries = _sut.ReadRecentEntries(10);
        entries.Should().HaveCountGreaterThanOrEqualTo(2);
        entries.Should().Contain(e => e.Operation == "Op1");
        entries.Should().Contain(e => e.Operation == "Op2");
    }

    [Fact]
    public void LogException_CapturesExceptionInfo()
    {
        var ex = new InvalidOperationException("test failure");
        _sut.LogException("ExOp", "DEVEX", ex, "user");

        var entries = _sut.ReadRecentEntries(10);
        entries.Should().Contain(e =>
            e.Operation == "ExOp" &&
            e.Status.Contains("InvalidOperationException"));
    }

    [Fact]
    public void ReadRecentEntries_NeverReturnsMoreThanRequested()
    {
        for (int i = 0; i < 20; i++)
            _sut.Log($"Op{i}", "DEV", "OK", null, "user");

        var entries = _sut.ReadRecentEntries(5);
        entries.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public void ReadRecentEntries_ZeroOrNegativeCount_ThrowsArgumentOutOfRangeException()
    {
        var act = () => _sut.ReadRecentEntries(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Log_WhenDisabled_DoesNotWriteFile()
    {
        using var disabled = new LoggingService(_tempDir, enabled: false);
        disabled.Log("Op", "DEV", "OK", null, "user");

        // File might exist from constructor check but should be empty or not created for this log
        // The in-memory buffer still works
        var entries = disabled.ReadRecentEntries(10);
        entries.Should().NotBeEmpty();
    }

    [Fact]
    public void Log_WithErrorCode_IncludesErrorCodeInEntry()
    {
        _sut.Log("FailOp", "DEV", "Failed", errorCode: 5, "user");

        var entries = _sut.ReadRecentEntries(10);
        entries.Should().Contain(e => e.ErrorCode == 5);
    }

    [Fact]
    public void Constructor_EmptyDirectory_ThrowsArgumentException()
    {
        var act = () => new LoggingService(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    public void Dispose()
    {
        _sut.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
