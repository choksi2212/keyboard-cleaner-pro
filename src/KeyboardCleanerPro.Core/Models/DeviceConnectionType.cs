namespace KeyboardCleanerPro.Core.Models;

/// <summary>Connection bus type for a keyboard device.</summary>
public enum DeviceConnectionType
{
    Unknown,
    Acpi,
    Ps2,
    I2C,
    Usb,
    Bluetooth
}
