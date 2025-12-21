using System.Runtime.InteropServices;
using System.Text;

namespace ConduitPlcDemo.Types;

/// <summary>
/// Represents a Logix STRING type (default 82 bytes).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
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

