namespace ConduitPlcDemo.Messages;

/// <summary>
/// Mensaje de petición para leer un tag del PLC
/// </summary>
public class TagReadRequest
{
    /// <summary>
    /// Nombre del tag a leer
    /// </summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// ID de correlación opcional para rastrear la petición
    /// </summary>
    public string? CorrelationId { get; set; }
}
