using System.Runtime.InteropServices;
using System.Text;
using KeyboardCleanerPro.Core.Infrastructure.Native;
using KeyboardCleanerPro.Core.Models;
using KeyboardCleanerPro.Core.Services.Logging;
using static KeyboardCleanerPro.Core.Infrastructure.Native.NativeStructures;

namespace KeyboardCleanerPro.Core.Services.Detection;

/// <summary>
/// Implements keyboard detection by calling SetupAPI to enumerate all devices in the
/// keyboard device class (GUID_DEVCLASS_KEYBOARD) and scoring them with <see cref="DeviceScorer"/>.
/// </summary>
public sealed class KeyboardDetectionService : IKeyboardDetectionService
{
    private readonly DeviceScorer    _scorer;
    private readonly ILoggingService _logger;

    public KeyboardDetectionService(DeviceScorer scorer, ILoggingService logger)
    {
        _scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public OperationResult<KeyboardDevice> DetectInternalKeyboard()
    {
        var enumResult = EnumerateAllKeyboards();
        if (!enumResult.IsSuccess)
            return OperationResult<KeyboardDevice>.Failure(enumResult.ErrorMessage!, enumResult.ErrorCode, enumResult.Exception);

        var candidate = enumResult.Value!
            .Where(k => k.Score > 0)
            .OrderByDescending(k => k.Score)
            .FirstOrDefault();

        if (candidate is null)
        {
            _logger.Log(nameof(DetectInternalKeyboard), "none", "NoInternalKeyboardFound", null, Environment.UserName);
            return OperationResult<KeyboardDevice>.Failure(
                "No internal keyboard device detected. Make sure you are running on a laptop with a built-in keyboard.");
        }

        _logger.Log(nameof(DetectInternalKeyboard), candidate.InstanceId, "Detected", null, Environment.UserName);
        return OperationResult<KeyboardDevice>.Success(candidate);
    }

    /// <inheritdoc />
    public OperationResult<IReadOnlyList<KeyboardDevice>> EnumerateAllKeyboards()
    {
        var keyboards = new List<KeyboardDevice>();
        var classGuid = NativeConstants.GUID_DEVCLASS_KEYBOARD;

        IntPtr deviceInfoSet = SetupApiNative.SetupDiGetClassDevs(
            ref classGuid,
            null,
            IntPtr.Zero,
            NativeConstants.DIGCF_PRESENT);

        if (deviceInfoSet == new IntPtr(-1))
        {
            int err = Marshal.GetLastWin32Error();
            _logger.Log(nameof(EnumerateAllKeyboards), "all", "SetupDiGetClassDevsFailed", err, Environment.UserName);
            return OperationResult<IReadOnlyList<KeyboardDevice>>.Failure(
                $"SetupDiGetClassDevs failed (Win32 error {err}).", err);
        }

        try
        {
            uint index = 0;
            var  devInfoData = CreateDevInfoData();

            while (SetupApiNative.SetupDiEnumDeviceInfo(deviceInfoSet, index, ref devInfoData))
            {
                var device = TryBuildDevice(deviceInfoSet, ref devInfoData);
                if (device is not null)
                    keyboards.Add(device);

                index++;
                devInfoData = CreateDevInfoData(); // reset struct for next iteration
            }

            // ERROR_NO_MORE_ITEMS (259) is the expected loop-termination code
            int loopErr = Marshal.GetLastWin32Error();
            if (loopErr != NativeConstants.ERROR_NO_MORE_ITEMS)
            {
                _logger.Log(nameof(EnumerateAllKeyboards), "all",
                    $"PartialEnumeration-Win32={loopErr}", loopErr, Environment.UserName);
            }
        }
        finally
        {
            SetupApiNative.SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }

        _logger.Log(nameof(EnumerateAllKeyboards), "all", $"Found {keyboards.Count} keyboards", null, Environment.UserName);
        return OperationResult<IReadOnlyList<KeyboardDevice>>.Success(keyboards.AsReadOnly());
    }

    /// <inheritdoc />
    public bool IsKeyboardCurrentlyEnabled(string deviceInstanceId)
    {
        ArgumentException.ThrowIfNullOrEmpty(deviceInstanceId);

        var result = EnumerateAllKeyboards();
        if (!result.IsSuccess) return true; // Assume enabled if we can't query

        return result.Value!
            .FirstOrDefault(k => string.Equals(k.InstanceId, deviceInstanceId, StringComparison.OrdinalIgnoreCase))
            ?.IsEnabled ?? true;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static SP_DEVINFO_DATA CreateDevInfoData() =>
        new() { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };

    private KeyboardDevice? TryBuildDevice(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA devInfoData)
    {
        string instanceId = ReadInstanceId(deviceInfoSet, ref devInfoData);
        if (string.IsNullOrEmpty(instanceId)) return null;

        string   description = ReadStringProperty(deviceInfoSet, ref devInfoData, NativeConstants.SPDRP_DEVICEDESC);
        string[] hardwareIds = ReadMultiStringProperty(deviceInfoSet, ref devInfoData, NativeConstants.SPDRP_HARDWAREID);
        bool     isEnabled   = QueryDeviceEnabledState(devInfoData.DevInst);

        int    score          = _scorer.CalculateScore(instanceId, hardwareIds);
        string connectionType = _scorer.DetermineConnectionType(instanceId, hardwareIds).ToString();

        return new KeyboardDevice(instanceId, description, hardwareIds, connectionType, score, isEnabled);
    }

    private static string ReadInstanceId(IntPtr devInfoSet, ref SP_DEVINFO_DATA devInfoData)
    {
        var buffer = new char[NativeConstants.BUFFER_SIZE_SMALL];
        return SetupApiNative.SetupDiGetDeviceInstanceId(devInfoSet, ref devInfoData, buffer, (uint)buffer.Length, out _)
            ? new string(buffer).TrimEnd('\0')
            : string.Empty;
    }

    private static string ReadStringProperty(IntPtr devInfoSet, ref SP_DEVINFO_DATA devInfoData, uint property)
    {
        var buffer = new byte[NativeConstants.BUFFER_SIZE_SMALL * 2];
        return SetupApiNative.SetupDiGetDeviceRegistryProperty(
                devInfoSet, ref devInfoData, property, out _, buffer, (uint)buffer.Length, out _)
            ? Encoding.Unicode.GetString(buffer).TrimEnd('\0')
            : string.Empty;
    }

    private static string[] ReadMultiStringProperty(IntPtr devInfoSet, ref SP_DEVINFO_DATA devInfoData, uint property)
    {
        var buffer = new byte[NativeConstants.BUFFER_SIZE_LARGE];
        if (!SetupApiNative.SetupDiGetDeviceRegistryProperty(
                devInfoSet, ref devInfoData, property, out _, buffer, (uint)buffer.Length, out _))
            return [];

        string raw = Encoding.Unicode.GetString(buffer);
        return raw.Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Returns true when the device node is started (not disabled, not problem state).
    /// Uses CM_Get_DevNode_Status: if problem code is CM_PROB_DISABLED (22) the device is off.
    /// </summary>
    private static bool QueryDeviceEnabledState(uint devInst)
    {
        uint cr = CfgMgrNative.CM_Get_DevNode_Status(out _, out uint problem, devInst, 0);
        if (cr != NativeConstants.CR_SUCCESS) return true; // Can't tell → assume enabled
        return problem != NativeConstants.CM_PROB_DISABLED;
    }
}
