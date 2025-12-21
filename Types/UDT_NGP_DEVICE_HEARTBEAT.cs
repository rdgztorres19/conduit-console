using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_DEVICE_HEARTBEAT - Device heartbeat information
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_DEVICE_HEARTBEAT
{
    public LOGIX_STRING deviceId = new();
    public LOGIX_STRING deviceType = new();
    public int heartbeatCount;
    public LOGIX_STRING codeVersion = new();
    public LOGIX_STRING errorCode = new();

    public LOGIX_STRING DeviceId
    {
        get => deviceId;
        set => deviceId = value;
    }

    public LOGIX_STRING DeviceType
    {
        get => deviceType;
        set => deviceType = value;
    }

    public int HeartbeatCount
    {
        get => heartbeatCount;
        set => heartbeatCount = value;
    }

    public LOGIX_STRING CodeVersion
    {
        get => codeVersion;
        set => codeVersion = value;
    }

    public LOGIX_STRING ErrorCode
    {
        get => errorCode;
        set => errorCode = value;
    }
}

