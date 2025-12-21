using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_SAMPLE - Top-level sample structure
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_SAMPLE
{
    public UDT_NGP_SAMPLE_BASE data = new();
    public LOGIX_STRING lotNumber = new();
    public UDT_NGP_PALLET[] pallets = Enumerable.Range(0, 8).Select(_ => new UDT_NGP_PALLET()).ToArray();

    public UDT_NGP_SAMPLE_BASE Data
    {
        get => data;
        set => data = value;
    }

    public LOGIX_STRING LotNumber
    {
        get => lotNumber;
        set => lotNumber = value;
    }

    public UDT_NGP_PALLET[] Pallets
    {
        get => pallets;
        set => pallets = value;
    }
}

