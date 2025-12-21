using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_INTERFEROMETER_COMMS_EXCHANGE - Interferometer communication exchange
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_INTERFEROMETER_COMMS_EXCHANGE
{
    public short cmdCode;

    public short CmdCode
    {
        get => cmdCode;
        set => cmdCode = value;
    }
}

