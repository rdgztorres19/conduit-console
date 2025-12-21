using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_STAGED_SAMPLE_INPROCESS - Staged sample in-process data
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_STAGED_SAMPLE_INPROCESS
{
    public int[] plcTimestamp = new int[7];
    public LOGIX_STRING loadedBy = new();
    public LOGIX_STRING measuredBy = new();
    public short measuredCavities;
    public short activeCavity;
    public short lotsCount;
    public UDT_NGP_LOT[] lots = Enumerable.Range(0, 32).Select(_ => new UDT_NGP_LOT()).ToArray();
    public UDT_NGP_SAMPLE sample = new();

    public int[] PlcTimestamp
    {
        get => plcTimestamp;
        set => plcTimestamp = value;
    }

    public LOGIX_STRING LoadedBy
    {
        get => loadedBy;
        set => loadedBy = value;
    }

    public LOGIX_STRING MeasuredBy
    {
        get => measuredBy;
        set => measuredBy = value;
    }

    public short MeasuredCavities
    {
        get => measuredCavities;
        set => measuredCavities = value;
    }

    public short ActiveCavity
    {
        get => activeCavity;
        set => activeCavity = value;
    }

    public short LotsCount
    {
        get => lotsCount;
        set => lotsCount = value;
    }

    public UDT_NGP_LOT[] Lots
    {
        get => lots;
        set => lots = value;
    }

    public UDT_NGP_SAMPLE Sample
    {
        get => sample;
        set => sample = value;
    }
}

