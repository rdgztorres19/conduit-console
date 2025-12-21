namespace ConduitPlcDemo.Messages;

/// <summary>
/// Mensaje de respuesta con el valor del tag leído del PLC
/// </summary>
public class TagReadResponse
{
    /// <summary>
    /// Nombre del tag leído
    /// </summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// Valor del tag (puede ser cualquier tipo)
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Calidad del tag
    /// </summary>
    public string Quality { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp de cuando se leyó el tag
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// ID de correlación para rastrear la petición original
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Indica si hubo un error al leer el tag
    /// </summary>
    public bool HasError { get; set; }

    /// <summary>
    /// Mensaje de error si hubo algún problema
    /// </summary>
    public string? ErrorMessage { get; set; }
}
