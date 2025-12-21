using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_INTERFEROMETER_ANALYSIS - Interferometer analysis result (2×INT)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_INTERFEROMETER_ANALYSIS
{
    public short measurement_status;
    public short result;

    public short MeasurementStatus
    {
        get => measurement_status;
        set => measurement_status = value;
    }

    public short Result
    {
        get => result;
        set => result = value;
    }
}

