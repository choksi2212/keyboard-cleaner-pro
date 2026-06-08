using FluentAssertions;
using KeyboardCleanerPro.Core.Models;
using Xunit;

namespace KeyboardCleanerPro.Tests.Models;

/// <summary>Tests for the <see cref="KeyboardDevice"/> model.</summary>
public sealed class KeyboardDeviceTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var device = new KeyboardDevice(
            instanceId:    "ACPI\\PNP0303\\1",
            description:   "Standard PS/2 Keyboard",
            hardwareIds:   ["ACPI\\PNP0303"],
            connectionType: "Acpi",
            score:         50,
            isEnabled:     true);

        device.InstanceId.Should().Be("ACPI\\PNP0303\\1");
        device.Description.Should().Be("Standard PS/2 Keyboard");
        device.HardwareIds.Should().ContainSingle().Which.Should().Be("ACPI\\PNP0303");
        device.ConnectionType.Should().Be("Acpi");
        device.Score.Should().Be(50);
        device.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void WithEnabled_ReturnsNewInstanceWithUpdatedFlag()
    {
        var original = new KeyboardDevice("ID1", "Desc", [], "Acpi", 50, isEnabled: true);
        var disabled = original.WithEnabled(false);

        original.IsEnabled.Should().BeTrue();
        disabled.IsEnabled.Should().BeFalse();
        disabled.InstanceId.Should().Be(original.InstanceId);
    }

    [Fact]
    public void Equals_SameInstanceId_AreEqual()
    {
        var a = new KeyboardDevice("ACPI\\PNP0303", "KB1", [], "Acpi", 50, true);
        var b = new KeyboardDevice("ACPI\\PNP0303", "KB2", [], "Ps2",  40, false);
        a.Should().Be(b);
    }

    [Fact]
    public void Equals_DifferentInstanceId_AreNotEqual()
    {
        var a = new KeyboardDevice("ACPI\\PNP0303", "KB1", [], "Acpi", 50, true);
        var b = new KeyboardDevice("ACPI\\PNP0304", "KB2", [], "Acpi", 50, true);
        a.Should().NotBe(b);
    }

    [Fact]
    public void Equals_IsCaseInsensitive()
    {
        var a = new KeyboardDevice("acpi\\pnp0303", "KB1", [], "Acpi", 50, true);
        var b = new KeyboardDevice("ACPI\\PNP0303", "KB2", [], "Acpi", 50, true);
        a.Should().Be(b);
    }

    [Fact]
    public void GetHashCode_SameInstanceId_AreEqual()
    {
        var a = new KeyboardDevice("ACPI\\PNP0303", "KB1", [], "Acpi", 50, true);
        var b = new KeyboardDevice("ACPI\\PNP0303", "KB2", [], "Acpi", 50, true);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsDescription()
    {
        var device = new KeyboardDevice("ID", "My Keyboard", [], "Acpi", 50, true);
        device.ToString().Should().Contain("My Keyboard");
    }

    [Fact]
    public void DefaultConstructor_HasEmptyCollections()
    {
        var device = new KeyboardDevice();
        device.HardwareIds.Should().BeEmpty();
        device.InstanceId.Should().BeEmpty();
    }
}
