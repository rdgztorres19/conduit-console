namespace ConduitPlcDemo.Messages;

/// <summary>
/// Mensaje de respuesta después de escribir un valor a un tag del PLC
/// </summary>
public class TagWriteResponse
{
    /// <summary>
    /// Nombre del tag base
    /// </summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// Path dentro del tag que se escribió
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Tag completo (tagName.path)
    /// </summary>
    public string FullTagName { get; set; } = string.Empty;

    /// <summary>
    /// Valor que se escribió
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Indica si la escritura fue exitosa
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Timestamp de cuando se escribió el tag
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// ID de correlación para rastrear la petición original
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Mensaje de error si hubo algún problema
    /// </summary>
    public string? ErrorMessage { get; set; }
}

