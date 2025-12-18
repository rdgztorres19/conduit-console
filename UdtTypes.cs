using System.Runtime.InteropServices;
using System.Text;

namespace ConduitPlcDemo;

/// <summary>
/// Represents a Logix STRING type (default 82 bytes).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class LOGIX_STRING
{
    /// <summary>
    /// Actual string length (not capacity).
    /// </summary>
    public int stringLength;

    /// <summary>
    /// Backing buffer for Logix STRING (default 82 bytes).
    /// </summary>
    public byte[] stringData = new byte[82];

    public override string ToString()
    {
        return Encoding.ASCII.GetString(stringData, 0, Math.Max(0, Math.Min(stringLength, stringData.Length)));
    }

    public void SetString(string s)
    {
        if (s.Length > stringData.Length)
            throw new ArgumentOutOfRangeException(nameof(s), $"String capacity is {stringData.Length} bytes");

        Array.Clear(stringData, 0, stringData.Length);
        Buffer.BlockCopy(Encoding.ASCII.GetBytes(s), 0, stringData, 0, s.Length);
        stringLength = s.Length;
    }

    public string Value => ToString();
}

/// <summary>
/// Top-level sample structure.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class STRUCT_samples
{
    public STRUCT_samples_base data = new();
    public STRUCT_pallets[] pallets = Enumerable.Range(0, 8).Select(_ => new STRUCT_pallets()).ToArray();

    public STRUCT_samples_base Data
    {
        get => data;
        set => data = value;
    }

    public STRUCT_pallets[] Pallets
    {
        get => pallets;
        set => pallets = value;
    }
}

/// <summary>
/// Sample base data (IDs and timestamps).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class STRUCT_samples_base
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

/// <summary>
/// Pallet structure (contains 8 cavities).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class STRUCT_pallets
{
    public STRUCT_pallets_base data = new();
    public STRUCT_cavities[] cavities = Enumerable.Range(0, 8).Select(_ => new STRUCT_cavities()).ToArray();

    public STRUCT_pallets_base Data
    {
        get => data;
        set => data = value;
    }

    public STRUCT_cavities[] Cavities
    {
        get => cavities;
        set => cavities = value;
    }
}

/// <summary>
/// Pallet base data (RFID, type, curvature).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class STRUCT_pallets_base
{
    public LOGIX_STRING rfid = new();
    public LOGIX_STRING casette_type = new();
    public LOGIX_STRING curvature = new();

    public LOGIX_STRING Rfid
    {
        get => rfid;
        set => rfid = value;
    }

    public LOGIX_STRING CasetteType
    {
        get => casette_type;
        set => casette_type = value;
    }

    public LOGIX_STRING Curvature
    {
        get => curvature;
        set => curvature = value;
    }
}

/// <summary>
/// Cavity structure (site, lot number, position, analysis).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class STRUCT_cavities
{
    public int site = 0;
    public LOGIX_STRING lotNumber = new();
    public STRUCT_interferometer_position interferometerPosition = new();
    public STRUCT_interferometer_analysis cavityAnalysis = new();

    public int Site
    {
        get => site;
        set => site = value;
    }

    public LOGIX_STRING LotNumber
    {
        get => lotNumber;
        set => lotNumber = value;
    }

    public STRUCT_interferometer_position InterferometerPosition
    {
        get => interferometerPosition;
        set => interferometerPosition = value;
    }

    public STRUCT_interferometer_analysis CavityAnalysis
    {
        get => cavityAnalysis;
        set => cavityAnalysis = value;
    }
}

/// <summary>
/// Interferometer position (X, Y, Z coordinates).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class STRUCT_interferometer_position
{
    public float xlocMm;
    public float ylocMm;
    public float zlocMm;
    public float zlocCalcMm;
    public float zlocToricMm;
    public float thetaLocDeg;

    public float XlocMm { get => xlocMm; set => xlocMm = value; }
    public float YlocMm { get => ylocMm; set => ylocMm = value; }
    public float ZlocMm { get => zlocMm; set => zlocMm = value; }
    public float ZlocCalcMm { get => zlocCalcMm; set => zlocCalcMm = value; }
    public float ZlocToricMm { get => zlocToricMm; set => zlocToricMm = value; }
    public float ThetaLocDeg { get => thetaLocDeg; set => thetaLocDeg = value; }
}

/// <summary>
/// Interferometer analysis result (2×DINT).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class STRUCT_interferometer_analysis
{
    public int measurement_status;
    public int result;

    public int MeasurementStatus
    {
        get => measurement_status;
        set => measurement_status = value;
    }

    public int Result
    {
        get => result;
        set => result = value;
    }
}
