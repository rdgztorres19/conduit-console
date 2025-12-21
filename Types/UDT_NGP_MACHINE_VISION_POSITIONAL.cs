using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_MACHINE_VISION_POSITIONAL - Machine vision positional data
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_MACHINE_VISION_POSITIONAL
{
    public float xloc;
    public float yloc;
    public float theta;

    public float Xloc
    {
        get => xloc;
        set => xloc = value;
    }

    public float Yloc
    {
        get => yloc;
        set => yloc = value;
    }

    public float Theta
    {
        get => theta;
        set => theta = value;
    }
}

