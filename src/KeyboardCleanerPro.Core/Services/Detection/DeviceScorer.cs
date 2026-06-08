using KeyboardCleanerPro.Core.Models;

namespace KeyboardCleanerPro.Core.Services.Detection;

/// <summary>
/// Pure scoring function that assigns each keyboard device a priority score
/// based on the spec §13 heuristics.
/// ACPI=+50, PS/2=+40, I2C=+30, USB=-100, Bluetooth=-100.
///
/// This class contains zero I/O or Windows API calls and is fully unit-testable.
/// </summary>
public sealed class DeviceScorer
{
    // ── Scoring weights (from spec §13) ───────────────────────────────────────
    private const int AcpiScore         = 50;
    private const int Ps2Score          = 40;
    private const int I2CScore          = 30;
    private const int UsbPenalty        = -100;
    private const int BluetoothPenalty  = -100;

    // ── Classification markers ────────────────────────────────────────────────
    private static readonly string[] Ps2Markers = ["8042", "PNP0303", "PNP0320", "I8042PRT", "KBDHID"];
    private static readonly string[] BthMarkers = ["BTH\\", "BTHENUM", "BLUETOOTH"];
    private static readonly string[] I2CMarkers = ["I2C", "HID\\ACPI"];

    /// <summary>
    /// Calculates the internal-keyboard likelihood score for a device.
    /// A positive score indicates an internal keyboard candidate.
    /// A score ≤ 0 means the device should be excluded.
    /// </summary>
    public int CalculateScore(string instanceId, string[] hardwareIds)
    {
        ArgumentException.ThrowIfNullOrEmpty(instanceId);
        ArgumentNullException.ThrowIfNull(hardwareIds);

        string upper   = instanceId.ToUpperInvariant();
        string[] hwUpper = hardwareIds.Select(h => h.ToUpperInvariant()).ToArray();

        int score = 0;

        // Apply penalties first — USB and Bluetooth devices must be excluded
        if (IsUsb(upper, hwUpper))       score += UsbPenalty;
        if (IsBluetooth(upper, hwUpper)) score += BluetoothPenalty;

        // Only award positive points if the device isn't already penalised out
        if (score >= 0)
        {
            if (IsAcpi(upper, hwUpper))      score += AcpiScore;
            else if (IsPs2(upper, hwUpper))  score += Ps2Score;
            else if (IsI2C(upper, hwUpper))  score += I2CScore;
        }

        return score;
    }

    /// <summary>Determines the connection type for display purposes.</summary>
    public DeviceConnectionType DetermineConnectionType(string instanceId, string[] hardwareIds)
    {
        ArgumentException.ThrowIfNullOrEmpty(instanceId);
        ArgumentNullException.ThrowIfNull(hardwareIds);

        string upper   = instanceId.ToUpperInvariant();
        string[] hwUpper = hardwareIds.Select(h => h.ToUpperInvariant()).ToArray();

        if (IsUsb(upper, hwUpper))       return DeviceConnectionType.Usb;
        if (IsBluetooth(upper, hwUpper)) return DeviceConnectionType.Bluetooth;
        if (IsAcpi(upper, hwUpper))      return DeviceConnectionType.Acpi;
        if (IsPs2(upper, hwUpper))       return DeviceConnectionType.Ps2;
        if (IsI2C(upper, hwUpper))       return DeviceConnectionType.I2C;
        return DeviceConnectionType.Unknown;
    }

    // ── Classification predicates ─────────────────────────────────────────────

    private static bool IsUsb(string instanceId, string[] hwIds) =>
        instanceId.StartsWith("USB\\")          ||
        instanceId.Contains("\\USB\\")          ||
        hwIds.Any(h => h.StartsWith("USB\\"));

    private static bool IsBluetooth(string instanceId, string[] hwIds) =>
        BthMarkers.Any(m => instanceId.Contains(m, StringComparison.Ordinal)) ||
        hwIds.Any(h => BthMarkers.Any(m => h.Contains(m, StringComparison.Ordinal)));

    private static bool IsAcpi(string instanceId, string[] hwIds) =>
        instanceId.StartsWith("ACPI\\") ||
        hwIds.Any(h => h.StartsWith("ACPI\\"));

    private static bool IsPs2(string instanceId, string[] hwIds) =>
        Ps2Markers.Any(m => instanceId.Contains(m, StringComparison.Ordinal)) ||
        hwIds.Any(h => Ps2Markers.Any(m => h.Contains(m, StringComparison.Ordinal)));

    private static bool IsI2C(string instanceId, string[] hwIds) =>
        I2CMarkers.Any(m => instanceId.Contains(m, StringComparison.Ordinal)) ||
        hwIds.Any(h => I2CMarkers.Any(m => h.Contains(m, StringComparison.Ordinal)));
}
