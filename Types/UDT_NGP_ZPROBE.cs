using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_ZPROBE - Z probe measurements
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_ZPROBE
{
    public float apex;
    public float offset;

    public float Apex
    {
        get => apex;
        set => apex = value;
    }

    public float Offset
    {
        get => offset;
        set => offset = value;
    }
}

