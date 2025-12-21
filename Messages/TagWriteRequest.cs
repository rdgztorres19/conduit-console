namespace ConduitPlcDemo.Messages;

/// <summary>
/// Mensaje de petición para escribir un valor a un path específico de un tag del PLC
/// </summary>
public class TagWriteRequest
{
    /// <summary>
    /// Nombre del tag base (ej: ngpSampleCurrent)
    /// </summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// Path dentro del tag (ej: "data.sampleId" o "pallets[0].data.rfid")
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Valor a escribir
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// ID de correlación opcional para rastrear la petición
    /// </summary>
    public string? CorrelationId { get; set; }
}

