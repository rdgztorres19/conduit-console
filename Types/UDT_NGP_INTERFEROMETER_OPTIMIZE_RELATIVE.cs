using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_INTERFEROMETER_OPTIMIZE_RELATIVE - Relative optimization coordinates
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_INTERFEROMETER_OPTIMIZE_RELATIVE
{
    public float xlocRelative;
    public float ylocRelative;
    public float zlocRelative;

    public float XlocRelative
    {
        get => xlocRelative;
        set => xlocRelative = value;
    }

    public float YlocRelative
    {
        get => ylocRelative;
        set => ylocRelative = value;
    }

    public float ZlocRelative
    {
        get => zlocRelative;
        set => zlocRelative = value;
    }
}

