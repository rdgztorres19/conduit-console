using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_INSTRUMENT - Instrument information
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_INSTRUMENT
{
    public UDT_NGP_SYSTEM system = new();
    public UDT_NGP_DEVICE_HEARTBEAT plc = new();
    public UDT_NGP_DEVICE_HEARTBEAT interferometer = new();

    public UDT_NGP_SYSTEM System
    {
        get => system;
        set => system = value;
    }

    public UDT_NGP_DEVICE_HEARTBEAT Plc
    {
        get => plc;
        set => plc = value;
    }

    public UDT_NGP_DEVICE_HEARTBEAT Interferometer
    {
        get => interferometer;
        set => interferometer = value;
    }
}

