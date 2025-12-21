using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_PALLET - Pallet structure (contains 8 cavities)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_PALLET
{
    public UDT_NGP_PALLET_BASE data = new();
    public UDT_NGP_CAVITY[] cavities = Enumerable.Range(0, 8).Select(_ => new UDT_NGP_CAVITY()).ToArray();

    public UDT_NGP_PALLET_BASE Data
    {
        get => data;
        set => data = value;
    }

    public UDT_NGP_CAVITY[] Cavities
    {
        get => cavities;
        set => cavities = value;
    }
}

