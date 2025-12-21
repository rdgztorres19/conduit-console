using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_GANTRY_POSITION - Gantry position coordinates
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_GANTRY_POSITION
{
    public float xloc;
    public float yloc;
    public float zloc;
    public float zlocCalc;
    public float zlocToric;
    public float xlocOptimized;
    public float ylocOptimized;
    public float zlocOptimized;

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

    public float Zloc
    {
        get => zloc;
        set => zloc = value;
    }

    public float ZlocCalc
    {
        get => zlocCalc;
        set => zlocCalc = value;
    }

    public float ZlocToric
    {
        get => zlocToric;
        set => zlocToric = value;
    }

    public float XlocOptimized
    {
        get => xlocOptimized;
        set => xlocOptimized = value;
    }

    public float YlocOptimized
    {
        get => ylocOptimized;
        set => ylocOptimized = value;
    }

    public float ZlocOptimized
    {
        get => zlocOptimized;
        set => zlocOptimized = value;
    }
}

