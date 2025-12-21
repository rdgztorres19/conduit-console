using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_COMPONENT_RESULTS - Component results with codes
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_COMPONENT_RESULTS
{
    public short Code01;
    public short Code02;
    public short engineeringCode01;

    public short Code01Value
    {
        get => Code01;
        set => Code01 = value;
    }

    public short Code02Value
    {
        get => Code02;
        set => Code02 = value;
    }

    public short EngineeringCode01
    {
        get => engineeringCode01;
        set => engineeringCode01 = value;
    }
}

