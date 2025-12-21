using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_CAVITY_INPROCESS - Cavity in-process data
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_CAVITY_INPROCESS
{
    public int[] plcTime = new int[7];
    public int cmdCode;
    public LOGIX_STRING loadedBy = new();
    public LOGIX_STRING loadedOn = new();
    public LOGIX_STRING measuredBy = new();
    public LOGIX_STRING measuredOn = new();
    public UDT_NGP_LOT lot = new();
    public UDT_NGP_SAMPLE_BASE sample = new();
    public UDT_NGP_PALLET_BASE pallet = new();
    public UDT_NGP_CAVITY cavity = new();

    public int[] PlcTime
    {
        get => plcTime;
        set => plcTime = value;
    }

    public int CmdCode
    {
        get => cmdCode;
        set => cmdCode = value;
    }

    public LOGIX_STRING LoadedBy
    {
        get => loadedBy;
        set => loadedBy = value;
    }

    public LOGIX_STRING LoadedOn
    {
        get => loadedOn;
        set => loadedOn = value;
    }

    public LOGIX_STRING MeasuredBy
    {
        get => measuredBy;
        set => measuredBy = value;
    }

    public LOGIX_STRING MeasuredOn
    {
        get => measuredOn;
        set => measuredOn = value;
    }

    public UDT_NGP_LOT Lot
    {
        get => lot;
        set => lot = value;
    }

    public UDT_NGP_SAMPLE_BASE Sample
    {
        get => sample;
        set => sample = value;
    }

    public UDT_NGP_PALLET_BASE Pallet
    {
        get => pallet;
        set => pallet = value;
    }

    public UDT_NGP_CAVITY Cavity
    {
        get => cavity;
        set => cavity = value;
    }
}

