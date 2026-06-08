using KeyboardCleanerPro.Core.Models;
using KeyboardCleanerPro.Core.Services.Logging;

namespace KeyboardCleanerPro.Core.Services.Control;

/// <summary>
/// Implements keyboard enable/disable via cfgmgr32 CM_Disable_DevNode /
/// CM_Enable_DevNode — the same mechanism Windows Device Manager uses.
/// This replaces the SetupDiCallClassInstaller approach which fails on
/// some PS/2 and ACPI keyboard drivers (SPAPI error 0xE0000231).
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
        ChangeDeviceState(deviceInstanceId, disable: true);

    /// <inheritdoc />
    public OperationResult EnableDevice(string deviceInstanceId) =>
        ChangeDeviceState(deviceInstanceId, disable: false);

    // ── Core implementation ────────────────────────────────────────────────────

    private OperationResult ChangeDeviceState(string deviceInstanceId, bool disable)
    {
        ArgumentException.ThrowIfNullOrEmpty(deviceInstanceId);

        string operationName = disable ? "DisableDevice" : "EnableDevice";

        // Step 1 — Locate the device node by instance ID.
        // CM_LOCATE_DEVNODE_NORMAL finds only present (running or stopped) devices.
        // CM_LOCATE_DEVNODE_PHANTOM also finds absent/disabled devices — needed
        // so that re-enable works even after the device has been software-disabled.
        uint locateFlags = disable
            ? Infrastructure.Native.CfgMgrNative.CM_LOCATE_DEVNODE_NORMAL
            : Infrastructure.Native.CfgMgrNative.CM_LOCATE_DEVNODE_PHANTOM;

        uint cr = Infrastructure.Native.CfgMgrNative.CM_Locate_DevNodeW(
            out uint devInst, deviceInstanceId, locateFlags);

        if (cr != Infrastructure.Native.CfgMgrNative.CR_SUCCESS)
        {
            _logger.Log(operationName, deviceInstanceId, "CM_Locate_DevNode failed", (int)cr, Environment.UserName);
            return OperationResult.Failure(
                $"Could not locate device '{deviceInstanceId}'. ConfigMgr result = 0x{cr:X8}.", (int)cr);
        }

        // Step 2 — Disable or enable.
        if (disable)
        {
            // CM_DISABLE_UI_NOT_OK suppresses any UI dialog Device Manager would show
            cr = Infrastructure.Native.CfgMgrNative.CM_Disable_DevNode(
                devInst, Infrastructure.Native.CfgMgrNative.CM_DISABLE_UI_NOT_OK);
        }
        else
        {
            cr = Infrastructure.Native.CfgMgrNative.CM_Enable_DevNode(devInst, 0);
        }

        if (cr != Infrastructure.Native.CfgMgrNative.CR_SUCCESS)
        {
            string action = disable ? "CM_Disable_DevNode" : "CM_Enable_DevNode";
            _logger.Log(operationName, deviceInstanceId, $"{action} failed", (int)cr, Environment.UserName);
            return OperationResult.Failure(
                $"{action} failed. ConfigMgr result = 0x{cr:X8}.", (int)cr);
        }

        string done = disable ? "Disabled" : "Enabled";
        _logger.Log(operationName, deviceInstanceId, done, null, Environment.UserName);
        return OperationResult.Success();
    }
}
