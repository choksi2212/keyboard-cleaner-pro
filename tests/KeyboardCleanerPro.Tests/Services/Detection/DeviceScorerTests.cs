using FluentAssertions;
using KeyboardCleanerPro.Core.Services.Detection;
using KeyboardCleanerPro.Core.Models;
using Xunit;

namespace KeyboardCleanerPro.Tests.Services.Detection;

/// <summary>
/// Rigorously tests every scoring path in <see cref="DeviceScorer"/>.
/// No I/O, no Windows APIs — pure deterministic logic.
/// </summary>
public sealed class DeviceScorerTests
{
    private readonly DeviceScorer _sut = new();

    // ── ACPI devices — score = +50 ────────────────────────────────────────────

    [Theory]
    [InlineData("ACPI\\PNP0303\\4&deadbeef&0",         new string[0])]
    [InlineData("ACPI\\MSFT0001\\3&abcd1234&0",        new string[0])]
    [InlineData("SOMEID",                               new[] { "ACPI\\PNP0303" })]
    public void CalculateScore_AcpiDevice_Returns50(string instanceId, string[] hwIds)
    {
        int score = _sut.CalculateScore(instanceId, hwIds);
        score.Should().Be(50);
    }

    [Theory]
    [InlineData("ACPI\\PNP0303\\4&deadbeef&0",  new string[0])]
    [InlineData("ACPI\\MSFT0001\\3&abcd1234&0", new string[0])]
    public void DetermineConnectionType_AcpiDevice_ReturnsAcpi(string instanceId, string[] hwIds)
    {
        var type = _sut.DetermineConnectionType(instanceId, hwIds);
        type.Should().Be(DeviceConnectionType.Acpi);
    }

    // ── PS/2 devices — score = +40 ────────────────────────────────────────────

    [Theory]
    [InlineData("ROOT\\8042PRT\\0000",       new string[0])]
    [InlineData("SOMEID",                    new[] { "PNP0303" })]
    [InlineData("SOMETHING\\I8042PRT\\0000", new string[0])]
    public void CalculateScore_Ps2Device_Returns40(string instanceId, string[] hwIds)
    {
        // ACPI\PNP0303 hardware IDs are classified as ACPI (+50), not PS/2 (+40)
        // These test cases use non-ACPI-prefixed IDs to exercise the pure PS/2 path
        int score = _sut.CalculateScore(instanceId, hwIds);
        score.Should().Be(40);
    }

    [Fact]
    public void DetermineConnectionType_Ps2Device_ReturnsPs2()
    {
        var type = _sut.DetermineConnectionType("ROOT\\8042PRT\\0000", []);
        type.Should().Be(DeviceConnectionType.Ps2);
    }

    // ── I2C HID devices — score = +30 ────────────────────────────────────────

    [Theory]
    [InlineData("HID\\I2CHID\\1&2&3",           new string[0])]
    [InlineData("SOMEID",                        new[] { "HID\\I2C\\XXXX" })]
    public void CalculateScore_I2CDevice_Returns30(string instanceId, string[] hwIds)
    {
        int score = _sut.CalculateScore(instanceId, hwIds);
        score.Should().Be(30);
    }

    [Fact]
    public void DetermineConnectionType_I2CDevice_ReturnsI2C()
    {
        var type = _sut.DetermineConnectionType("HID\\I2CHID\\1&2&3", []);
        type.Should().Be(DeviceConnectionType.I2C);
    }

    // ── USB devices — score = -100 ────────────────────────────────────────────

    [Theory]
    [InlineData("USB\\VID_046D&PID_C31C\\6&abcd&0&1", new string[0])]
    [InlineData("HID\\VID_0000",                       new[] { "USB\\VID_0001" })]
    public void CalculateScore_UsbDevice_ReturnsMinus100(string instanceId, string[] hwIds)
    {
        int score = _sut.CalculateScore(instanceId, hwIds);
        score.Should().Be(-100);
    }

    [Fact]
    public void DetermineConnectionType_UsbDevice_ReturnsUsb()
    {
        var type = _sut.DetermineConnectionType("USB\\VID_046D&PID_C31C\\6&abcd&0&1", []);
        type.Should().Be(DeviceConnectionType.Usb);
    }

    // ── Bluetooth devices — score = -100 ─────────────────────────────────────

    [Theory]
    [InlineData("BTHENUM\\{00001124-0000-1000-8000-00805F9B34FB}_VID&01000A12_PID&0001\\...", new string[0])]
    [InlineData("SOMEID",                                                                       new[] { "BTH\\VID_0001" })]
    public void CalculateScore_BluetoothDevice_ReturnsMinus100(string instanceId, string[] hwIds)
    {
        int score = _sut.CalculateScore(instanceId, hwIds);
        score.Should().Be(-100);
    }

    [Fact]
    public void DetermineConnectionType_BluetoothDevice_ReturnsBluetooth()
    {
        var type = _sut.DetermineConnectionType("BTHENUM\\xxxx", []);
        type.Should().Be(DeviceConnectionType.Bluetooth);
    }

    // ── Unknown device — score = 0 ────────────────────────────────────────────

    [Fact]
    public void CalculateScore_UnknownDevice_Returns0()
    {
        int score = _sut.CalculateScore("ROOT\\UNKNOWN\\0000", []);
        score.Should().Be(0);
    }

    [Fact]
    public void DetermineConnectionType_UnknownDevice_ReturnsUnknown()
    {
        var type = _sut.DetermineConnectionType("ROOT\\UNKNOWN\\0000", []);
        type.Should().Be(DeviceConnectionType.Unknown);
    }

    // ── Guard clauses ─────────────────────────────────────────────────────────

    [Fact]
    public void CalculateScore_NullInstanceId_ThrowsArgumentException()
    {
        var act = () => _sut.CalculateScore(null!, []);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculateScore_EmptyInstanceId_ThrowsArgumentException()
    {
        var act = () => _sut.CalculateScore(string.Empty, []);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculateScore_NullHardwareIds_ThrowsArgumentNullException()
    {
        var act = () => _sut.CalculateScore("ACPI\\PNP0303", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── ACPI wins over PS/2 (higher score selected) ───────────────────────────

    [Fact]
    public void CalculateScore_AcpiInstanceIdWithPs2HwId_ReturnsAcpiScore()
    {
        // ACPI in instanceId = +50; PS/2 in hwId = +40 — but we don't stack, just classify once
        // ACPI check fires first → returns 50
        int score = _sut.CalculateScore("ACPI\\PNP0303\\4&DEAD&0", ["PNP0303"]);
        score.Should().Be(50);
    }

    // ── Case insensitivity ────────────────────────────────────────────────────

    [Fact]
    public void CalculateScore_LowercaseAcpi_IsRecognisedCorrectly()
    {
        int score = _sut.CalculateScore("acpi\\pnp0303\\4&dead&0", []);
        score.Should().Be(50);
    }

    [Fact]
    public void CalculateScore_LowercaseUsb_IsRecognisedCorrectly()
    {
        int score = _sut.CalculateScore("usb\\vid_046d&pid_c31c\\abc", []);
        score.Should().Be(-100);
    }
}
