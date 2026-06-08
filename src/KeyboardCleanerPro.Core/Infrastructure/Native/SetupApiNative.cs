using System.Runtime.InteropServices;
using static KeyboardCleanerPro.Core.Infrastructure.Native.NativeStructures;

namespace KeyboardCleanerPro.Core.Infrastructure.Native;

/// <summary>
/// P/Invoke declarations for setupapi.dll — device enumeration, property query and install control.
/// </summary>
internal static class SetupApiNative
{
    private const string SetupApiLib = "setupapi.dll";

    /// <summary>Returns a handle to a device-information set for all keyboard-class devices.</summary>
    [DllImport(SetupApiLib, SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr SetupDiGetClassDevs(
        ref Guid   classGuid,
        string?    enumerator,
        IntPtr     hwndParent,
        uint       flags);

    /// <summary>Enumerates devices in an information set by sequential index.</summary>
    [DllImport(SetupApiLib, SetLastError = true)]
    public static extern bool SetupDiEnumDeviceInfo(
        IntPtr               deviceInfoSet,
        uint                 memberIndex,
        ref SP_DEVINFO_DATA  deviceInfoData);

    /// <summary>Retrieves the device-instance ID string for a device.</summary>
    [DllImport(SetupApiLib, SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool SetupDiGetDeviceInstanceId(
        IntPtr              deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        char[]              deviceInstanceId,
        uint                deviceInstanceIdSize,
        out uint            requiredSize);

    /// <summary>Retrieves a device registry property (e.g. hardware ID, description).</summary>
    [DllImport(SetupApiLib, SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool SetupDiGetDeviceRegistryProperty(
        IntPtr              deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        uint                property,
        out uint            propertyRegDataType,
        byte[]?             propertyBuffer,
        uint                propertyBufferSize,
        out uint            requiredSize);

    /// <summary>Sets the class-installer parameters prior to calling SetupDiCallClassInstaller.</summary>
    [DllImport(SetupApiLib, SetLastError = true)]
    public static extern bool SetupDiSetClassInstallParams(
        IntPtr                  deviceInfoSet,
        ref SP_DEVINFO_DATA     deviceInfoData,
        ref SP_PROPCHANGE_PARAMS classInstallParams,
        uint                    classInstallParamsSize);

    /// <summary>Performs the device install action identified by InstallFunction (e.g. DIF_PROPERTYCHANGE).</summary>
    [DllImport(SetupApiLib, SetLastError = true)]
    public static extern bool SetupDiCallClassInstaller(
        uint                installFunction,
        IntPtr              deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData);

    /// <summary>Destroys a device information set and frees all associated memory.</summary>
    [DllImport(SetupApiLib, SetLastError = true)]
    public static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);
}
