using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_PLASTIC_DESIGN - Plastic design specifications
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_PLASTIC_DESIGN
{
    public LOGIX_STRING designCode = new();
    public LOGIX_STRING toolDesign = new();
    public float nominalSphereROC;
    public float nominalCylinderROC;
    public LOGIX_STRING curvature = new();
    public float flangeThickness;
    public float criticalSurfaceSag;
    public float centerThickness;
    public float nonCriticalSurfaceRadius;
    public float rsph;
    public float criticalSurfaceRadius;
    public float fociiOffset;

    public LOGIX_STRING DesignCode
    {
        get => designCode;
        set => designCode = value;
    }

    public LOGIX_STRING ToolDesign
    {
        get => toolDesign;
        set => toolDesign = value;
    }

    public float NominalSphereROC
    {
        get => nominalSphereROC;
        set => nominalSphereROC = value;
    }

    public float NominalCylinderROC
    {
        get => nominalCylinderROC;
        set => nominalCylinderROC = value;
    }

    public LOGIX_STRING Curvature
    {
        get => curvature;
        set => curvature = value;
    }

    public float FlangeThickness
    {
        get => flangeThickness;
        set => flangeThickness = value;
    }

    public float CriticalSurfaceSag
    {
        get => criticalSurfaceSag;
        set => criticalSurfaceSag = value;
    }

    public float CenterThickness
    {
        get => centerThickness;
        set => centerThickness = value;
    }

    public float NonCriticalSurfaceRadius
    {
        get => nonCriticalSurfaceRadius;
        set => nonCriticalSurfaceRadius = value;
    }

    public float Rsph
    {
        get => rsph;
        set => rsph = value;
    }

    public float CriticalSurfaceRadius
    {
        get => criticalSurfaceRadius;
        set => criticalSurfaceRadius = value;
    }

    public float FociiOffset
    {
        get => fociiOffset;
        set => fociiOffset = value;
    }
}

