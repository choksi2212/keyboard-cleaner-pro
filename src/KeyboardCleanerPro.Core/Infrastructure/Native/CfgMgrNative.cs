using System.Runtime.InteropServices;

namespace KeyboardCleanerPro.Core.Infrastructure.Native;

/// <summary>
/// P/Invoke declarations for cfgmgr32.dll — device-node status and ID queries.
/// </summary>
internal static class CfgMgrNative
{
    private const string CfgMgrLib = "cfgmgr32.dll";

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
}
