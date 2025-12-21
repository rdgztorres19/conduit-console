using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_LOT - Lot information with design data
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_LOT
{
    public LOGIX_STRING lotNumber = new();
    public LOGIX_STRING generation = new();
    public LOGIX_STRING line = new();
    public LOGIX_STRING analysisId = new();
    public LOGIX_STRING sku = new();
    public LOGIX_STRING status = new();
    public LOGIX_STRING analysisType = new();
    public UDT_NGP_MOLD_DESIGN moldDesign = new();
    public UDT_NGP_PLASTIC_DESIGN[] plasticDesign = Enumerable.Range(0, 2).Select(_ => new UDT_NGP_PLASTIC_DESIGN()).ToArray();

    public LOGIX_STRING LotNumber
    {
        get => lotNumber;
        set => lotNumber = value;
    }

    public LOGIX_STRING Generation
    {
        get => generation;
        set => generation = value;
    }

    public LOGIX_STRING Line
    {
        get => line;
        set => line = value;
    }

    public LOGIX_STRING AnalysisId
    {
        get => analysisId;
        set => analysisId = value;
    }

    public LOGIX_STRING Sku
    {
        get => sku;
        set => sku = value;
    }

    public LOGIX_STRING Status
    {
        get => status;
        set => status = value;
    }

    public LOGIX_STRING AnalysisType
    {
        get => analysisType;
        set => analysisType = value;
    }

    public UDT_NGP_MOLD_DESIGN MoldDesign
    {
        get => moldDesign;
        set => moldDesign = value;
    }

    public UDT_NGP_PLASTIC_DESIGN[] PlasticDesign
    {
        get => plasticDesign;
        set => plasticDesign = value;
    }
}

