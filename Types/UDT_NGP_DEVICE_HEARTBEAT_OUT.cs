using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_DEVICE_HEARTBEAT_OUT - Device heartbeat output
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_DEVICE_HEARTBEAT_OUT
{
    public int heartbeatCountEcho;

    public int HeartbeatCountEcho
    {
        get => heartbeatCountEcho;
        set => heartbeatCountEcho = value;
    }
}

