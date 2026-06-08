using System.Runtime.InteropServices;

namespace KeyboardCleanerPro.Core.Infrastructure.Native;

/// <summary>
/// Interop structures required by SetupAPI device management calls.
/// Layouts are fixed to match the native ABI exactly.
/// </summary>
internal static class NativeStructures
{
    /// <summary>
    /// SP_DEVINFO_DATA – Identifies a single device in a device information set.
    /// cbSize MUST be set to Marshal.SizeOf{SP_DEVINFO_DATA} before every call.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVINFO_DATA
    {
        public uint   cbSize;
        public Guid   ClassGuid;
        public uint   DevInst;
        public IntPtr Reserved;
    }

    /// <summary>
    /// SP_CLASSINSTALL_HEADER – Header prepended to every class-installer parameter block.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SP_CLASSINSTALL_HEADER
    {
        public uint cbSize;
        public uint InstallFunction;
    }

    /// <summary>
    /// SP_PROPCHANGE_PARAMS – Parameters for DIF_PROPERTYCHANGE used to enable/disable devices.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SP_PROPCHANGE_PARAMS
    {
        public SP_CLASSINSTALL_HEADER ClassInstallHeader;
        public uint StateChange;
        public uint Scope;
        public uint HwProfile;
    }
}
