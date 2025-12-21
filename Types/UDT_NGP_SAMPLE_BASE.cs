using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_SAMPLE_BASE - Sample base data (IDs and timestamps)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_SAMPLE_BASE
{
    public LOGIX_STRING sampleId = new();
    public LOGIX_STRING sampledOn = new();
    public LOGIX_STRING sampledBy = new();

    public LOGIX_STRING SampleId
    {
        get => sampleId;
        set => sampleId = value;
    }

    public LOGIX_STRING SampledOn
    {
        get => sampledOn;
        set => sampledOn = value;
    }

    public LOGIX_STRING SampledBy
    {
        get => sampledBy;
        set => sampledBy = value;
    }
}

