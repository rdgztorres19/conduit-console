using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_CAVITY - Cavity structure with identifier, siteNumber, and lotNumber
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_CAVITY
{
    public short identifier;
    public short siteNumber;
    public LOGIX_STRING lotNumber = new();

    public short Identifier
    {
        get => identifier;
        set => identifier = value;
    }

    public short SiteNumber
    {
        get => siteNumber;
        set => siteNumber = value;
    }

    public LOGIX_STRING LotNumber
    {
        get => lotNumber;
        set => lotNumber = value;
    }
}

