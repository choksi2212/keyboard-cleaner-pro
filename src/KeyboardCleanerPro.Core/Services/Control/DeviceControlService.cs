using System.Runtime.InteropServices;
using KeyboardCleanerPro.Core.Infrastructure.Native;
using KeyboardCleanerPro.Core.Models;
using KeyboardCleanerPro.Core.Services.Logging;
using static KeyboardCleanerPro.Core.Infrastructure.Native.NativeStructures;

namespace KeyboardCleanerPro.Core.Services.Control;

/// <summary>
/// Implements keyboard enable/disable by calling SetupDiCallClassInstaller with
/// DIF_PROPERTYCHANGE — the same mechanism used by Device Manager.
/// Requires the process to be elevated (Administrator).
/// </summary>
public sealed class DeviceControlService : IDeviceControlService
{
    private readonly ILoggingService _logger;

    public DeviceControlService(ILoggingService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public OperationResult DisableDevice(string deviceInstanceId) =>
        ChangeDeviceState(deviceInstanceId, NativeConstants.DICS_DISABLE, "DisableDevice");

    /// <inheritdoc />
    public OperationResult EnableDevice(string deviceInstanceId) =>
        ChangeDeviceState(deviceInstanceId, NativeConstants.DICS_ENABLE, "EnableDevice");

    // ── Core state-change implementation ──────────────────────────────────────

    private OperationResult ChangeDeviceState(string deviceInstanceId, uint desiredState, string operationName)
    {
        ArgumentException.ThrowIfNullOrEmpty(deviceInstanceId);

        var classGuid = NativeConstants.GUID_DEVCLASS_KEYBOARD;

        IntPtr deviceInfoSet = SetupApiNative.SetupDiGetClassDevs(
            ref classGuid, null, IntPtr.Zero, NativeConstants.DIGCF_PRESENT);

        if (deviceInfoSet == new IntPtr(-1))
        {
            int err = Marshal.GetLastWin32Error();
            _logger.Log(operationName, deviceInstanceId, "GetClassDevsFailed", err, Environment.UserName);
            return OperationResult.Failure($"SetupDiGetClassDevs failed (Win32={err}).", err);
        }

        try
        {
            return FindAndChangeDevice(deviceInfoSet, deviceInstanceId, desiredState, operationName);
        }
        finally
        {
            SetupApiNative.SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }
    }

    private OperationResult FindAndChangeDevice(
        IntPtr  deviceInfoSet,
        string  targetInstanceId,
        uint    desiredState,
        string  operationName)
    {
        var devInfoData = CreateDevInfoData();
        uint index = 0;

        while (SetupApiNative.SetupDiEnumDeviceInfo(deviceInfoSet, index, ref devInfoData))
        {
            string currentId = ReadInstanceId(deviceInfoSet, ref devInfoData);
            if (string.Equals(currentId, targetInstanceId, StringComparison.OrdinalIgnoreCase))
                return ApplyStateChange(deviceInfoSet, ref devInfoData, desiredState, targetInstanceId, operationName);

            index++;
            devInfoData = CreateDevInfoData();
        }

        _logger.Log(operationName, targetInstanceId, "DeviceNotFound", NativeConstants.ERROR_NOT_FOUND, Environment.UserName);
        return OperationResult.Failure(
            $"Device '{targetInstanceId}' was not found in the keyboard device class.",
            NativeConstants.ERROR_NOT_FOUND);
    }

    private OperationResult ApplyStateChange(
        IntPtr              deviceInfoSet,
        ref SP_DEVINFO_DATA devInfoData,
        uint                desiredState,
        string              instanceId,
        string              operationName)
    {
        var propChangeParams = new SP_PROPCHANGE_PARAMS
        {
            ClassInstallHeader = new SP_CLASSINSTALL_HEADER
            {
                cbSize          = (uint)Marshal.SizeOf<SP_CLASSINSTALL_HEADER>(),
                InstallFunction = NativeConstants.DIF_PROPERTYCHANGE
            },
            StateChange = desiredState,
            Scope       = NativeConstants.DICS_FLAG_GLOBAL,
            HwProfile   = 0
        };

        if (!SetupApiNative.SetupDiSetClassInstallParams(
                deviceInfoSet,
                ref devInfoData,
                ref propChangeParams,
                (uint)Marshal.SizeOf<SP_PROPCHANGE_PARAMS>()))
        {
            int err = Marshal.GetLastWin32Error();
            _logger.Log(operationName, instanceId, "SetClassInstallParamsFailed", err, Environment.UserName);
            return OperationResult.Failure($"SetupDiSetClassInstallParams failed (Win32={err}).", err);
        }

        if (!SetupApiNative.SetupDiCallClassInstaller(
                NativeConstants.DIF_PROPERTYCHANGE,
                deviceInfoSet,
                ref devInfoData))
        {
            int err = Marshal.GetLastWin32Error();
            _logger.Log(operationName, instanceId, "CallClassInstallerFailed", err, Environment.UserName);
            return OperationResult.Failure($"SetupDiCallClassInstaller failed (Win32={err}).", err);
        }

        string action = desiredState == NativeConstants.DICS_DISABLE ? "Disabled" : "Enabled";
        _logger.Log(operationName, instanceId, action, null, Environment.UserName);
        return OperationResult.Success();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SP_DEVINFO_DATA CreateDevInfoData() =>
        new() { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };

    private static string ReadInstanceId(IntPtr devInfoSet, ref SP_DEVINFO_DATA devInfoData)
    {
        var buffer = new char[NativeConstants.BUFFER_SIZE_SMALL];
        return SetupApiNative.SetupDiGetDeviceInstanceId(devInfoSet, ref devInfoData, buffer, (uint)buffer.Length, out _)
            ? new string(buffer).TrimEnd('\0')
            : string.Empty;
    }
}
