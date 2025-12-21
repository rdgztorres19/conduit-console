using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_PALLET_BASE - Pallet base data (RFID, casette type, curvature)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_PALLET_BASE
{
    public LOGIX_STRING rfid = new();
    public LOGIX_STRING casetteType = new();
    public LOGIX_STRING curvature = new();

    public LOGIX_STRING Rfid
    {
        get => rfid;
        set => rfid = value;
    }

    public LOGIX_STRING CasetteType
    {
        get => casetteType;
        set => casetteType = value;
    }

    public LOGIX_STRING Curvature
    {
        get => curvature;
        set => curvature = value;
    }
}

