using System.Runtime.InteropServices;

namespace ConduitPlcDemo.Types;

/// <summary>
/// UDT_NGP_SYSTEM - System information
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public class UDT_NGP_SYSTEM
{
    public LOGIX_STRING machineId = new();
    public LOGIX_STRING assetId = new();
    public LOGIX_STRING mode = new();
    public LOGIX_STRING state = new();
    public LOGIX_STRING timestamp = new();
    public int[] plcTimestamp = new int[7];
    public LOGIX_STRING userFtSecurityCode = new();
    public LOGIX_STRING userId = new();
    public LOGIX_STRING calibrationId = new();
    public LOGIX_STRING lastCalibration = new();

    public LOGIX_STRING MachineId
    {
        get => machineId;
        set => machineId = value;
    }

    public LOGIX_STRING AssetId
    {
        get => assetId;
        set => assetId = value;
    }

    public LOGIX_STRING Mode
    {
        get => mode;
        set => mode = value;
    }

    public LOGIX_STRING State
    {
        get => state;
        set => state = value;
    }

    public LOGIX_STRING Timestamp
    {
        get => timestamp;
        set => timestamp = value;
    }

    public int[] PlcTimestamp
    {
        get => plcTimestamp;
        set => plcTimestamp = value;
    }

    public LOGIX_STRING UserFtSecurityCode
    {
        get => userFtSecurityCode;
        set => userFtSecurityCode = value;
    }

    public LOGIX_STRING UserId
    {
        get => userId;
        set => userId = value;
    }

    public LOGIX_STRING CalibrationId
    {
        get => calibrationId;
        set => calibrationId = value;
    }

    public LOGIX_STRING LastCalibration
    {
        get => lastCalibration;
        set => lastCalibration = value;
    }
}

