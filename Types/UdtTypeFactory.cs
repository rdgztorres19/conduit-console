using System;
using System.Collections.Generic;

namespace ConduitPlcDemo.Types;

/// <summary>
/// Factory that maps UDT names to their corresponding Type for dynamic tag reading.
/// </summary>
public static class UdtTypeFactory
{
    private static readonly Dictionary<string, Type> _tagNameToTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Mapeo de NOMBRES DE TAGS (columna "Name" del PLC) a sus tipos UDT
        // Basado en la imagen del PLC Studio 5000
        
        // Tags principales
        { "ngpLotCurrent", typeof(UDT_NGP_LOT) },
        { "ngpSampleCurrent", typeof(UDT_NGP_SAMPLE) },
        { "tagNgpInstrument", typeof(UDT_NGP_INSTRUMENT) },
        { "tagNgpLot", typeof(UDT_NGP_LOT) }, // Array de 64
        { "tagNgpLotBlank", typeof(UDT_NGP_LOT) },
        { "tagNgpSample", typeof(UDT_NGP_SAMPLE) }, // Array de 100
        { "tagNgpSampleBlank", typeof(UDT_NGP_SAMPLE) },
        { "inDataFtoptix", typeof(UDT_NGP_CAVITY) }, // xUDT_FTOPTIX_CTL - mapeo temporal
        
        // También mapear los nombres de tipos UDT directamente (para compatibilidad)
        { "XUDT_NGP_CAVITY", typeof(UDT_NGP_CAVITY) },
        { "XUDT_NGP_PALLET", typeof(UDT_NGP_PALLET) },
        { "XUDT_NGP_PALLET_BASE", typeof(UDT_NGP_PALLET_BASE) },
        { "XUDT_NGP_SAMPLE", typeof(UDT_NGP_SAMPLE) },
        { "XUDT_NGP_SAMPLE_BASE", typeof(UDT_NGP_SAMPLE_BASE) },
        { "XUDT_NGP_LOT", typeof(UDT_NGP_LOT) },
        { "XUDT_NGP_MOLD_DESIGN", typeof(UDT_NGP_MOLD_DESIGN) },
        { "XUDT_NGP_PLASTIC_DESIGN", typeof(UDT_NGP_PLASTIC_DESIGN) },
        { "XUDT_NGP_SYSTEM", typeof(UDT_NGP_SYSTEM) },
        { "XUDT_NGP_DEVICE_HEARTBEAT", typeof(UDT_NGP_DEVICE_HEARTBEAT) },
        { "XUDT_NGP_INSTRUMENT", typeof(UDT_NGP_INSTRUMENT) },
        { "XUDT_FTOPTIX_CTL", typeof(UDT_NGP_CAVITY) },
        
        // También soportar nombres con minúsculas (case-insensitive)
        { "xUDT_NGP_CAVITY", typeof(UDT_NGP_CAVITY) },
        { "xUDT_NGP_PALLET", typeof(UDT_NGP_PALLET) },
        { "xUDT_NGP_PALLET_BASE", typeof(UDT_NGP_PALLET_BASE) },
        { "xUDT_NGP_SAMPLE", typeof(UDT_NGP_SAMPLE) },
        { "xUDT_NGP_SAMPLE_BASE", typeof(UDT_NGP_SAMPLE_BASE) },
        { "xUDT_NGP_LOT", typeof(UDT_NGP_LOT) },
        { "xUDT_NGP_MOLD_DESIGN", typeof(UDT_NGP_MOLD_DESIGN) },
        { "xUDT_NGP_PLASTIC_DESIGN", typeof(UDT_NGP_PLASTIC_DESIGN) },
        { "xUDT_NGP_SYSTEM", typeof(UDT_NGP_SYSTEM) },
        { "xUDT_NGP_DEVICE_HEARTBEAT", typeof(UDT_NGP_DEVICE_HEARTBEAT) },
        { "xUDT_NGP_INSTRUMENT", typeof(UDT_NGP_INSTRUMENT) },
        { "xUDT_FTOPTIX_CTL", typeof(UDT_NGP_CAVITY) },
        
        // También soportar nombres sin prefijo x para compatibilidad
        { "UDT_NGP_CAVITY", typeof(UDT_NGP_CAVITY) },
        { "UDT_NGP_PALLET_BASE", typeof(UDT_NGP_PALLET_BASE) },
        { "UDT_NGP_SAMPLE_BASE", typeof(UDT_NGP_SAMPLE_BASE) },
        { "UDT_NGP_COMPONENT_RESULTS", typeof(UDT_NGP_COMPONENT_RESULTS) },
        { "UDT_NGP_DEVICE_HEARTBEAT", typeof(UDT_NGP_DEVICE_HEARTBEAT) },
        { "UDT_NGP_DEVICE_HEARTBEAT_IN", typeof(UDT_NGP_DEVICE_HEARTBEAT_IN) },
        { "UDT_NGP_DEVICE_HEARTBEAT_OUT", typeof(UDT_NGP_DEVICE_HEARTBEAT_OUT) },
        { "UDT_NGP_GANTRY_POSITION", typeof(UDT_NGP_GANTRY_POSITION) },
        { "UDT_NGP_INTERFEROMETER_ANALYSIS", typeof(UDT_NGP_INTERFEROMETER_ANALYSIS) },
        { "UDT_NGP_INTERFEROMETER_COMMS_EXCHANGE", typeof(UDT_NGP_INTERFEROMETER_COMMS_EXCHANGE) },
        { "UDT_NGP_INTERFEROMETER_OPTIMIZE_RELATIVE", typeof(UDT_NGP_INTERFEROMETER_OPTIMIZE_RELATIVE) },
        { "UDT_NGP_MACHINE_VISION_POSITIONAL", typeof(UDT_NGP_MACHINE_VISION_POSITIONAL) },
        { "UDT_NGP_MOLD_DESIGN", typeof(UDT_NGP_MOLD_DESIGN) },
        { "UDT_NGP_PLASTIC_DESIGN", typeof(UDT_NGP_PLASTIC_DESIGN) },
        { "UDT_NGP_SYSTEM", typeof(UDT_NGP_SYSTEM) },
        { "UDT_NGP_ZPROBE", typeof(UDT_NGP_ZPROBE) },
        { "UDT_NGP_PALLET", typeof(UDT_NGP_PALLET) },
        { "UDT_NGP_SAMPLE", typeof(UDT_NGP_SAMPLE) },
        { "UDT_NGP_LOT", typeof(UDT_NGP_LOT) },
        { "UDT_NGP_CAVITY_INPROCESS", typeof(UDT_NGP_CAVITY_INPROCESS) },
        { "UDT_NGP_INSTRUMENT", typeof(UDT_NGP_INSTRUMENT) },
        { "UDT_NGP_INTERFEROMETER_COMMS_EXCHANGE_B", typeof(UDT_NGP_INTERFEROMETER_COMMS_EXCHANGE_B) },
        { "UDT_NGP_INTERFEROMETER_COMMS_EXCHANGE_C", typeof(UDT_NGP_INTERFEROMETER_COMMS_EXCHANGE_C) },
        { "UDT_NGP_STAGED_SAMPLE_INPROCESS", typeof(UDT_NGP_STAGED_SAMPLE_INPROCESS) },
        
        // Basic types
        { "LOGIX_STRING", typeof(LOGIX_STRING) },
        { "STRING", typeof(LOGIX_STRING) },
    };

    /// <summary>
    /// Gets the Type for a given tag name or UDT name.
    /// Handles array notation like "xUDT_NGP_LOT[64]" by extracting the base type name.
    /// </summary>
    /// <param name="tagOrUdtName">The tag name (from PLC) or UDT name (case-insensitive). Can include array notation like "[64]"</param>
    /// <returns>The Type if found, null otherwise</returns>
    public static Type? GetType(string tagOrUdtName)
    {
        if (string.IsNullOrWhiteSpace(tagOrUdtName))
            return null;

        // Remove array notation if present (e.g., "xUDT_NGP_LOT[64]" -> "xUDT_NGP_LOT")
        var baseName = tagOrUdtName;
        var bracketIndex = baseName.IndexOf('[');
        if (bracketIndex >= 0)
        {
            baseName = baseName.Substring(0, bracketIndex).Trim();
        }

        return _tagNameToTypeMap.TryGetValue(baseName, out var type) ? type : null;
    }

    /// <summary>
    /// Gets the Type for a given tag name or UDT name, throwing an exception if not found.
    /// </summary>
    /// <param name="tagOrUdtName">The tag name or UDT name (case-insensitive)</param>
    /// <returns>The Type</returns>
    /// <exception cref="ArgumentException">Thrown when the tag/UDT name is not found</exception>
    public static Type GetTypeOrThrow(string tagOrUdtName)
    {
        var type = GetType(tagOrUdtName);
        if (type == null)
        {
            throw new ArgumentException($"Tag/UDT type '{tagOrUdtName}' not found. Available types: {string.Join(", ", _tagNameToTypeMap.Keys)}", nameof(tagOrUdtName));
        }
        return type;
    }

    /// <summary>
    /// Checks if a tag name or UDT name is registered.
    /// </summary>
    /// <param name="tagOrUdtName">The tag name or UDT name (case-insensitive)</param>
    /// <returns>True if the tag/UDT is registered, false otherwise</returns>
    public static bool IsRegistered(string tagOrUdtName)
    {
        if (string.IsNullOrWhiteSpace(tagOrUdtName))
            return false;
            
        // Remove array notation if present
        var baseName = tagOrUdtName;
        var bracketIndex = baseName.IndexOf('[');
        if (bracketIndex >= 0)
        {
            baseName = baseName.Substring(0, bracketIndex).Trim();
        }
        
        return _tagNameToTypeMap.ContainsKey(baseName);
    }

    /// <summary>
    /// Gets all registered tag/UDT names.
    /// </summary>
    /// <returns>A collection of all registered tag/UDT names</returns>
    public static IEnumerable<string> GetAllRegisteredNames()
    {
        return _tagNameToTypeMap.Keys;
    }

    /// <summary>
    /// Gets all registered UDT types.
    /// </summary>
    /// <returns>A collection of all registered UDT types</returns>
    public static IEnumerable<Type> GetAllRegisteredTypes()
    {
        return _tagNameToTypeMap.Values;
    }
}

