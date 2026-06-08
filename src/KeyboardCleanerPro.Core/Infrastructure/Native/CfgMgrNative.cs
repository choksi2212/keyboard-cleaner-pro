using System.Runtime.InteropServices;

namespace KeyboardCleanerPro.Core.Infrastructure.Native;

/// <summary>
/// P/Invoke declarations for cfgmgr32.dll — device-node status, ID queries,
/// and enable/disable operations (the same API Device Manager uses internally).
/// </summary>
internal static class CfgMgrNative
{
    private const string CfgMgrLib = "cfgmgr32.dll";

    // ── CM_Locate_DevNode flags ───────────────────────────────────────────────
    public const uint CM_LOCATE_DEVNODE_NORMAL       = 0x00000000;
    public const uint CM_LOCATE_DEVNODE_PHANTOM      = 0x00000001;

    // ── CM_Disable_DevNode flags ──────────────────────────────────────────────
    /// <summary>Suppress any Device Manager UI when disabling.</summary>
    public const uint CM_DISABLE_UI_NOT_OK           = 0x00000004;

    // ── ConfigMgr return codes ────────────────────────────────────────────────
    public const uint CR_SUCCESS                     = 0x00000000;
    public const uint CR_NOT_DISABLEABLE             = 0x00000017;

    /// <summary>
    /// Retrieves the status and problem code for a device instance.
    /// Returns CR_SUCCESS on success.
    /// </summary>
    [DllImport(CfgMgrLib, CharSet = CharSet.Auto)]
    public static extern uint CM_Get_DevNode_Status(
        out uint pulStatus,
        out uint pulProblemNumber,
        uint     dnDevInst,
        uint     ulFlags);

    /// <summary>
    /// Retrieves the device instance ID string for a given device-node instance.
    /// </summary>
    [DllImport(CfgMgrLib, CharSet = CharSet.Auto)]
    public static extern uint CM_Get_Device_ID(
        uint   dnDevInst,
        char[] buffer,
        uint   bufferLen,
        uint   ulFlags);

    /// <summary>
    /// Retrieves the parent device node for a given device instance.
    /// </summary>
    [DllImport(CfgMgrLib, CharSet = CharSet.Auto)]
    public static extern uint CM_Get_Parent(
        out uint pdnDevInst,
        uint     dnDevInst,
        uint     ulFlags);

    /// <summary>
    /// Locates a device node by its instance-ID string.
    /// Call this to obtain the DEVINST handle before calling Disable/Enable.
    /// </summary>
    [DllImport(CfgMgrLib, CharSet = CharSet.Unicode)]
    public static extern uint CM_Locate_DevNodeW(
        out uint pdnDevInst,
        string   pDeviceID,
        uint     ulFlags);

    /// <summary>
    /// Disables the specified device node — identical to Device Manager → Disable device.
    /// Requires the calling process to be elevated (Administrator).
    /// </summary>
    [DllImport(CfgMgrLib)]
    public static extern uint CM_Disable_DevNode(
        uint dnDevInst,
        uint ulFlags);

    /// <summary>
    /// Enables the specified device node — identical to Device Manager → Enable device.
    /// Requires the calling process to be elevated (Administrator).
    /// </summary>
    [DllImport(CfgMgrLib)]
    public static extern uint CM_Enable_DevNode(
        uint dnDevInst,
        uint ulFlags);
}
