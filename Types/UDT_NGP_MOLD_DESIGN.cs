using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_MOLD_DESIGN - Mold design information
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_MOLD_DESIGN
{
    public LOGIX_STRING lsfcId = new();
    public LOGIX_STRING lsbcId = new();
    public LOGIX_STRING csideId = new();
    public LOGIX_STRING fcaId = new();
    public LOGIX_STRING bcaId = new();
    public LOGIX_STRING fcb1Id = new();
    public LOGIX_STRING bcb1Id = new();
    public LOGIX_STRING fcb2Id = new();
    public LOGIX_STRING bcb2Id = new();
    public LOGIX_STRING fca2Id = new();
    public LOGIX_STRING bca2Id = new();
    public LOGIX_STRING frontCasDesign = new();
    public LOGIX_STRING backCasDesign = new();
    public LOGIX_STRING csFrontCoreDesign = new();
    public LOGIX_STRING rearCoreDesign = new();

    public LOGIX_STRING LsfcId
    {
        get => lsfcId;
        set => lsfcId = value;
    }

    public LOGIX_STRING LsbcId
    {
        get => lsbcId;
        set => lsbcId = value;
    }

    public LOGIX_STRING CsideId
    {
        get => csideId;
        set => csideId = value;
    }

    public LOGIX_STRING FcaId
    {
        get => fcaId;
        set => fcaId = value;
    }

    public LOGIX_STRING BcaId
    {
        get => bcaId;
        set => bcaId = value;
    }

    public LOGIX_STRING Fcb1Id
    {
        get => fcb1Id;
        set => fcb1Id = value;
    }

    public LOGIX_STRING Bcb1Id
    {
        get => bcb1Id;
        set => bcb1Id = value;
    }

    public LOGIX_STRING Fcb2Id
    {
        get => fcb2Id;
        set => fcb2Id = value;
    }

    public LOGIX_STRING Bcb2Id
    {
        get => bcb2Id;
        set => bcb2Id = value;
    }

    public LOGIX_STRING Fca2Id
    {
        get => fca2Id;
        set => fca2Id = value;
    }

    public LOGIX_STRING Bca2Id
    {
        get => bca2Id;
        set => bca2Id = value;
    }

    public LOGIX_STRING FrontCasDesign
    {
        get => frontCasDesign;
        set => frontCasDesign = value;
    }

    public LOGIX_STRING BackCasDesign
    {
        get => backCasDesign;
        set => backCasDesign = value;
    }

    public LOGIX_STRING CsFrontCoreDesign
    {
        get => csFrontCoreDesign;
        set => csFrontCoreDesign = value;
    }

    public LOGIX_STRING RearCoreDesign
    {
        get => rearCoreDesign;
        set => rearCoreDesign = value;
    }
}

