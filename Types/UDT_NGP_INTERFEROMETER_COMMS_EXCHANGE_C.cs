using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_INTERFEROMETER_COMMS_EXCHANGE_C - Interferometer communication exchange C
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_INTERFEROMETER_COMMS_EXCHANGE_C
{
    public short cmdCode;
    public UDT_NGP_GANTRY_POSITION gantryPosition = new();
    public UDT_NGP_MACHINE_VISION_POSITIONAL machineVision = new();
    public UDT_NGP_ZPROBE zProbe = new();
    public UDT_NGP_COMPONENT_RESULTS machineVisionResults = new();

    public short CmdCode
    {
        get => cmdCode;
        set => cmdCode = value;
    }

    public UDT_NGP_GANTRY_POSITION GantryPosition
    {
        get => gantryPosition;
        set => gantryPosition = value;
    }

    public UDT_NGP_MACHINE_VISION_POSITIONAL MachineVision
    {
        get => machineVision;
        set => machineVision = value;
    }

    public UDT_NGP_ZPROBE ZProbe
    {
        get => zProbe;
        set => zProbe = value;
    }

    public UDT_NGP_COMPONENT_RESULTS MachineVisionResults
    {
        get => machineVisionResults;
        set => machineVisionResults = value;
    }
}

