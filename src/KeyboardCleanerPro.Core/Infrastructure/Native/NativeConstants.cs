namespace KeyboardCleanerPro.Core.Infrastructure.Native;

/// <summary>
/// Native Win32 constants used for device management via SetupAPI and ConfigMgr.
/// </summary>
internal static class NativeConstants
{
    // ── Keyboard device class GUID ────────────────────────────────────────────
    public static readonly Guid GUID_DEVCLASS_KEYBOARD =
        new("4D36E96B-E325-11CE-BFC1-08002BE10318");

    // ── SetupDiGetClassDevs flags ─────────────────────────────────────────────
    public const uint DIGCF_PRESENT         = 0x00000002;
    public const uint DIGCF_ALLCLASSES      = 0x00000004;
    public const uint DIGCF_DEVICEINTERFACE = 0x00000010;

    // ── Device registry property codes (SPDRP_*) ─────────────────────────────
    public const uint SPDRP_DEVICEDESC            = 0x00000000;
    public const uint SPDRP_HARDWAREID            = 0x00000001;
    public const uint SPDRP_LOCATION_INFORMATION  = 0x0000000D;

    // ── DIF install function ──────────────────────────────────────────────────
    public const uint DIF_PROPERTYCHANGE = 0x00000012;

    // ── Device Install Change State (DICS_*) ──────────────────────────────────
    public const uint DICS_ENABLE  = 0x00000001;
    public const uint DICS_DISABLE = 0x00000002;

    // ── DICS scope flags ──────────────────────────────────────────────────────
    public const uint DICS_FLAG_GLOBAL         = 0x00000001;
    public const uint DICS_FLAG_CONFIGSPECIFIC = 0x00000002;

    // ── Win32 error codes ─────────────────────────────────────────────────────
    public const int ERROR_SUCCESS        = 0;
    public const int ERROR_NO_MORE_ITEMS  = 259;
    public const int ERROR_ACCESS_DENIED  = 5;
    public const int ERROR_NOT_FOUND      = 1168;

    // ── ConfigMgr device node status bits ────────────────────────────────────
    public const uint DN_STARTED          = 0x00000008; // Device started/running
    public const uint CM_PROB_DISABLED    = 0x00000016; // Device disabled (22)

    // ── ConfigMgr return codes ────────────────────────────────────────────────
    public const uint CR_SUCCESS          = 0x00000000;

    // ── Buffer sizes ──────────────────────────────────────────────────────────
    public const uint MAX_DEVICE_ID_LEN   = 200;
    public const int  BUFFER_SIZE_SMALL   = 512;
    public const int  BUFFER_SIZE_LARGE   = 4096;
}
