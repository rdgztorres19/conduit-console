using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_DEVICE_HEARTBEAT_IN - Device heartbeat input
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_DEVICE_HEARTBEAT_IN
{
    public int heartbeatCount;

    public int HeartbeatCount
    {
        get => heartbeatCount;
        set => heartbeatCount = value;
    }
}

