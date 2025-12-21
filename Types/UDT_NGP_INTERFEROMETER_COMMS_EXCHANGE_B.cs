using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_INTERFEROMETER_COMMS_EXCHANGE_B - Interferometer communication exchange B
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_INTERFEROMETER_COMMS_EXCHANGE_B
{
    public short cmdCode;
    public UDT_NGP_INTERFEROMETER_OPTIMIZE_RELATIVE interferometerOptimize = new();

    public short CmdCode
    {
        get => cmdCode;
        set => cmdCode = value;
    }

    public UDT_NGP_INTERFEROMETER_OPTIMIZE_RELATIVE InterferometerOptimize
    {
        get => interferometerOptimize;
        set => interferometerOptimize = value;
    }
}

